using UnityEngine;

namespace Games.Common.Metaballs {
    public abstract class BlobsControllerBase : MonoBehaviour {
        public abstract void GetBlob(int i, out Vector3 p, out float r);
        public abstract int BlobsCount();
    }
}