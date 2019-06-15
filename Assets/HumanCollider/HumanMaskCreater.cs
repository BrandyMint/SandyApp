using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BgConveyer;
using DepthSensor;
using Launcher.KinectCalibration;
using UnityEngine;
using Joint = DepthSensor.Sensor.Joint;

namespace HumanCollider {
    public class HumanMaskCreater : MonoBehaviour {
        public ushort zoneCut;
        public ushort zoneTouch;
        public ushort zoneTouchFoot;

        private const ushort MAX_COLOR_NEIGHTBORS = 3;
        private const ushort DEPTH_SNAPSHOTS_COUNT = 10;
        private const ushort MAX_WALL_DIST = 10000;
        private const ushort MAX_COLORING_WAVES = 150;
        private const float FOOT_RADIUS_SQR = 300.0f;
        private const bool USE_COLORING = true;
        private const bool COLORING_EXTRA_DEPTH = true;

        private class CalcTempData {
            public byte[] neightbors;
            public ushort[] depth;
            public ArrayIntQueue queue;
            public HumanMask mask;
            public ushort[] staticDepth;
            public List<Vector2> footPoses;
        }

        private CalcTempData calcTemp;
        private DepthSensorManager dsm = null;
        private StaticDepthScene staticDepth = null;
        private const byte BODY_INDEX_EMPTY = 255;
        private static HumanMaskCreater instance = null;
        private KinectConveyer kinConv = null;
        private readonly Joint.Type[] FOOT_JOINTS = {
            Joint.Type.ANKLE_RIGHT,
            Joint.Type.FOOT_RIGHT,
            Joint.Type.ANKLE_LEFT,
            Joint.Type.FOOT_LEFT
        };

        void Awake() {
            instance = this;
            kinConv = gameObject.AddComponent<KinectConveyer>();
        }

        public static HumanMaskCreater GetInstance() {
            return instance;
        }

        void Start() {
            zoneCut = (ushort) KinectSettings.ZoneCut;
            zoneTouch = (ushort) KinectSettings.ZoneTouch;
            zoneTouchFoot = (ushort) KinectSettings.ZoneTouchFoot;

            calcTemp = new CalcTempData();
            calcTemp.neightbors = null;
            calcTemp.queue = new ArrayIntQueue();
            calcTemp.mask = new HumanMask();
            calcTemp.footPoses = new List<Vector2>();

            staticDepth = gameObject.AddComponent<StaticDepthScene>();
            dsm = DepthSensorManager.Instance;
            if (dsm != null)
                dsm.OnInitialized += OnDepthSensorAvailable;
        }

        private void OnDestroy() {
            if (dsm != null)
                dsm.OnInitialized -= OnDepthSensorAvailable;
                dsm.OnInitialized -= OnDepthSensorAvailable;
        }

        private void OnDepthSensorAvailable() {
            dsm.Device.Depth.Active = true;
            dsm.Device.Index.Active = true;
            dsm.Device.Body.Active = true;
            staticDepth.ReCreate(DEPTH_SNAPSHOTS_COUNT, DepthToColorChanged);
            kinConv.AddToBG(GetType().Name, null, ConveyerUpdate());
        }

        private IEnumerator ConveyerUpdate() {
            while (staticDepth.GetData() == null)
                yield return null;
            
            var sDepth = dsm.Device.Depth;
            var sIndex = dsm.Device.Index;
            while (true) {
                GetInitinalTempData(sDepth.width, sIndex.height, 
                    sDepth.data, sIndex.data, calcTemp);
                if (USE_COLORING)
                    CalcHumanMask(calcTemp);
                yield return null;
            }
        }

        public void CreateStaticSceneDepth() {
            staticDepth.ReCreate(DEPTH_SNAPSHOTS_COUNT, DepthToColorChanged);
        }

        public KinectConveyer GetKinectConveyer() {
            return kinConv;
        }

        public HumanMask GetHumanMask() {
            if (calcTemp.mask == null || calcTemp.mask.arr == null)
                return null;
            return calcTemp.mask;
        }

        public void SetBorders(float w1, float h1, float w2, float h2) {
            calcTemp.mask.SetBorders(w1, h1, w2, h2);
        }

        private void GetInitinalTempData(int w, int h, ushort[] depth, byte[] bodyIndex, CalcTempData calcTemp) {
            int l = bodyIndex.Length;
            if (calcTemp.neightbors == null || calcTemp.neightbors.Length != l)
                calcTemp.neightbors = new byte[l];
            if (calcTemp.mask.width != w || calcTemp.mask.height != h) {
                calcTemp.mask.SetDimens(w, h);
            }
            if (calcTemp.queue.MaxSize != l)
                calcTemp.queue.MaxSize = l;

            calcTemp.depth = depth;
            calcTemp.queue.Clear();
            calcTemp.staticDepth = staticDepth.GetData();
            SetFootPoses(calcTemp.footPoses);

            Parallel.ForEach(calcTemp.mask.GetIndexesInBorders(), (i) => {
            //foreach (int i in calcTemp.mask.GetIndexesInBorders()) {
                calcTemp.neightbors[i] = 0;
                if (bodyIndex[i] != BODY_INDEX_EMPTY) {
                    byte d = SliceByDepth(calcTemp.depth[i], i, calcTemp, false);
                    if (d == HumanMask.NEAR) {
                        calcTemp.mask.arr[i] = HumanMask.EMPTY;
                    } else {
                        calcTemp.mask.arr[i] = d;
                        Monitor.Enter(calcTemp.queue);
                        calcTemp.queue.Enqueue(i);
                        Monitor.Exit(calcTemp.queue);
                    }
                } else {
                    calcTemp.mask.arr[i] = HumanMask.EMPTY;
                }
            });
        }

        private void SetFootPoses(List<Vector2> footPoses) {
            footPoses.Clear();
            foreach (var body in dsm.Device.Body.data) {
                if (body.IsTracked) {
                    foreach (var type in FOOT_JOINTS) {
                        var joint = body.joints[type];
                        if (joint.IsTracked) {
                            footPoses.Add(dsm.Device.CameraPosToDepthMapPos(joint.Pos));
                        }
                    }
                }
            }
        }

        private byte SliceByDepth(ushort depth, int i, CalcTempData calcTemp, bool sliceEmpty = true) {
            int d = calcTemp.staticDepth[i] - depth;
            if (sliceEmpty && d < zoneCut)
                return HumanMask.EMPTY;
            if (IsFoot(i, calcTemp)) {
                if (d < zoneTouchFoot)
                    return HumanMask.NEAR_FOOT;
                return HumanMask.FAR_FOOT;
            } else {
                if (d < zoneTouch)
                    return HumanMask.NEAR;
                return HumanMask.FAR;
            }
        }

        private bool IsFoot(int i, CalcTempData calcTemp) {
            Vector2 p = new Vector2(i % calcTemp.mask.width, i / calcTemp.mask.width);
            foreach (var footP in calcTemp.footPoses) {
                if ((footP - p).SqrMagnitude() < FOOT_RADIUS_SQR) {
                    return true;
                }
            }
            return false;
        }

        private void CalcHumanMask(CalcTempData calcTemp) {
            byte[] mask = calcTemp.mask.arr;
            int countInCurrWave = calcTemp.queue.GetCount();
            int currWave = 0;
            while (calcTemp.queue.GetCount() > 0 && currWave < MAX_COLORING_WAVES) {
                int i = calcTemp.queue.Dequeue();
                --countInCurrWave;
                for (int n = 0; n < 8; ++n) {
                    int j = calcTemp.mask.GetIndexOfNeighbor(i, n);
                    if (j > 0 && PaintThisPix(ref mask[j], ref calcTemp.neightbors[j], j, calcTemp)) {
                        calcTemp.queue.Enqueue(j);
                    }
                }
                if (countInCurrWave == 0) {
                    countInCurrWave = calcTemp.queue.GetCount();
                    ++currWave;
                }
            }
        }

        private bool PaintThisPix(ref byte mask, ref byte neightbors, int j, CalcTempData calcTemp) {
            ++neightbors;
            if (mask == HumanMask.EMPTY && neightbors == MAX_COLOR_NEIGHTBORS) {
                ushort d = calcTemp.depth[j];
                if (COLORING_EXTRA_DEPTH && d > MAX_WALL_DIST)
                    mask = HumanMask.FAR;
                else
                    mask = SliceByDepth(d, j, calcTemp);
                return mask != HumanMask.EMPTY;
            }
            return false;
        }

        public event Action<Vector3, float> OnDepthToColorOffsetChanged;

        private void DepthToColorChanged() {
            kinConv.AddToMainThread("DepthToColorChanged", GetType().Name, 
                DepthToColorChangedConveyer());
        }

        private IEnumerator DepthToColorChangedConveyer() {
            Vector3 offsetPos;
            float scaleFromDepth;
            if (GetDethToColorOffset(out offsetPos, out scaleFromDepth)) 
                OnDepthToColorOffsetChanged?.Invoke(offsetPos, scaleFromDepth);
            yield return null;
        }

        public bool GetDethToColorOffset(out Vector3 offsetPos, out float scaleFromDepth) {
            offsetPos = Vector3.zero;
            scaleFromDepth = 0;
            var depth = staticDepth?.GetData();
            if (depth == null)
                return false;
            var depthToWall = staticDepth.GetDepthToWall();
            var dCorner = new Vector2(dsm.Device.Depth.width - 1, dsm.Device.Depth.height - 1);
            var dCenter = dCorner / 2f;
            var cCenter = dsm.Device.DepthMapPosToColorMapPos(dCenter, depthToWall);
            var cCorner = dsm.Device.DepthMapPosToColorMapPos(dCorner, depthToWall);
            scaleFromDepth = (cCorner - cCenter).magnitude / (dCorner - dCenter).magnitude;
            offsetPos = cCenter - new Vector2(dsm.Device.Color.width - 1, dsm.Device.Color.height - 1) / 2;
            return true;
        }
    }
}