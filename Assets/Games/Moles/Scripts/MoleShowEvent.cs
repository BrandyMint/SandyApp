using System;
using UnityEngine;

namespace Games.Moles {
    public class MoleShowEvent : MonoBehaviour {
        public event Action OnShow;
        
        private void OnShowEvent() {
            OnShow?.Invoke();
        }
    }
}