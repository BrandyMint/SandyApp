﻿using System;
using BuildHelper.Editor.Core;
using UnityEditor;
 using UnityEditor.Build.Content;

 namespace BuildHelper.Editor {
    public static class UserBuildCommands {
#region Build Menu
        [MenuItem("Build/Build In Develop")]
        public static void BuildInDevelop() {
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var target = EditorUserBuildSettings.activeBuildTarget;
                var options = GetStandartPlayerOptions(target);
                var buildGroup = BuildPipeline.GetBuildTargetGroup(target);
                
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
                if (!string.IsNullOrEmpty(defines)) defines += ";";
                defines += "IN_DEVELOP"; 
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, defines);
                
                options.locationPathName = BuildHelperStrings.GetBuildPath(target, buildVersion);
                BuildTime.Build(options);                
            });
        }

        [MenuItem("Build/Build Win64")]
        public static void BuildWin64ToPathWithVersion() {
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var target = BuildTarget.StandaloneWindows64;
                var options = GetStandartPlayerOptions(target);
                options.locationPathName = BuildHelperStrings.GetBuildPath(target, buildVersion);
                BuildTime.Build(options);                
            });
        }

        /*[MenuItem("Build/Build all from master branch")]
        public static void BuildAllFromMaster() {
            var branch = GitRequest.CurrentBranch();
            if (branch != BuildHelperStrings.RELEASE_BRANCH)
                GitRequest.Checkout(BuildHelperStrings.RELEASE_BRANCH);

            try {
                BuildAndroidWithSign();
                BuildIOSToPathWithVersion();
            } finally {
                if (branch != BuildHelperStrings.RELEASE_BRANCH)
                    GitRequest.Checkout(branch);
            }
        }*/

        #endregion

#region Post build async operations
        /// <seealso cref="UserBuildCommands.InstallApkToDeviceAndRun"/>
        [Serializable]
        public class InstallApkParams {
            public string pathToApk;
            public string adbDeviceId;
            public string appIdentifier;
        }

        /// <summary>
        /// Install apk on android device and run it.
        /// </summary>
        /// <param name="p">Install params.</param>
        public static void InstallApkToDeviceAndRun(InstallApkParams p) {
            var appIdentifier = p.appIdentifier != null ? p.appIdentifier 
                : PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            AdbRequest.InstallToDevice(p.pathToApk, p.adbDeviceId, OnDone: success => {
                if (success)
                    AdbRequest.RunOnDevice(appIdentifier, p.adbDeviceId);
            });
        }
#endregion

#region Utility functions
        private static readonly InstallApkParams _installApkParams = new InstallApkParams();
        private static void BuildAndroidBase(bool run, Action<string, BuildPlayerOptions> customizeBuild, bool forceIL2CPP = false) {
            Action<string> build = adbDeviceId => {
                BuildTime.RestoreSettingsIfFailed(() => {
                    _installApkParams.adbDeviceId = adbDeviceId;
                    var buildVersion = BuildHelperStrings.GetBuildVersion();
                    var target = BuildTarget.Android;
                    var options = GetStandartPlayerOptions(target);
                    if (forceIL2CPP)
                        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    customizeBuild(buildVersion, options);
                    if (run)
                        PostBuildExecutor.Make(InstallApkToDeviceAndRun, _installApkParams);
                });
            };
            IdentifierFormWindow.ShowIfNeedChange(BuildTargetGroup.Android, () => {
                if (run) 
                    AndroidDevicesWindow.ShowIfNeedSelect(build);
                else 
                    build(null);
            });
        }
        
        private static void SignAndroidPackage(bool fill, string keystore, string alias) {
            if (fill) {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = BuildHelperStrings.ProjRoot("Keystore/" + keystore);
                PlayerSettings.Android.keystorePass = "mmnc1c2c3c4";
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = "mmnc1c2c3c4";
            } else {
                PlayerSettings.Android.keystoreName = null;
                PlayerSettings.Android.keyaliasName = null;
            }
        }
        
        private static string BuildAndroidPaid(string buildVersion, BuildPlayerOptions options, 
            AndroidArchitecture device, string additionalDefines = null, bool signature = false)
        {
            SignAndroidPackage(signature, "key.keystore", "butterflies");
            if (additionalDefines != null)
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, additionalDefines);
            return BuildAndroidForDevice(device, buildVersion, options);
        }
        
        /*private static string BuildAndroidFree(string buildVersion, BuildPlayerOptions options, 
            AndroidTargetDevice device, string additionalDefines = null, bool signature = false)
        {
            BuildTime.SaveSettingsToRestore();
            SignAndroidPackage(signature, "vr_editor_free.keystore", "vr_molecules_editor_free");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "APP_IS_FREE" 
                + (additionalDefines == null ? "" : ";" + additionalDefines));
            var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) + "_free";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
            PlayerSettings.productName += " Free";
            BuildTime.OverrideIcon("Assets/Editor/Icon/icon.png", "Assets/Editor/Icon/icon_free.png");
            return BuildAndroidForDevice(device, buildVersion, options);
        }*/
        
        /// <summary>
        /// Create default <i>BuildPlayerOptions</i> with specified target and return it.
        /// This options include all scenes that defined in Build Settings window.
        /// </summary>
        /// <param name="target">build target</param>
        /// <returns>new <i>BuildPlayerOptions</i></returns>
        private static BuildPlayerOptions GetStandartPlayerOptions(BuildTarget target) {
            return new BuildPlayerOptions {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                target = target,
                options = BuildOptions.None
            };
        }

        /// <summary>
        /// Build project for Android with specified AndroidTargetDevice.
        /// This function change PlayerSettings and restore it after build.
        /// Different devices will get different bundle version code.
        /// See: <see cref="BuildHelperStrings.GenBundleNumber(UnityEditor.AndroidTargetDevice)"/> 
        /// </summary>
        /// <param name="device">Android target device</param>
        /// <param name="buildVersion">Build version wich will be available by <i>Application.version</i></param>
        /// <param name="options">Build player options</param>
        /// <returns>Build path</returns>
        private static string BuildAndroidForDevice(AndroidArchitecture device, string buildVersion, BuildPlayerOptions options) {
            BuildTime.SaveSettingsToRestore();
            if (device == AndroidArchitecture.ARM64)
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = device;
            PlayerSettings.Android.bundleVersionCode = BuildHelperStrings.GenBundleNumber(device);
            var buildPath = BuildHelperStrings.GetBuildPath(BuildTarget.Android, buildVersion, specifyName: device.ToString());
            options.locationPathName = buildPath;
            _installApkParams.pathToApk = buildPath;
            _installApkParams.appIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            BuildTime.Build(options);
            return buildPath;
        }
#endregion
    }
}