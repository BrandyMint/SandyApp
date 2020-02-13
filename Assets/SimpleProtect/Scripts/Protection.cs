#if BUILD_PROTECT_COPY || BUILD_ACTIVATOR
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SimpleProtect {
    public static class Protection {
        public static bool ValidateKey(string key) {
            return key == GenerateKey();
        }

        public static string GenerateKey() {
            var unique = GetDeviceUniqueId();
            var s = unique + Phrase.Get();
            using (var sha = SHA512.Create()) {
                var hash = sha.ComputeHash(Encoding.Default.GetBytes(s));
                return BitConverter.ToString(hash);
            }
        }

        private static string GetDeviceUniqueId() {
            var id = SystemInfo.deviceUniqueIdentifier;
#if UNITY_STANDALONE_LINUX
            //TODO: SystemInfo.deviceUniqueIdentifier not unique in Linux, workaround with 'cat /etc/fstab'  
            try {
                var p = new Process {
                    StartInfo = {
                        FileName = "cat",
                        Arguments = "/etc/fstab",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                p.Start();
                p.WaitForExit(10000);
                id += p.StandardOutput.ReadToEnd();
            } catch {
                // ignored
            }
#endif
            return id;
        }
    }
}
#endif