using System.Collections;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Common {
    public class LifeTime : MonoBehaviour{
        [SerializeField] public float time = 1f;
        [SerializeField] public float waitInteractableEffects = 0f;

        private void Start() {
            StartCoroutine(Life());
        }

        public IEnumerator Life() {
            yield return new WaitForSeconds(time);
            var interacable = GetComponent<IInteractable>();
            if (interacable != null) {
                interacable.Show(false);
                yield return new WaitForSeconds(waitInteractableEffects);
                interacable.Dead();
            } else
                Destroy(gameObject);
        }
    }
}