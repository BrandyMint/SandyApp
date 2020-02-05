using Games.Common.Game;
using UnityEngine;
using Random = UnityEngine.Random;
using Utilities;

namespace Games.Landscape {
    public class GameAnimals : BaseGame {
        [SerializeField] private Transform _treesRoot;
        
        private AbstractAnimal[] _animals;
        private Transform[] _trees;

        protected override void Start() {
            _animals = GetComponentsInChildren<AbstractAnimal>();
            _trees = _treesRoot.GetComponentsOnlyInChildren<Transform>();
            SaveInitialSizes(_animals);
            SaveInitialSizes(_trees);
            base.Start();
            SpawnAnimals();
        }
        
        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            SetSizes(_gameField.Scale, _animals);
            SetSizes(_gameField.Scale, _trees);
        }

        private void SpawnAnimals() {
            foreach (var animal in _animals) {
                var p = new Vector3(Random.value, Random.value) - Vector3.one / 2f;
                p *= 0.8f;
                var flying = animal as AnimalFlying;
                if (flying != null) {
                    p.z = flying.ZFly = -Prefs.Sandbox.OffsetMaxDepth;
                } else {
                    p.z = 0f;
                }
                animal.transform.position = _gameField.transform.TransformPoint(p);
                animal.transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), animal.transform.up);
                animal.field = _gameField;
                animal.StartAnimation();
            }
            foreach (var tree in _trees) {
                var p = new Vector3(Random.value, Random.value) - Vector3.one / 2f;
                p *= 0.5f;
                tree.transform.position = _gameField.transform.TransformPoint(p);
                tree.transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), tree.transform.up);
            }
        }
    }
}