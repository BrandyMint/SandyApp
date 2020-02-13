using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Games.Notes {
    [CreateAssetMenu(fileName = "Track", menuName = "Custom Objects/Track")]
    public class Track : ScriptableObject {
        [Serializable]
        public class Note {
            public string t;
            public int note;
            public string name {
                get => t;
            }
        }

        [SerializeField] private string _timeFormat = @"m\:s\.F";
        [SerializeField] private int _repeat = 1;
        [SerializeField] private float _repeatOffset = 1.5f;
        [SerializeField] private Note[] _notes;

        private float _scale;
        
        public float Scale {
            get => _scale;
            set => _scale = value;
        }

        public float FullTime => GetSeconds(_notes.Last()) * _repeat + _repeatOffset * _scale * (_repeat - 1);

        private int _i;
        private int _iRepeat;
        private float _t;

        public void SeekToStart() {
            _i = 0;
            _iRepeat = 0;
            _t = 0f;
        }

        public float Next(out IEnumerable<int> notes) {
            var repeatOffset = 0f;
            if (_i >= _notes.Length) {
                _i = 0;
                _t = 0;
                ++_iRepeat;
                repeatOffset = _repeatOffset * _scale;
            }
            if (_iRepeat >= _repeat) {
                notes = null;
                return -1f;
            }
            
            var n = _notes[_i];
            var offset = GetSeconds(n) - _t;
            notes = GetNotesAtTime(n);
            _t += offset;
            return offset + repeatOffset;
        }

        private IEnumerable<int> GetNotesAtTime(Note n) {
            var nn = n;
            do {
                yield return nn.note;
            } while (++_i < _notes.Length && (nn = _notes[_i]).t == n.t);
        }

        private float GetSeconds(Note n) {
            return GetSeconds(n.t);
        }
        
        private float GetSeconds(string s) {
            if (TimeSpan.TryParseExact(s, _timeFormat, CultureInfo.InvariantCulture, out var t)) {
                return (float) t.TotalSeconds * _scale;
            } else {
                Debug.LogError($"Fail read time {s} in {_i}");
                return 0f;
            }
        }
    }
}