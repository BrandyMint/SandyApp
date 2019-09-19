using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DepthSensor.Editor {
    [InitializeOnLoad]
    public class Deploy : IPostprocessBuildWithReport {
        private static string _DEPLOY_PATH = "Deploy";
        private static string _LOG_PATH = "Logs/Deploy.log";
        private static string[] _PLATFROMS = {"Linux", "Windows"};
        private static string[] _LIB_EXTENSIONS = {".dll", ".so"};
        
        static Deploy() {
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode)
                    return;
                var projPath = GetProjPath();
#if UNITY_EDITOR_LINUX
                CopyFromDeploy(projPath, "Linux", true, false);
#endif
#if UNITY_EDITOR_WIN
                CopyFromDeploy(projPath, "Windows", true, false);
#endif
            };
            
        }

        private static string GetProjPath() {
            return Directory.GetCurrentDirectory();
        }

        public static void ForBuild(string dstPath, BuildTarget target) {
            var targetStr = target.ToString();
            foreach (var platform in _PLATFROMS) {
                if (targetStr.Contains(platform))
                    CopyFromDeploy(dstPath, platform, false);
            }
        }

        [MenuItem("Build/Clear Deployed for Editor")]
        public static void Clear() {
            var logPath = GetLogPath();
            var projPath = GetProjPath();
            if (File.Exists(logPath)) {
                Debug.Log("Clear old deploy...");
                foreach (var path in File.ReadAllLines(logPath)) {
                    var fullPath = Path.Combine(projPath, path);
                    if (Directory.Exists(fullPath))
                        Directory.Delete(fullPath, true);
                    else if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
                File.Delete(logPath);
                Debug.Log("Done!");
            }
        }
        
        private static void CopyFromDeploy(string dstPath, string platform, bool log, bool overwrite = true) {
#if !DISABLE_OPENNI2
            CopyFromDeployWithAll(dstPath, platform, "OpenNI2", log, overwrite);
#if ENABLE_NITE2
            CopyFromDeployWithAll(dstPath, platform, "NiTE2", log, overwrite);
#endif
#endif
        }

        private static void CopyFromDeployWithAll(string dstPath, string platform, string target, bool log, bool overwrite) {
            Debug.Log($"Deploying {target} for {platform}...");
            CopyFromDeploy(dstPath, "All", target, log, overwrite);
            if (CopyFromDeploy(dstPath, platform, target, log, overwrite))
                Debug.Log("Done!");
            else {
                Debug.LogWarning($"Not exist deploy {target} for {platform}");
            }
        }
        
        private static bool CopyFromDeploy(string dstPath, string platform, string target, bool log, bool overwrite) {
            var projPath = GetProjPath();
            var srcPath = Path.Combine(projPath, "Deploy", target, platform);
            if (Directory.Exists(srcPath)) {
                var diSrc = new DirectoryInfo(srcPath);
                var diDst = new DirectoryInfo(dstPath);
                CopyRecursive(diSrc, diDst, log, overwrite);
                return true;
            }
            return false;
        }

        private static void CopyRecursive(DirectoryInfo source, DirectoryInfo target, bool log, bool overwriteLibs) {
            using (var appendLog = log ? File.AppendText(GetLogPath()) : null) {
                foreach (var fi in source.GetFiles()) {
                    var destination = Path.Combine(target.FullName, fi.Name);
                    if (File.Exists(destination) && _LIB_EXTENSIONS.Contains(fi.Extension) && !overwriteLibs)
                        continue;

                    fi.CopyTo(destination, true);
                    appendLog?.WriteLine(fi.Name);
                }
                
                foreach (var diSourceSubDir in source.GetDirectories()) {
                    var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyRecursive(diSourceSubDir, nextTargetSubDir, false, overwriteLibs);
                    appendLog?.WriteLine(diSourceSubDir.Name);
                }
            }
        }

        private static string GetLogPath() {
            var projPath = GetProjPath();
            var logPath = Path.Combine(projPath, _LOG_PATH);
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            return logPath;
        }

        public int callbackOrder => -1;
        public void OnPostprocessBuild(BuildReport report) {
            ForBuild(Path.GetDirectoryName(report.summary.outputPath), report.summary.platform);
        }
    }
}