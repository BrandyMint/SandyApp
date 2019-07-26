#define WORKAROUND_ANDROID_UNITY_SHADER_COMPILER_BUG

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// This class processes events before and after all the build processes.
    /// It also saves and restores <i>ProjectSettings.asset</i> that have been programmatically
    /// changed for specific build configurations.
    /// </summary>
    [InitializeOnLoad]
    public class BuildTime :  IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        public int callbackOrder {
            get { return 0; }
        }

        /// <summary>
        /// Restore settings after UnityEditor crash.
        /// </summary>
        static BuildTime() {
            RestoreOverridenIcons();
            RestoreSettings();
        }

        private const string _SETTINGS_PATH = "ProjectSettings/ProjectSettings.asset";
        private const string _SETTINGS_TEMP_PATH = "ProjectSettings/ProjectSettings.temp";
        private const string _BUILD_HELPER_TEMP_PATH = "BuildHelper_Temp";
        private const string _OVERRIDE_ICONS_LOG_PATH = _BUILD_HELPER_TEMP_PATH + "/override_icons_log.txt";
        private static bool _settingsAlreadySaved;

        /// <summary>
        /// Implements <see cref="IPreprocessBuild.OnPreprocessBuild"/>.
        /// Is performed before all the build processes, saves <i>ProjectSettings.asset</i> and
        /// sets build version.
        /// </summary>
        /// <param name="report"></param>
        public void OnPreprocessBuild(BuildReport report) {
#if UNITY_EDITOR_LINUX && WORKAROUND_ANDROID_UNITY_SHADER_COMPILER_BUG
            if (report.summary.platform == BuildTarget.Android)
                KillUnityShaderCompiler();
#endif
            Debug.Log("Starting build to: " + report.summary.outputPath);
            SaveSettingsToRestore();
            PlayerSettings.bundleVersion = BuildHelperStrings.GetBuildVersion();
            if (WasOverrideIcon())
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// Implements <see cref="IPostprocessBuild.OnPostprocessBuild"/>.
        /// Is performed after all the build processes and restore <i>ProjectSettings.asset</i>.
        /// </summary>
        /// <param name="report"></param>
        public void OnPostprocessBuild(BuildReport report) {
            RestoreOverridenIcons();
            RestoreSettings();
        }

        /// <summary>
        /// Saves <i>ProjectSettings.asset</i> to temporary file for change and then restore.
        /// </summary>
        /// <seealso cref="RestoreSettings"/>
        /// <seealso cref="RestoreSettingsIfFailed"/>
        public static void SaveSettingsToRestore() {
            KeepKeystoreInfo();
            if (!_settingsAlreadySaved) {
                var settingsPath = BuildHelperStrings.ProjRoot(_SETTINGS_PATH);
                var settingsPathTemp = BuildHelperStrings.ProjRoot(_SETTINGS_TEMP_PATH);
                File.Copy(settingsPath, settingsPathTemp, true);
                _settingsAlreadySaved = true;
                Debug.Log("BuildTime: Project Settings saved");
            }
        }

        /// <summary>
        /// Saves changes of <i>ProjectSettings.asset</i> and delete temporary file.
        /// After this and until a new call to <see cref="SaveSettingsToRestore"/>,
        /// the call to <see cref="RestoreSettings"/> does not produce any effect.
        /// </summary>
        public static void AcceptChangedSettings() {
            var settingsPathTemp = BuildHelperStrings.ProjRoot(_SETTINGS_TEMP_PATH);
            if (File.Exists(settingsPathTemp)) {
                File.Delete(settingsPathTemp);
            }
            _settingsAlreadySaved = false;
            AssetDatabase.SaveAssets();
            Debug.Log("BuildTime: Project Settings accepted");
        }

        /// <summary>
        /// Restores the saved <i>ProjectSettings.asset</i> and refresh Unity asset database.
        /// If <i>ProjectSettings.asset</i> is not saved then nothing will happen.   
        /// </summary>
        /// <remarks>It is better to use <see cref="RestoreSettingsIfFailed"/> if there is no guarantee 
        /// that the build process will not break with the exception 
        /// and <i>RestoreSettings</i> is not executed.</remarks>
        /// <seealso cref="SaveSettingsToRestore"/>
        public static void RestoreSettings() {
            var settingsPath = BuildHelperStrings.ProjRoot(_SETTINGS_PATH);
            var settingsPathTemp = BuildHelperStrings.ProjRoot(_SETTINGS_TEMP_PATH);
            if (File.Exists(settingsPathTemp)) {
                File.Copy(settingsPathTemp, settingsPath, true);
                File.Delete(settingsPathTemp);
                Debug.Log("BuildTime: Project Settings restored");
                AssetDatabase.Refresh();
            }
            _settingsAlreadySaved = false;
            RestoreKeystoreInfo();
        }

        /// <summary>
        /// Perform specified action and safely restore <i>ProjectSettings.asset</i> 
        /// if action throws exception.
        /// </summary>
        /// <param name="buildAction">An action that can throw exception</param>
        /// <exception cref="Exception">Exception that thrown by an action</exception>
        /// <seealso cref="SaveSettingsToRestore"/>
        /// <seealso cref="RestoreSettings"/>
        public static void RestoreSettingsIfFailed(Action buildAction) {
            try {
                buildAction();
            } catch (Exception e) {
                RestoreOverridenIcons();
                RestoreSettings();
                throw e;
            }
        }

        /// <summary>
        /// Wrapper for <see cref="BuildPipeline.BuildPlayer(BuildPlayerOptions)"/>.
        /// </summary>
        /// <param name="options">Options for <i>BuildPlayer</i></param>
        /// <exception cref="BuildFailedException">Throw 
        /// if <see cref="BuildPipeline.BuildPlayer(BuildPlayerOptions)"/> return error</exception>
        public static void Build(BuildPlayerOptions options) {
#if UNITY_2018_1_OR_NEWER
            var report = BuildPipeline.BuildPlayer(options);
            if (report == null)
                throw new BuildFailedException("Build failed. No report generated!");
            else {
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException("Check log file. Build ended unsuccessfully with result: " + report.summary.result);
            }
#else
            if (!string.IsNullOrEmpty(BuildPipeline.BuildPlayer(options))) {
                throw new BuildFailedException("");
            }
#endif
        }

#region OverrideIcon
        public static void OverrideIcon(string defIconPath, string overrideIconPath) {
            var tempPath = GenFileNameInBuildHelperTempPath();
            FileUtil.CopyFileOrDirectory(defIconPath, tempPath);
            FileUtil.ReplaceFile(overrideIconPath, defIconPath);
            LogOverrideIcon(defIconPath, tempPath);
        }
        
        private static void RestoreOverridenIcons() {
            if (WasOverrideIcon()) {
                var logPath = BuildHelperStrings.ProjRoot(_OVERRIDE_ICONS_LOG_PATH);
                var lines = File.ReadAllLines(logPath);
                for (int i = lines.Length - 1; i > 0; i -= 2) {
                    FileUtil.ReplaceFile(lines[i], lines[i - 1]);                    
                }
                Directory.Delete(BuildHelperStrings.ProjRoot(_BUILD_HELPER_TEMP_PATH), true);
            }
        }
        
        private static bool WasOverrideIcon() {
            var logPath = BuildHelperStrings.ProjRoot(_OVERRIDE_ICONS_LOG_PATH);
            return File.Exists(logPath);
        }

        private static string GenFileNameInBuildHelperTempPath() {
            var dir = Directory.CreateDirectory(BuildHelperStrings.ProjRoot(_BUILD_HELPER_TEMP_PATH));
            var existFiles = dir.GetFiles();
            string fileName;
            do {
                fileName = UnityEngine.Random.Range(0, int.MaxValue).ToString();
            } while (existFiles.Any(file => file.Name == fileName));
            return Path.Combine(dir.FullName, fileName);
        }

        private static void LogOverrideIcon(params string[] lines) {
            var logPath = BuildHelperStrings.ProjRoot(_OVERRIDE_ICONS_LOG_PATH);
            using (var sw = new StreamWriter(logPath, true)) {
                foreach (var line in lines) {
                    sw.WriteLine(line);
                }
            }
        }
#endregion

#region Keep Keystore Info        
        private class KeystoreInfo {
            public string keystoreName { get; set; }
            public string keystorePass { get; set; }
            public string keyaliasName { get; set; }
            public string keyaliasPass { get; set; }
        }
        private static KeystoreInfo _keystoreInfo;

        private static void KeepKeystoreInfo() {
            if (_keystoreInfo == null) {
                _keystoreInfo = new KeystoreInfo();
            }
            CopyProperties<KeystoreInfo>(typeof(PlayerSettings.Android), _keystoreInfo);
        }

        private static void RestoreKeystoreInfo() {
            if (_keystoreInfo != null) {
                CopyProperties<KeystoreInfo>(_keystoreInfo, typeof(PlayerSettings.Android));
            }
        }
        
        private static void CopyProperties<T>(object from, object to) {
            var tFrom = DivideTypeObject(ref from);
            var tTo = DivideTypeObject(ref to);
             
            foreach (var field in typeof(T).GetProperties()) {
                var val = tFrom.GetProperty(field.Name).GetValue(from, null);
                tTo.GetProperty(field.Name).SetValue(to, val, null);
            }
        }

        private static Type DivideTypeObject(ref object obj) {
            Type type;
            if (obj is Type) {
                type = (Type) obj;
                obj = null;
            } else {
                type = obj.GetType();
            }
            return type;
        }
#endregion

        private static void KillUnityShaderCompiler() {
            foreach (var proc in Process.GetProcessesByName("UnityShaderCompiler")) {
                proc.Kill();
            }
        }
    }
}