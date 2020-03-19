using System.Linq;
using Games.Common.Game;
using Games.Common.Navigation;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Landscape {
    public class GameAnimals : BaseGame {
        [SerializeField] private Transform _treesRoot;
        [SerializeField] private NavMeshField _navMeshField;
        [SerializeField] private bool _notChangeNavMeshDepthForDebug;
        
        private Animal[] _animals;
        private MagnetToNavMesh[] _trees;
        private bool _treesSpawned;

        protected override void Start() {
            _animals = GetComponentsInChildren<Animal>();
            _trees = _treesRoot.GetComponentsOnlyInChildren<MagnetToNavMesh>();
            SaveInitialSizes(_animals);
            SaveInitialSizes(_trees);
            base.Start();
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            SetSizes(_gameField.Scale, _animals);
            SetSizes(_gameField.Scale, _trees);
            
            if (!_notChangeNavMeshDepthForDebug) {
                _navMeshField.MinDepth = Prefs.Sandbox.PercentToDepth(Prefs.Landscape.DepthSea);
                _navMeshField.MaxDepth = Prefs.Sandbox.PercentToDepth(Prefs.Landscape.DepthMountains);
            }
            _navMeshField.AgentRadius = _animals.Average(a => math.cmin(a.transform.localScale))/2f;
            
            SpawnTrees();
            SpawnAnimals();
        }

        private void SpawnTrees() {
            foreach (var tree in _trees) {
                tree.Init(_gameField.transform);
                tree.AcceptScale(tree.transform.localScale);
                if (!_treesSpawned) {
                    tree.RandomSpawn();
                    tree.OnSpawned += () => OnTreeSpawned(tree);
                }
            }

            _treesSpawned = true;
        }

        private void OnTreeSpawned(SpawningToNavMesh tree) {
            var bounds = tree.GetComponent<Renderer>().bounds;
            foreach (var other in _trees) {
                if (tree == other)
                    continue;
                var boundsOther = other.GetComponent<Renderer>().bounds;
                if (bounds.Intersects(boundsOther)) {
                    tree.RandomSpawn();
                    return;
                }
            }
        }

        private void SpawnAnimals() {
            foreach (var animal in _animals) {
                var flying = animal.GetComponent<AnimalWalkerBezierFlying>();
                if (flying != null) {
                    flying.ZFly = -Prefs.Sandbox.OffsetMaxDepth;
                }
                var walker = animal.GetComponent<IAnimalWalker>();
                walker.Init(_gameField);
                walker.RandomSpawn();
                animal.StartAnimation();
            }
        }
    }
}