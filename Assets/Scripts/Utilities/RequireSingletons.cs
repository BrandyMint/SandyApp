using System.Collections.Generic;
using UnityEngine;

namespace Utilities {
    public class RequireSingletons : MonoBehaviour {
        [SerializeField] private Object[] _singletonPrefabs;
        
        private static readonly ISet<int> _alreadyLoaded = new SortedSet<int>();

        private void Awake() {
            foreach (var singleton in _singletonPrefabs) {
                var hash = singleton.GetHashCode();
                if (!_alreadyLoaded.Contains(hash)) {
                    _alreadyLoaded.Add(hash);
                    Instantiate(singleton, null, false);
                }
            }
        }
    }
}