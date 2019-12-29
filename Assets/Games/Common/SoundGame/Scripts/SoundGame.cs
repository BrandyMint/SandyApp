using System.Collections.Generic;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Common.SoundGame {
    public class SoundGame : BaseGameWithGetDepth {
        [SerializeField] private Transform _instrumentRoot;

        protected SoundKey[] _sounds;
        protected readonly HashSet<SoundKey> _soundsPlaying = new HashSet<SoundKey>();

        private void Awake() {
            _sounds = _instrumentRoot.GetComponentsInChildren<SoundKey>();
        }

        protected override void Start() {
            _testMouseModeHold = true;
            base.Start();
        }
        
        protected override void OnCalibrationChanged() {
            base.OnCalibrationChanged();
        }

        protected override void ProcessDepthFrame() {
            base.ProcessDepthFrame();
            foreach (var sound in _sounds) {
                if (!_soundsPlaying.Contains(sound))
                    sound.Stop();
            }
            _soundsPlaying.Clear();
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            var sound = item as SoundKey;
            if (sound != null)
                OnSoundItem(sound, viewPos);
        }

        private void OnSoundItem(SoundKey sound, Vector2 viewPos) {
            sound.Play();
            _soundsPlaying.Add(sound);
        }
    }
}