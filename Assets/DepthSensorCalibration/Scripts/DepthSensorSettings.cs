using UnityEngine;

namespace DepthSensorCalibration {
    public class KinectSettings {
        private const string PREFS_TAG = "KinectSettings.";
        public const float INITIAL_SIZE = 8.5f;

        public static float PosX {
            get { return PlayerPrefs.GetFloat(PREFS_TAG + "PosX", 0); }
            set { PlayerPrefs.SetFloat(PREFS_TAG + "PosX", value); }
        }

        public static float PosY {
            get { return PlayerPrefs.GetFloat(PREFS_TAG + "PosY", 0); }
            set { PlayerPrefs.SetFloat(PREFS_TAG + "PosY", value); }
        }

        public static float Size {
            get { return PlayerPrefs.GetFloat(PREFS_TAG + "Size", 1); }
            set { PlayerPrefs.SetFloat(PREFS_TAG + "Size", value); }
        }

        public static int ZeroDepth {
            get { return PlayerPrefs.GetInt(PREFS_TAG + "ZoneCut", 10); }
            set { PlayerPrefs.SetInt(PREFS_TAG + "ZoneCut", value); }
        }

        public static int ZoneTouch {
            get { return PlayerPrefs.GetInt(PREFS_TAG + "ZoneTouch", 50); }
            set { PlayerPrefs.SetInt(PREFS_TAG + "ZoneTouch", value); }
        }

        public static int ZoneTouchFoot {
            get { return PlayerPrefs.GetInt(PREFS_TAG + "ZoneTouchFoot", 150); }
            set { PlayerPrefs.SetInt(PREFS_TAG + "ZoneTouchFoot", value); }
        }

        public static void Save() {
            PlayerPrefs.Save();
        }
    }
}