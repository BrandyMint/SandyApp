#if BUILD_PROTECT_COPY || BUILD_ACTIVATOR
using System;
using System.IO;
using UnityEngine;

namespace SimpleProtect {
    public static class ProtectionStore {
        private static readonly string _FILE_NAME = "protect";

        private static string GetPath() {
            return Path.Combine(Application.persistentDataPath, _FILE_NAME);
        }
        
        public static string Load() {
            try {
                return File.ReadAllText(GetPath());
            } catch (Exception) {
                return null;
            }
        }

#if BUILD_ACTIVATOR
        public static bool Save(string val) {
            try {
                File.WriteAllText(GetPath(), val);
                return true;
            } catch (Exception) {
                return false;
            }
        }
#endif
    }
}
#endif