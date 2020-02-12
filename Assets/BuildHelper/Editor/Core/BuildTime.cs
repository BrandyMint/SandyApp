#define WORKAROUND_ANDROID_UNITY_SHADER_COMPILER_BUG

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            RestoreAll();
        }

        private const string _SETTINGS_PATH = "ProjectSettings/ProjectSettings.asset";
        private const string _SETTINGS_TEMP_PATH = "ProjectSettings/ProjectSettings.temp";
        private const string _BUILD_HELPER_TEMP_PATH = "BuildHelper_Temp";
        private const string _VERSION_FILE_PATH = "version";
        private const string _OVERRIDE_ICONS_LOG_PATH = "override_icons_log.txt";
        private const string _EXCLUDED_LOG_PATH = "excluded_log.txt";
        private const string _REPLACE_FILE_LOG_PATH = "replaced_log.txt";
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
            if (WasOverrideIcon() || WasExcludedPath() || WasReplaceFile()) {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Implements <see cref="IPostprocessBuild.OnPostprocessBuild"/>.
        /// Is performed after all the build processes and restore <i>ProjectSettings.asset</i>.
        /// </summary>
        /// <param name="report"></param>
        public void OnPostprocessBuild(BuildReport report) {
            var path = Directory.Exists(report.summary.outputPath) 
                ? report.summary.outputPath 
                : Path.GetDirectoryName(report.summary.outputPath);
            PlaceVersionFile(path);
            RestoreAll();
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
            DeleteFileOrDirectory(settingsPathTemp);
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
            if (FileOrPathExist(settingsPathTemp)) {
                MoveFileOrDirectoryWithReplace(settingsPathTemp, settingsPath);
                Debug.Log("BuildTime: Project Settings restored");
                AssetDatabase.Refresh();
            }
            _settingsAlreadySaved = false;
            RestoreKeystoreInfo();
        }

        [MenuItem("Build/Restore")]
        public static void RestoreAll() {
            RestoreExcludedPaths();
            RestoreReplacedFiles();
            RestoreOverridenIcons();
            RestoreSettings();
            var temp = BuildHelperStrings.ProjRoot(_BUILD_HELPER_TEMP_PATH);
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
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
                RestoreAll();
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
            if (WasExcludedPath())
                SyncSolution();
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

        private void PlaceVersionFile(string path) {
            path = Path.Combine(path, _VERSION_FILE_PATH);
            File.WriteAllText(path, PlayerSettings.bundleVersion);
        }

        private static void SyncSolution() {
            EditorApplication.ExecuteMenuItem("Assets/Sync C# Project");
        }

#region OverrideIcon
        public static void OverrideIcon(string defIconPath, string overrideIconPath) {
            var tempPath = GenFileInBuildHelperTempPath();
            CopyFileOrDirectoryWithReplace(defIconPath, tempPath);
            CopyFileOrDirectoryWithReplace(overrideIconPath, defIconPath);
            LogOverrideIcon(defIconPath, tempPath);
        }
        
        private static void RestoreOverridenIcons() {
            if (WasOverrideIcon()) {
                RestoreReplaced(_OVERRIDE_ICONS_LOG_PATH);
            }
        }
        
        private static bool WasOverrideIcon() {
            var logPath = GenFileInBuildHelperTempPath(_OVERRIDE_ICONS_LOG_PATH, false);
            return File.Exists(logPath);
        } 

        private static string GenFileInBuildHelperTempPath(string fileName = null, bool createTempDir = true) {
            var tempDir = BuildHelperStrings.ProjRoot(_BUILD_HELPER_TEMP_PATH);
            if (createTempDir && !Directory.Exists(tempDir)) 
                Directory.CreateDirectory(tempDir);

            if (string.IsNullOrEmpty(fileName)) {
                var existFiles = Directory.GetFiles(tempDir);
                do {
                    fileName = UnityEngine.Random.Range(0, int.MaxValue).ToString();
                } while (existFiles.Any(file => file == fileName));
            }
            
            return Path.Combine(tempDir, fileName);
        }

        private static void LogOverrideIcon(params string[] lines) {
            var logPath = GenFileInBuildHelperTempPath(_OVERRIDE_ICONS_LOG_PATH);
            using (var sw = new StreamWriter(logPath, true)) {
                foreach (var line in lines) {
                    sw.WriteLine(line);
                }
            }
        }
#endregion

#region Exclude Path
        public static void ExcludePath(string path) {
            MoveFileOrDirectoryWithReplace(path, path + "~");
            LogExcludePath(path);
            var meta = path + ".meta";
            if (FileOrPathExist(meta)) {
                MoveFileOrDirectoryWithReplace(meta, meta + "~");
                LogExcludePath(meta);
            }
        }

        private static bool FileOrPathExist(string path) {
            return File.Exists(path) || Directory.Exists(path);
        }

        private static void MoveFileOrDirectoryWithReplace(string src, string dst) {
            DeleteFileOrDirectory(dst);
            if (File.Exists(src))
                File.Move(src, dst);
            else 
                Directory.Move(src, dst);
        }
        
        private static void CopyFileOrDirectoryWithReplace(string src, string dst) {
            DeleteFileOrDirectory(dst);
            if (File.Exists(src))
                File.Copy(src, dst, true);
            else 
                FileUtil.CopyFileOrDirectory(src, dst);
        }
        
        private static void DeleteFileOrDirectory(string path) {
            if (File.Exists(path)) {
                File.Delete(path);
            }
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }

        private static void LogExcludePath(string path) {
            var logPath = GenFileInBuildHelperTempPath(_EXCLUDED_LOG_PATH);
            using (var sw = new StreamWriter(logPath, true)) {
                sw.WriteLine(path);
            }
        }
        
        private static void RestoreExcludedPaths() {
            if (WasExcludedPath()) {
                var logPath = GenFileInBuildHelperTempPath(_EXCLUDED_LOG_PATH);
                var lines = File.ReadAllLines(logPath);
                for (int i = lines.Length - 1; i >= 0; --i) {
                    var path = lines[i];
                    MoveFileOrDirectoryWithReplace(path + "~", path);                    
                }
                File.Delete(logPath);
            }
        }
        
        private static bool WasExcludedPath() {
            var logPath = GenFileInBuildHelperTempPath(_EXCLUDED_LOG_PATH, false);
            return File.Exists(logPath);
        }
#endregion
        
#region Replace In File

        public static void ReplaceInFile(string file, string search, string replace = "") {
            ReplaceInFile(file, false, search, replace);
        }

        public static void ReplaceInFileRegex(string file, string search, string replace = "") {
            ReplaceInFile(file, true, search, replace);
        }

        public static void ReplaceInFile(string file, bool regex, string search, string replace) {
            bool needBackup = !ReplaceFileIsBackuped(file);
            var tempPath = "";
            if (needBackup) {
                tempPath = GenFileInBuildHelperTempPath();
                CopyFileOrDirectoryWithReplace(file, tempPath);
            }
            
            var text = File.ReadAllText(file);
            text = Regex.Replace(text, search, replace);
            File.WriteAllText(file, text);

            if (needBackup) {
                LogReplacedFile(file, tempPath);
            }
        }

        private static bool ReplaceFileIsBackuped(string path) {
            if (WasReplaceFile()) {
                var logPath = GenFileInBuildHelperTempPath(_REPLACE_FILE_LOG_PATH);
                var replacedList = File.ReadAllLines(logPath);
                foreach (var replaced in replacedList) {
                    if (path == replaced)
                        return true;
                }
            }

            return false;
        }
        
        private static void RestoreReplacedFiles() {
            if (WasReplaceFile()) {
                RestoreReplaced(_REPLACE_FILE_LOG_PATH);
            }
        }
        
        private static void RestoreReplaced(string logName) {
            var logPath = GenFileInBuildHelperTempPath(logName);
            var lines = File.ReadAllLines(logPath);
            for (int i = lines.Length - 1; i > 0; i -= 2) {
                MoveFileOrDirectoryWithReplace(lines[i], lines[i - 1]);          
            }
            File.Delete(logPath);
        }
        
        private static bool WasReplaceFile() {
            var logPath = GenFileInBuildHelperTempPath(_REPLACE_FILE_LOG_PATH, false);
            return File.Exists(logPath);
        }

        private static void LogReplacedFile(params string[] lines) {
            var logPath = GenFileInBuildHelperTempPath(_REPLACE_FILE_LOG_PATH);
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