using System;
using System.Collections;
using System.Linq;
using DepthSensor;
using UnityEngine;

namespace HumanCollider {
    class StaticDepthScene : MonoBehaviour {
        private const float CALC_DEPTH_FACTOR = 0.2f;
        
        private ushort depthToWall;
        private ushort[][] snapshots = null;
        private ushort[] staticDepth = null;

        public void ReCreate(ushort countSnapshots, Action onCreated) {
            HumanMaskCreater.GetInstance().GetKinectConveyer().AddToBG(
                GetType().Name, null,
                ConveyerUpdate(countSnapshots, onCreated));
        }

        private IEnumerator ConveyerUpdate(ushort countSnapshots, Action onCreated) {
            snapshots = new ushort[countSnapshots][];

            var sDepth = DepthSensorManager.Instance.Device.Depth;
            int counter = 0;
            while (counter < countSnapshots) {
                if (sDepth.data != null && (counter > 0 || sDepth.data.Max() > 0)) {
                    snapshots[counter] = new ushort[sDepth.data.Length];
                    Array.Copy(sDepth.data, snapshots[counter], sDepth.data.Length);
                    ++counter;
                }
                yield return null;
            }

            staticDepth = GetMedianSnapshot(snapshots);
            snapshots = null;

            depthToWall = CalcDepthToWall();
            if (onCreated != null)
                onCreated();
            yield return null;
        }

        public ushort[] GetData() {
            return staticDepth;
        }

        public ushort GetDepthToWall() {
            return depthToWall;
        }

        private static ushort[] GetMedianSnapshot(ushort[][] snaps) {
            int columnCount = snaps[0].Length;
            int valsCount = snaps.Length;
            for (int i = 0; i < columnCount; ++i) {
                for (int j = 0; j < valsCount -1; j++) {
                    for (int k = j + 1; k < valsCount; k++) {
                        if (snaps[j][i] > snaps[k][i]) {
                            ushort t = snaps[j][i];
                            snaps[j][i] = snaps[k][i];
                            snaps[k][i] = t;
                        }
                    }
                }
            }
            return snaps[snaps.Length / 2];
        }

        private ushort CalcDepthToWall() {
            var depth4 = 
                staticDepth[OffsetFromCenterToIndex(new Vector2(CALC_DEPTH_FACTOR, CALC_DEPTH_FACTOR))] +
                staticDepth[OffsetFromCenterToIndex(new Vector2(CALC_DEPTH_FACTOR, -CALC_DEPTH_FACTOR))] +
                staticDepth[OffsetFromCenterToIndex(new Vector2(-CALC_DEPTH_FACTOR, -CALC_DEPTH_FACTOR))] +
                staticDepth[OffsetFromCenterToIndex(new Vector2(-CALC_DEPTH_FACTOR, CALC_DEPTH_FACTOR))];
            return (ushort) (depth4 / 4);
        }

        private static int OffsetFromCenterToIndex(Vector2 offset) {
            var sDepth = DepthSensorManager.Instance.Device.Depth;
            var x = (int) Mathf.Round(Mathf.Lerp(0, sDepth.width - 1, 0.5f + offset.x));
            var y = (int) Mathf.Round(Mathf.Lerp(0, sDepth.height - 1, 0.5f + offset.y));
            return sDepth.width * y + x;
        }
    }
}