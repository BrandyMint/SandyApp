﻿using System.Collections;
using System.Linq;
using DepthSensorCalibration;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Arithmetic {
    public class ArithmeticGame : FindObjectGame {
        [SerializeField] private ExerciveGenerator _generator;
        [SerializeField] private Camera _camSandbox;

        private int _answer;

        protected override void Start() {
            base.Start();
            _items = _tplItems.ToList();
            foreach (var item in _items) {
                item.gameObject.SetActive(true);
            }
            ShowItems(false);
        }

        protected override CameraRenderToTexture CreateRenderDepth() {
            return _camSandbox.gameObject.AddComponent<CameraRenderToTexture>();
        }

        protected override void StartGame() {
            GameScore.Score = 0;
            NextExercive();
        }

        protected override void StopGame() {
            StopCoroutine(nameof(ShowAnswer));
            ShowItems(false);
            _isGameStarted = false;
        }

        private void ShowItems(bool show) {
            foreach (var item in _items) {
                item.Show(show);
            }
        }

        private void NextExercive() {
            ShowItems(true);
            _generator.Generate(_items.Count, out _answer, out var answers);
            var i = 0;
            foreach (var answer in answers) {
                var item = _items[i++];
                item.ItemType = answer;
                item.GetComponent<RandomColorRenderer>().SetRandomColor();
            }
            _isGameStarted = true;
        }

        protected override void OnFireItem(Interactable item) {
            bool isRight = item.ItemType == _answer;
            if (isRight) {
                ++GameScore.Score;
            }
            item.Bang(isRight);
            _isGameStarted = false;
            StartCoroutine(nameof(ShowAnswer), isRight);
        }

        private IEnumerator ShowAnswer(bool isRight) {
            ShowItems(false);
            _generator.ShowAnswer(_answer, isRight);
            yield return new WaitForSeconds(_timeOffsetSpown);
            NextExercive();
        }

        protected override void OnCalibrationChanged() { }
    }
}