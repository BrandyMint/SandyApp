using UnityEditor;
using UnityEngine;

namespace BuildHelper.Editor.Core {
    public class SearchVersionWindow : EditorWindow {
        private string _txtVersion = "";
        private int _count = 5;
        private bool _byRevision;

        [MenuItem("Build/Find version")]
        public static void ShowWindow() {
            GetWindow<SearchVersionWindow>("Find version");
        }

        private void OnGUI() {
            _byRevision = GUILayout.Toggle(_byRevision, "ByRevision");
            if (_byRevision) {
                EditorGUILayout.LabelField(string.Format(
@"Type the last part of version: e.g. 
    if full version is '2.1.234' - type '234';
    if '2.1.{0}1685c5a' - type '{0}1685c5a'."
                    , BuildHelperStrings.PREFIX_DEVELOP), EditorStyles.helpBox);
            }
            
            _txtVersion = EditorGUILayout.TextField("Generated version", _txtVersion);
            _count = EditorGUILayout.IntSlider("Log length", _count, 1, 100);
            if (GUILayout.Button("Search")) {
                string found = null;
                if (_byRevision) {
                    string branch;
                    string rev = _txtVersion;
                    if (rev.StartsWith(BuildHelperStrings.PREFIX_DEVELOP)) {
                        rev = rev.Substring(BuildHelperStrings.PREFIX_DEVELOP.Length);
                        branch = null;
                    }
                    else {
                        branch = BuildHelperStrings.RELEASE_BRANCH;
                    }
                    found = GitRequest.FindRevision(rev, _count, branch);
                } else {
                    found = GitRequest.FindVersion(_txtVersion, _count);
                }

                if (string.IsNullOrEmpty(found)) 
                    found = " not found";
                Debug.LogFormat("Version {0}:\n{1}",  _txtVersion, found);
            }
        }
    }
}