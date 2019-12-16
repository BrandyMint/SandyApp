using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utilities.Editor {
    public static class ScriptableObjectUtility {
        public static void CreateAssetInPlace<T> (T asset = null) where T : ScriptableObject {
            var path = AssetDatabase.GetAssetPath (Selection.activeObject);
            if (path == "") {
                path = "Assets";
            } else if (Path.GetExtension (path) != "") {
                path = path.Replace (Path.GetFileName(path), "New " + typeof(T) + ".asset");
            }
 
            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path);
            if (asset == null)
                asset = ScriptableObject.CreateInstance<T> ();
            CreateAssetAndFocus(asset, assetPathAndName);
        }
        
        public static void CreateAssetAndFocus(ScriptableObject asset, string path) {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}