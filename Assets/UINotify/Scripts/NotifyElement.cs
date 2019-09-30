using System;
using UnityEngine;
using UnityEngine.UI;

namespace UINotify {
    public class NotifyElement : MonoBehaviour {
        [Serializable]
        public class StyleBind {
            public Style style;
            public GameObject obj;
        }

        [SerializeField] private StyleBind[] _styles; 
        [SerializeField] private Text _txtTittle;
        [SerializeField] private Text _txt;
        [SerializeField] private Transform _contentContainer;
        [SerializeField] private float _timeShow;
        [SerializeField] private AnimationCurve _curveShow;
        [SerializeField] private float _timeHide;
        [SerializeField] private AnimationCurve _curveHide;
        [SerializeField] private float _timeMove;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private LayoutElement _spaceLeft;
        [SerializeField] private LayoutElement _spaceRight;
        
        
        public event Action OnFinish;

        private GameObject _content;
        private float _timer;
        private float _fullTime;
        private bool _infinityTime;
        private bool _firstShow = true;
        private float _hideDistance;

        public void Set(Notify.Params p) {
            foreach (var style in _styles) {
                style.obj.SetActive(style.style == p.style);
            }
            _txtTittle.gameObject.SetActive(!string.IsNullOrEmpty(p.title));
            _txtTittle.text = p.title;
            _txt.gameObject.SetActive(!string.IsNullOrEmpty(p.text));
            _txt.text = p.text;
            
            if (_content != null) Destroy(_content);
            _content = p.content;
            _contentContainer.gameObject.SetActive(p.content != null);
            if (p.content != null) {
                p.content.transform.SetParent(_contentContainer, false);
            }

            if (p.time == LifeTime.INFINITY) {
                _timer = 0f;
                _infinityTime = true;
            } else {
                _infinityTime = false;
                _timer = (float) p.time / 1000f;
            }
            _fullTime = _timer = _timer + _timeShow + _timeHide + _timeMove;
            
            if (_firstShow) {
                SetShow(0f);
                _firstShow = false;
            } else {
                SetShow(1f);
                _timer -= _timeShow;
            }
            
            
            SetHide(0f);
            SetMove(0f);
        }

        private void Awake() {
            _hideDistance = _spaceLeft.preferredWidth + _spaceRight.preferredWidth;
        }

        public void Hide() {
            _infinityTime = false;
            if (_timer > _timeHide + _timeMove)
                _timer = _timeHide + _timeMove;
        }

        private void Update() {
            if (_timer > 0f) {
                if (_timer > _timeHide + _timeMove)
                    SetShow(Mathf.InverseLerp(_fullTime, _fullTime - _timeShow, _timer));
                else if (_timer > _timeMove)
                    SetHide(Mathf.InverseLerp(_timeHide + _timeMove, _timeMove, _timer));
                else
                    SetMove(Mathf.InverseLerp(_timeMove, 0f, _timer));
            } else {
                OnFinish?.Invoke();
                Destroy(gameObject);
            }
            if (!_infinityTime || _infinityTime && _timer > _timeHide)
                _timer -= Time.deltaTime;
        }

        private void SetShow(float k) {
            _group.alpha = _curveShow.Evaluate(k);
        }

        private void SetHide(float k) {
            var x = _curveHide.Evaluate(k);
            _spaceLeft.preferredWidth = _hideDistance * x;
            _spaceRight.preferredWidth = _hideDistance * (1f - x);
            _group.alpha = _curveHide.Evaluate(1f - k);
        }

        private void SetMove(float k) {
            var s = _curveHide.Evaluate(1f - k);
            transform.localScale = new Vector3(1f, s, 1f);
            _group.alpha = 0f;
        }
    }
}