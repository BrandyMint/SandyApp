using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Games.Common.SoundGame;
using UnityEngine;
using Utilities;

namespace Games.Notes {
    public class GameNotes : SoundGame {
        [SerializeField] private Transform _notesRoot;
        [SerializeField] private Transform _stringsRoot;
        [SerializeField] private float _noteTime = 1.5f;
        [SerializeField] private float _trackSpeed = 1f;
        [SerializeField] private Track[] _tracks;

        private Note[] _tplNotes;
        private Renderer[] _strings;
        private readonly List<Note> _notes = new List<Note>(); 

        protected override void Awake() {
            base.Awake();
            _tplNotes = _notesRoot.GetComponentsInChildren<Note>();
            _strings = _stringsRoot.GetComponentsOnlyInChildren<Renderer>();
            foreach (var n in _tplNotes) {
                n.gameObject.SetActive(false);
            }
        }

        protected override void Start() {
            SaveInitialSizes(_tplNotes);
            base.Start();
            InteractableSimple.OnDestroyed += OnItemDestroyed;
        }

        protected override void OnDestroy() {
            InteractableSimple.OnDestroyed -= OnItemDestroyed;
            base.OnDestroy();
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            SetSizes(_gameField.Scale, _tplNotes);
        }

        protected override void StartGame() {
            base.StartGame();
            StartCoroutine(nameof(GoTrack), _tracks.Random());
        }

        protected override void StopGame() {
            StopCoroutine(nameof(GoTrack));
            base.StopGame();
            foreach (var note in _notes) {
                note.Dead();
            }
        }

        private IEnumerator GoTrack(Track track) {
            track.Scale = 1f / _trackSpeed;
            GameController.CurrStateDuration = GameController.CurrStateTimeLeft = track.FullTime + _noteTime * 2;
            track.SeekToStart();
            
            while (true) {
                var t = track.Next(out var notes);
                if (t >= 0f) {
                    yield return new WaitForSeconds(t);
                    foreach (var note in notes) {
                        var tpl = _tplNotes.First(n => n.ItemType == note);
                        Spawn(tpl);
                    }
                } else 
                    yield break;
            }
        }

        private void Spawn(Note tpl) {
            var s = _strings[tpl.ItemType - 1];
            var note = Instantiate(tpl, tpl.transform.parent, false);
            note.gameObject.SetActive(true);
            var size = _instrumentRoot.TransformVector(Vector3.up).magnitude;
            var v = s.transform.TransformVector(-Vector3.up);
            note.Go(s.bounds, v, _noteTime, size);
            _notes.Add(note);
        }

        private void OnItemDestroyed(InteractableSimple item) {
            var note = item as Note;
            if (note != null) {
                _notes.Remove(note);
            }
        }

        protected override void OnSoundItem(SoundKey sound, Vector2 viewPos) {
            if (_soundsPlaying.Contains(sound))
                return;

            if (!sound.Selected) {
                var note = _notes.FirstOrDefault(n => n.IsGoing && n.ItemType == sound.ItemType && n.IsGoodTimeToBang);
                if (note != null) {
                    ++GameScore.Score;
                    note.Bang(true);
                    sound.Play(true);
                }
                else {
                    sound.Play(false);
                }
            }
            _soundsPlaying.Add(sound);
        }
    }
}