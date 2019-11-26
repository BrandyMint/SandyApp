using System.Collections;
using UnityEngine;

namespace Games.Eatable {
    public class LifeTime : MonoBehaviour{
        [SerializeField] public float time = 1f;

        private void Start() {
            StartCoroutine(Life());
        }

        public IEnumerator Life() {
            yield return new WaitForSeconds(time);
            Destroy(gameObject);
        }
    }
}