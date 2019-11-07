﻿using System;
using BuildHelper.Editor.Core;
using UnityEditor;
 using UnityEngine;

 namespace BuildHelper.Editor {
    public static class UserBuildCommands {
#region Build Menu
        [MenuItem("Build/Build In Develop Win64")]
        public static void BuildInDevelop() {
            BuildInDevelop(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Build/Build Win64")]
        public static void BuildWin64() {
            Build(BuildTarget.StandaloneWindows64);
        }
        
        [MenuItem("Build/Build Linux")]
        public static void BuildLinux() {
            Build(BuildTarget.StandaloneLinux64);
        }
        
        [MenuItem("Build/Build Both")]
        public static void BuildBoth() {
            Build(BuildTarget.StandaloneWindows64);
            Build(BuildTarget.StandaloneLinux64);
        }

        /*[MenuItem("Build/Build all from master branch")]
        public static void BuildAllFromMaster() {
            var branch = GitRequest.CurrentBranch();
            if (branch != BuildHelperStrings.RELEASE_BRANCH)
                GitRequest.Checkout(BuildHelperStrings.RELEASE_BRANCH);

            try {
            } finally {
                if (branch != BuildHelperStrings.RELEASE_BRANCH)
                    GitRequest.Checkout(branch);
            }
        }*/

#endregion

#region Utility functions
        public static void BuildInDevelop(BuildTarget target) {
            Build(target, null, (t, options) => {
                var buildGroup = BuildPipeline.GetBuildTargetGroup(target);
                AddScriptingDefine(buildGroup, "IN_DEVELOP");
            });
        }

        public static void Build(string outputPath = null, Action<BuildTarget, BuildPlayerOptions> customize = null) {
            var target = EditorUserBuildSettings.activeBuildTarget;
            Build(target, outputPath, customize);
        }

        private static void Build(BuildTarget target, string outputPath = null, Action<BuildTarget, BuildPlayerOptions> customize = null) {
            if (Application.isBatchMode) {
                outputPath = GetCmdArg("-outputPath");
                var targetStr = GetCmdArg("-target");
                if (Enum.TryParse(targetStr, out BuildTarget targetOverride))
                    target = targetOverride;
            }
            
            Debug.Log($"Selected build target: {target}");
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var options = GetStandardPlayerOptions(target);
                options.locationPathName = BuildHelperStrings.GetBuildPath(target, buildVersion, outputPath);
                customize?.Invoke(target, options);

                if (target == BuildTarget.StandaloneLinux64) {
                    var buildGroup = BuildPipeline.GetBuildTargetGroup(target);
                    AddScriptingDefine(buildGroup, "USE_MAT_ASYNC_SET");
                }
                BuildTime.Build(options);
            });
        }

        private static string GetCmdArg(string key) {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) {
                if (args[i] == key) {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static void AddScriptingDefine(BuildTargetGroup buildGroup, string addDefines) {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
            if (!string.IsNullOrEmpty(defines)) defines += ";";
            defines += addDefines;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, defines);
        }

        /// <summary>
        /// Create default <i>BuildPlayerOptions</i> with specified target and return it.
        /// This options include all scenes that defined in Build Settings window.
        /// </summary>
        /// <param name="target">build target</param>
        /// <returns>new <i>BuildPlayerOptions</i></returns>
        private static BuildPlayerOptions GetStandardPlayerOptions(BuildTarget target) {
            return new BuildPlayerOptions {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                target = target,
                options = BuildOptions.None
            };
        }
#endregion
    }
}