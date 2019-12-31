using System;
using System.Collections;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Mahjong {
    public class Card : InteractableModel {
        public static event Action<Card> OnShowed;
        
        private static readonly int _SHOW = Animator.StringToHash("show");
        private static readonly int _HIDE = Animator.StringToHash("hide");
        
        private Animator _anim;
        private bool _isShowing;

        protected override void Awake() {
            _anim = GetComponent<Animator>();
            base.Awake();
        }

        public override void Show(bool show) {
            _isShowing = show;
            if (show) {
                _anim.SetTrigger(_SHOW);
                SetInteractable(false);
            } else {
                _anim.SetTrigger(_HIDE);
                SetInteractable(true);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void OnShowedEvent() {
            if (_isShowing)
                OnShowed?.Invoke(this);
        }

        private void SetInteractable(bool interactable) {
            _model.GetComponentInChildren<Collider>().enabled = interactable;
        }

        public override void Bang(bool isRight) {
            (isRight ? _rightBang : _wrongBang).Play();
            PlayAudioBang(isRight);
        }

        public void SetTexture(Texture t) {
            var r = _model.GetComponentInChildren<Renderer>();
            var materials = r.materials;
            var mat = new Material(materials[1]) {mainTexture = t};
            materials[1] = mat;
            r.materials = materials;
        }
    }
}