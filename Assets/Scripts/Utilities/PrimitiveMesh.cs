using System.Collections.Generic;
using UnityEngine;

namespace Utilities {
    public static class PrimitiveMesh {
        private static Dictionary<PrimitiveType, Mesh> _primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Get(PrimitiveType type) {
            if (!_primitiveMeshes.ContainsKey(type)) {
                return CreatePrimitiveMesh(type);
            }

            return _primitiveMeshes[type];
        }

        private static Mesh CreatePrimitiveMesh(PrimitiveType type) {
            var gameObject = GameObject.CreatePrimitive(type);
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(gameObject);

            _primitiveMeshes[type] = mesh;
            return mesh;
        }
    }
}