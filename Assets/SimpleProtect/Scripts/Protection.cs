#if BUILD_PROTECT_COPY || BUILD_ACTIVATOR
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SimpleProtect {
    public static class Protection {
        public static bool ValidateKey(string key) {
            return key == GenerateKey();
        }

        public static string GenerateKey() {
            var unique = SystemInfo.deviceUniqueIdentifier;
            var s = unique + Phrase.Get() + unique;
            using (var sha = SHA512.Create()) {
                var hash = sha.ComputeHash(Encoding.Default.GetBytes(s));
                return BitConverter.ToString(hash);
            }
        }
    }
}
#endif