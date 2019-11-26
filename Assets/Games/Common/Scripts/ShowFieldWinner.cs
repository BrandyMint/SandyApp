using System;
using Games.Common.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Common {
    [RequireComponent(typeof(Graphic))]
    public class ShowFieldWinner : MonoBehaviour {
        private static readonly int _WINNER = Shader.PropertyToID("_Winner");
        private Material _mat;
        

        private void Awake() {
            var img = GetComponent<Graphic>();
            _mat = img.material = Instantiate(img.material);
            GameEvent.OnCountdown += OnCountdown;
        }

        private void OnDestroy() {
            GameEvent.OnCountdown -= OnCountdown;
        }

        private void OnCountdown() {
            _mat.SetFloat(_WINNER, -1f);
        }

        private void OnEnable() {
            _mat.SetFloat(_WINNER, (float) GameScore.GetWinner() / GameScore.PlayerScore.Count);
        }
    }
}