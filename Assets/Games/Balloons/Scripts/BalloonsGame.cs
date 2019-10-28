﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Games.Balloons {
    public class BalloonsGame : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private Balloon _tplBalloon;
        [SerializeField] private Borders _borders;
        [SerializeField] private float _maxBallons = 12;
        [SerializeField] private float _timeOffsetSpown = 2f;
        [SerializeField] private float _startForce = 3f;

        private List<Balloon> _balloons = new List<Balloon>();
        
        private int _hitMask;

        private void Start() {
            _hitMask = LayerMask.GetMask("interactable");
            
            _tplBalloon.gameObject.SetActive(false);
            var size = math.cmax(_tplBalloon.transform.localScale);
            _borders.AlignToCamera(_cam, 2f);
            _borders.SetWidth(size * 2f);

            Balloon.OnDestroyed += OnBalloonDestroyed;
            Balloon.OnCollisionEntered += OnBalloonCollisionEnter;

            StartCoroutine(Spawning());
        }

        private void OnDestroy() {
            Balloon.OnCollisionEntered -= OnBalloonCollisionEnter;
            Balloon.OnDestroyed -= OnBalloonDestroyed;
        }

        private IEnumerator Spawning() {
            while (true) {
                if (_balloons.Count < _maxBallons) {
                    SpawnBalloon();
                }
                yield return new WaitForSeconds(_timeOffsetSpown);
            }
        }

        private void SpawnBalloon() {
            var stayAway = _balloons.Select(b => b.transform.position).ToArray();
            var stayAwayDist = math.cmax(_tplBalloon.transform.localScale) * 1.5f;
            if (SpawnArea.AnyGetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var newBalloon = Instantiate(_tplBalloon, worldPos, worldRot, _tplBalloon.transform.parent);
                var rigid = newBalloon.GetComponent<Rigidbody>();
                newBalloon.gameObject.SetActive(true);
                var dir = newBalloon.transform.rotation * Vector3.forward;
                rigid.AddForce(dir * _startForce);
                _balloons.Add(newBalloon);
            }
        }

        private void OnBalloonDestroyed(Balloon balloon) {
            _balloons.Remove(balloon);
        }
        
        private void OnBalloonCollisionEnter(Balloon balloon, Collision collision) {
            if (collision.collider == _borders.ExitBorder) {
                balloon.Dead();
            }
        }

        private void Update() {
            if (Input.GetMouseButtonDown(0)) {
                Fire(Input.mousePosition);
            }
        }

        private void Fire(Vector2 screenPos) {
            var ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, _cam.farClipPlane, _hitMask)) {
                var balloon = hit.collider.GetComponent<Balloon>();
                if (balloon != null) {
                    balloon.Bang();
                }
            }
        }
    }
}