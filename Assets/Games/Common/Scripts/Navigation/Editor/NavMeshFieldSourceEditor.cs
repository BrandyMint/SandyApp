using UnityEditor;
using UnityEditor.AI;

namespace Games.Common.Navigation.Editor {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshFieldSourceMeshFilter))]
    public class NavMeshFieldSourceEditor : UnityEditor.Editor {
        private SerializedProperty _area;

        private void OnEnable() {
            _area = serializedObject.FindProperty("_area");
        }

        public override void OnInspectorGUI() {
            NavMeshComponentsGUIUtility.AreaPopup("Area", _area);
            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }
    }
}