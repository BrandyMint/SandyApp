using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;
using Object = UnityEngine.Object;

namespace Games.Common {
    [ExecuteInEditMode]
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemScale : MonoBehaviour, ISerializationCallbackReceiver {
        [Serializable] private class InitValues : SerializableDictionary<string, float> {}
        
        [SerializeField] private bool _needScaleOnUpdate;
        [SerializeField] private InitValues _initialVals = new InitValues();

        private ParticleSystem[] _particleSystems = {};

        private void Awake() {
            _particleSystems = GetComponentsInChildren<ParticleSystem>();
        }

        private void Start() {
            Scale();
        }

#if UNITY_EDITOR
        private void OnEnable() {
            if (UnityEditor.EditorApplication.isPlaying)
                return;
            Awake();
            ProcessValues(SaveCurrentVal);
        }
#endif


        private void LateUpdate() {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying || _needScaleOnUpdate) Scale();
#else
            if (_needScaleOnUpdate) Scale();
#endif
        }

        private void Scale() {
            ProcessValues(UpdateValToScale);
        }

        private void ProcessValues(Func<int, Component, string, float, float, float> Process) {
            var i = 0;
            var currScale = transform.TransformVector(Vector3.forward).magnitude;
            foreach (var system in _particleSystems) {
                var main = system.main;
                main.gravityModifierMultiplier = Process(i, system, "gravity", main.gravityModifierMultiplier, currScale);
                var emission = system.emission;
                emission.rateOverDistanceMultiplier = Process(i, system, "emission.distance", emission.rateOverDistanceMultiplier, 1f / currScale);
                ++i;
            }
        }

#if UNITY_EDITOR
        private float SaveCurrentVal(int i, Object c, string id, float defValue, float scale) {
            var hash = GetHash(i, c, id);
            return scale * Get(hash, defValue / scale);
        }
#endif

        private float UpdateValToScale(int i, Object c, string id, float defValue, float scale) {
            var hash = GetHash(i, c, id);
            return scale * Get(hash, defValue / scale);
        }

        private float Get(string id, float defValue, bool forceSave = false) {
            float val;
            if (forceSave || !_initialVals.TryGetValue(id, out val)) {
                _initialVals[id] = defValue;
                val = defValue;
            }

            return val;
        }

        private static string GetHash(int i, Object c, string id) {
            return $"{i}.{c.name}.{id}";
        }
        
        public void OnBeforeSerialize() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                return;
            _notGarbage = new List<string>();
            ProcessValues(FindGarbage);
            var allKeys = _initialVals.Keys.Except(_notGarbage).ToArray();
            foreach (var key in allKeys) {
                _initialVals.Remove(key);
            }
#endif
        }

        private List<string> _notGarbage;

        private float FindGarbage(int i, Object c, string id, float defValue, float scale) {
            var hash = GetHash(i, c, id);
            if (_initialVals.ContainsKey(hash))
                _notGarbage.Add(hash);
            return defValue;
        }

        
        public void OnAfterDeserialize() {
        }
    }
}