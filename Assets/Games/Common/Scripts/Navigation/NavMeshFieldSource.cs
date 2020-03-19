using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Games.Common.Navigation {
    public abstract class NavMeshFieldSource : MonoBehaviour {
        [HideInInspector]
        [SerializeField] protected int _area;
        
        protected static readonly List<NavMeshFieldSource> _sources = new List<NavMeshFieldSource>();

        protected virtual void Awake() {
            _sources.Add(this);
        }

        protected void OnDestroy() {
            _sources.Remove(this);
        }

        public static void Collect(ref List<NavMeshBuildSource> sources) {
            sources.Clear();

            foreach (var s in _sources) {
                if (s != null && s.GetBuildSource(out var build))
                    sources.Add(build);
            }
        }

        protected abstract bool GetBuildSource(out NavMeshBuildSource source);
    }
}