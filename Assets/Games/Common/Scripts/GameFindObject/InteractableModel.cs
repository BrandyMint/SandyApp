﻿using UnityEngine;

namespace Games.Common.GameFindObject {
    public class InteractableModel : Interactable {
        [SerializeField] private GameObject _model;

        protected override void Awake() {
            _r = _model.GetComponent<Renderer>();
        }
        
        public override void Bang(bool isRight) {
            Show(false);
            StartCoroutine(PlayParticlesAndDead(isRight ? _rightBang : _wrongBang));
        }
        
        public override void Dead() {
            Show(false);
            base.Dead();
        }
        
        public override void Show(bool show) {
            _model.SetActive(show);
        }
    }
}