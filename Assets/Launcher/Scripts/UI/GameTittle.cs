using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Launcher.UI {
    public class GameTittle : MonoBehaviour {
        [SerializeField] private bool _autoSetCurrent;
        [SerializeField] private bool _withNumber;

        private Action<string> _setText;

        private void InitIfNeed() {
            if (_setText != null) return;
            
            var txt = GetComponent<Text>();
            if (txt != null)
                _setText = s => txt.text = s;
            else {
                var txtMesh = GetComponent<TMP_Text>();
                if (txtMesh != null)
                    _setText = s => txtMesh.text = s;
            }
        }

        public void Start() {
            if (_autoSetCurrent) {
                Set(GamesList.GetIdCurrent());
            }
        }

        public void Set(int id) {
            InitIfNeed();
            var tittle = "";
            if (_withNumber)
                tittle += $"{id + 1}. ";
            tittle += GamesList.GetDescription(id).SceneName;
            _setText(tittle);
        }
    }
}