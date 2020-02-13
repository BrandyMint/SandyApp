using Launcher.KeyMapping;
using UnityEngine;

namespace Games.Common {
    public class LightControl : MonoBehaviour {
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _minAngle = 10f;
        [SerializeField] private float _maxAngle = 50f;

        private void Start() {
            KeyMapper.AddListener(KeyEvent.LEFT, MoveLeft);
            KeyMapper.AddListener(KeyEvent.RIGHT, MoveRight);
            KeyMapper.AddListener(KeyEvent.UP, MoveUp);
            KeyMapper.AddListener(KeyEvent.DOWN, MoveDown);
        }
        
        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.LEFT, MoveLeft);
            KeyMapper.RemoveListener(KeyEvent.RIGHT, MoveRight);
            KeyMapper.RemoveListener(KeyEvent.UP, MoveUp);
            KeyMapper.RemoveListener(KeyEvent.DOWN, MoveDown);
        }

        private void MoveHorizontal(float k) {
            var angle = transform.localEulerAngles;
            if (Prefs.App.FlipHorizontalSandbox ^ Prefs.App.FlipVerticalSandbox)
                k *= -1f;
            angle.y += k * _speed * Time.deltaTime;
            transform.localEulerAngles = angle;
        } 
        
        private void MoveVertical(float k) {
            var angle = transform.localEulerAngles;
            angle.x += k * _speed * Time.deltaTime;
            angle.x = Mathf.Clamp(angle.x, _minAngle, _maxAngle);
            transform.localEulerAngles = angle;
        } 

        private void MoveLeft() {
            MoveHorizontal(-1f);
        }

        private void MoveRight() {
            MoveHorizontal(1f);
        }

        private void MoveUp() {
            MoveVertical(1f);
        }

        private void MoveDown() {
            MoveVertical(-1f);
        }
    }
}