using Unity.Collections;

namespace Games.Common.ColliderGenerator {
    public class DataHandsByteArray : BaseColliderGeneratorDataProvider {
        public NativeArray<byte> arr;

        public override bool IsShapePixel(int x, int y) {
            return arr[GetIFrom(x, y)] > 0;
        }
    }
}