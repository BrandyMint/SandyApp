using System.IO;
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
        
        
        /*static Deploy() {
            Clear();
            var projPath = GetProjPath();
#if UNITY_EDITOR_LINUX
            CopyFromDeploy(projPath, "Linux", true);
#endif
#if UNITY_EDITOR_WIN
            CopyFromDeploy(projPath, "Windows", true);
#endif
        }*/

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
        
        private static void CopyFromDeploy(string dstPath, string platform, bool log) {
#if ENABLE_OPENNI2
            CopyFromDeployWithAll(dstPath, platform, "OpenNI2", log);
#if ENABLE_NITE2
            CopyFromDeployWithAll(dstPath, platform, "NiTE2", log);
#endif
#endif
        }

        private static void CopyFromDeployWithAll(string dstPath, string platform, string target, bool log) {
            Debug.Log($"Deploying {target} for {platform}...");
            CopyFromDeploy(dstPath, "All", target, log);
            if (CopyFromDeploy(dstPath, platform, target, log))
                Debug.Log("Done!");
            else {
                Debug.LogWarning($"Not exist deploy {target} for {platform}");
            }
        }
        
        private static bool CopyFromDeploy(string dstPath, string platform, string target, bool log) {
            var projPath = GetProjPath();
            var srcPath = Path.Combine(projPath, "Deploy", target, platform);
            if (Directory.Exists(srcPath)) {
                var diSrc = new DirectoryInfo(srcPath);
                var diDst = new DirectoryInfo(dstPath);
                CopyRecursive(diSrc, diDst, log);
                return true;
            }
            return false;
        }

        private static void CopyRecursive(DirectoryInfo source, DirectoryInfo target, bool log) {
            using (var appendLog = log ? File.AppendText(GetLogPath()) : null) {
                foreach (var fi in source.GetFiles()) {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                    appendLog?.WriteLine(fi.Name);
                }
                
                foreach (var diSourceSubDir in source.GetDirectories()) {
                    var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyRecursive(diSourceSubDir, nextTargetSubDir, false);
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