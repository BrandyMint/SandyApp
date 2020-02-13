using Unity.Mathematics;
using UnityEngine;

namespace Games.Common.Metaballs {
    public class BlobsControllerTransformChilds : BlobsControllerBase {
        public override void GetBlob(int i, out Vector3 p, out float r) {
            var child = transform.GetChild(i);
            p = child.localPosition;
            r = math.cmax(child.localScale);
        }

        public override int BlobsCount() {
            return transform.childCount;
        }
    }
}