using UnityEngine;

namespace SimpleProtect {
    public class ActivateController : MonoBehaviour {
        [SerializeField] private GameObject _onSuccess;
        [SerializeField] private GameObject _onFail;

#if BUILD_ACTIVATOR
        private void Awake() {

            var success = ProtectionStore.Save(Protection.GenerateKey());
            _onSuccess.SetActive(success);
            _onFail.SetActive(!success);
        }
#endif
    }
}
