using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HumanCollider {
    public class ColliderMonitor : MonoBehaviour {
        [SerializeField] private Color[] _colors = {
            Color.clear,
            Color.red,
            Color.green,
            Color.yellow,
            Color.blue
        };
        [SerializeField] private FilterMode _filter = FilterMode.Point;
        
        private RawImage image;
        private HumanMaskCreater hmc;
        private int _taskId;
        private MaskSnapshot maskSnapshot;
        
        private struct MaskSnapshot {
            public int width, height;
            public byte[] arr;
        }

        void Start() {
            image = GetComponent<RawImage>();
            image.material.SetTexture("_Colors", CreateColorsTexture());
            image.enabled = true;

            hmc = HumanMaskCreater.GetInstance();
            SetupKinectConveyer();
        }

        private Texture CreateColorsTexture() {
            int n = 256;
            var sampleColors = new Color[n];
            for (int i = 0; i < _colors.Length && i < sampleColors.Length; ++i) {
                sampleColors[i] = _colors[i];
            }
            var t = new Texture2D(n, 1, TextureFormat.RGBA32, false) {
                filterMode = _filter
            };
            t.SetPixels(sampleColors);
            t.Apply();
            return t;
        }

        private void SetupKinectConveyer() {
            if (hmc != null) {
                _taskId = hmc.GetKinectConveyer()
                    .AddToBG(
                        GetType().Name + "BG", typeof(HumanMaskCreater).Name,
                        ConveyerBGUpdate());
                hmc.GetKinectConveyer()
                    .AddToMainThread(
                        GetType().Name + "Main", GetType().Name + "BG",
                        ConveyerMainUpdate());
            }
        }

        private void OnEnable() {
            SetupKinectConveyer();
        }

        private void OnDisable() {
            if (hmc) {
                hmc.GetKinectConveyer().RemoveTask(_taskId);
            }
        }

        private IEnumerator ConveyerBGUpdate() {
            while (hmc.GetHumanMask() == null)
                yield return null;
            maskSnapshot.arr = new byte[hmc.GetHumanMask().arr.Length];
            while (true) {
                var mask = hmc.GetHumanMask();
                maskSnapshot.width = mask.width;
                maskSnapshot.height = mask.height;
                lock (maskSnapshot.arr.SyncRoot) {
                    if (maskSnapshot.arr.Length != mask.arr.Length) {
                        maskSnapshot.arr = new byte[mask.arr.Length];                        
                    }
                    Array.Copy(mask.arr, maskSnapshot.arr, mask.arr.Length);
                }
                yield return null;
            }
        }
        
        private IEnumerator ConveyerMainUpdate() {
            while (maskSnapshot.arr == null)
                yield return null;
            while (true) {
                lock (maskSnapshot.arr.SyncRoot) {
                    var tex = image.texture as Texture2D;
                    if (tex == null) {
                        tex = new Texture2D(maskSnapshot.width, maskSnapshot.height, TextureFormat.Alpha8, false) {
                            filterMode = _filter
                        };
                        image.texture = tex;
                        SetTransformSize(maskSnapshot.width, maskSnapshot.height);
                    }
                    if (tex.width != maskSnapshot.width || tex.height != maskSnapshot.height) {
                        tex.Resize(maskSnapshot.width, maskSnapshot.height, TextureFormat.Alpha8, false);
                        SetTransformSize(maskSnapshot.width, maskSnapshot.height);
                    }
                    if (tex.filterMode != _filter) {
                        tex.filterMode = _filter;
                        image.material.GetTexture("_Colors").filterMode = _filter;
                    }
                    tex.LoadRawTextureData(maskSnapshot.arr);
                    tex.Apply();
                }
                yield return null;
            }
        }

        private void SetTransformSize(float w, float h) {
            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);
        }
    }
}