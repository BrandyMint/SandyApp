using System.Collections.Generic;
using System.Linq;
using Games.Common.SoundGame;
using UnityEngine;

namespace Games.Guitar {
    public class GameGuitar : SoundGame {
        private HashSet<SoundKey> _prevSoundsPlaying = new HashSet<SoundKey>();

        protected override void Start() {
            SaveInitialSizes(_instrumentRoot);
            base.Start();
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            SetSizes(_gameField.Scale, _instrumentRoot);
            _instrumentRoot.position = _gameField.transform.position;
        }

        protected override void PostProcessDepthFrame() {
            foreach (var sound in _sounds) {
                if (!_soundsPlaying.Contains(sound))
                    sound.Selected = false;
            }

            foreach (var sound in _soundsPlaying) {
                if (sound.GetNoteName() == "0" && !_prevSoundsPlaying.Contains(sound)) {
                    var actual = _sounds.FirstOrDefault(s => s.Selected && s.ItemType == sound.ItemType && s != sound)
                                 ?? sound;
                    actual.Selected = false;
                    actual.Play();
                }
            }

            _prevSoundsPlaying = _soundsPlaying;
            _soundsPlaying = new HashSet<SoundKey>();
        }

        protected override void OnSoundItem(SoundKey sound, Vector2 viewPos) {
            if (_soundsPlaying.Contains(sound))
                return;
            
            var soundId = int.Parse(sound.GetNoteName());
            if (soundId != 0) {
                var other = _soundsPlaying.FirstOrDefault(s => s.ItemType == sound.ItemType && s.GetNoteName() != "0");
                if (other != null) {
                    var otherId = int.Parse(other.GetNoteName());
                    if (otherId < soundId) {
                        sound = null;
                    } else {
                        other.Selected = false;
                        _soundsPlaying.Remove(other);
                    }
                }
            }
            if (sound != null) {
                sound.Selected = true;
                _soundsPlaying.Add(sound);
            }
        }
    }
}