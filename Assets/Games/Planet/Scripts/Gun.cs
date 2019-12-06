using System;
using System.Collections;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;

namespace Games.Planet {
    public class Gun : MonoBehaviour {
        [SerializeField] private float _firstFireTime = 3f;
        [SerializeField] private float _fireTime = 1f;
        [SerializeField] private Transform _target;
        [SerializeField] private Bullet _tplBullet;

        private void Awake() {
            _tplBullet.gameObject.SetActive(false);
        }

        private void Start() {
            StartCoroutine(Attacking());
        }

        private IEnumerator Attacking() {
            yield return new WaitForSeconds(_firstFireTime);
            while (true) {
                if (GameEvent.Current == GameState.START)
                    Fire();
                yield return new WaitForSeconds(_fireTime);
            }
        }

        private void Fire() {
            var pos = _tplBullet.transform.position;
            var rot = Quaternion.LookRotation(_target.position - pos);
            var scale = _tplBullet.transform.lossyScale;
            var bullet = Instantiate(_tplBullet, pos, rot, null);
            bullet.transform.localScale = scale;
            bullet.speed *= math.cmax(scale);
            bullet.gameObject.SetActive(true);
        }
    }
}