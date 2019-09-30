using System;
using System.Collections.Generic;
using UnityEngine;

namespace UINotify {
    public class NotifyController : MonoBehaviour {
        protected internal static event Action<Notify.Control> OnFinish;
        
        [SerializeField] private NotifyElement _tplElem;

        private readonly Dictionary<Notify.Control, NotifyElement> _elements 
            = new Dictionary<Notify.Control, NotifyElement>();

        private void Awake() {
            _tplElem.gameObject.SetActive(false);
        }

        public NotifyElement CreateNew(Notify.Control control) {
            var newElem = Instantiate(_tplElem, _tplElem.transform.parent, false);
            newElem.OnFinish += () => Finished(control);
            _elements.Add(control, newElem);
            newElem.gameObject.SetActive(true);
            return newElem;
        }

        public void Hide(Notify.Control control) {
            if (_elements.TryGetValue(control, out var elem)) {
                elem.Hide();
            }
        }

        public void Set(Notify.Control control, Notify.Params p) {
            if (_elements.TryGetValue(control, out var elem)) {
                elem.Set(p);
            }
        }

        private void Finished(Notify.Control control) {
            OnFinish?.Invoke(control);
            _elements.Remove(control);
        } 
    }
}