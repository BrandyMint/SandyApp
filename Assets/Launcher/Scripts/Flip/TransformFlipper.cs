﻿using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Launcher.Flip {
    public class TransformFlipper : MonoBehaviour {
        private Transform _flip;
        private Vector3 _initialScale;

        private void Start() {
            _flip = GetComponent<Canvas>() != null ? InitCanvasFlip() : transform;
            _initialScale = _flip.localScale;
            
            OnAppParamChanged();
            Prefs.App.OnChanged += OnAppParamChanged;
        }

        private void OnDestroy() {
            if (Prefs.App != null)
                Prefs.App.OnChanged -= OnAppParamChanged;
        }

        private RectTransform InitCanvasFlip() {
            var flip = new GameObject("Flip").AddComponent<RectTransform>();
            flip.SetParent(transform, false);
            flip.localRotation = Quaternion.identity;
            flip.localScale = Vector3.one;
            flip.localPosition = Vector3.zero;
            flip.anchorMin = Vector2.zero;
            flip.anchorMax = Vector2.one;
            flip.sizeDelta = Vector2.zero;

            foreach (var child in transform.GetComponentsOnlyInChildren<Transform>()) {
                if (child != flip) {
                    child.SetParent(flip, false);
                }
            }
            
            return flip;
        }

        private void OnAppParamChanged() {
            var scale = new float3 {
                x = Prefs.App.FlipHorizontal ? -1f : 1f,
                y = Prefs.App.FlipVertical ? -1f : 1f,
                z = 1f
            };
            _flip.localScale = _initialScale * scale;
        }
    }
}