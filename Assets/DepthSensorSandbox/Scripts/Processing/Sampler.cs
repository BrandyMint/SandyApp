using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public struct Sampler {
        public const int INVALID_ID = -1;
        public const ushort INVALID_DEPTH = 0;

        private int _width;
        private int _height;
        private int _borderNbr0;
        private int _borderNbr1;
        private int _borderNbr2;
        private int _borderNbr3;
        
        public void SetDimens(int w, int h) {
            _width = w;
            _height = h;
            
            var wMin = 0;
            var wMax =  w;
            var hMin = 0;
            var hMax =  h;
            
            //    0 
            //  3 i 1
            //    2 
            _borderNbr0 = w * (hMax - 1) - 1;
            _borderNbr1 = wMax - 2;
            _borderNbr2 = w * (hMin + 1);
            _borderNbr3 = wMin + 1;
        }
        
        public ushort SafeGet(Buffer2D<ushort> depth, int x, int y) {
            if (x < 0 || x >= depth.width
                      || y < 0 || y >= depth.height)
                return INVALID_DEPTH;
            return depth.data[depth.GetIFrom(x, y)];
        }

        public ushort SafeGet(Buffer2D<ushort> depth, Vector2Int xy) {
            return SafeGet(depth, xy.x, xy.y);
        }
        
        //  7 0 4
        //  3 i 1
        //  6 2 5
        public int GetIndexOfNeighbor(int i, int nbr) {
            switch (nbr) {
                case 0:
                    return (i > _borderNbr0) ? INVALID_ID : i + _width;
                case 1:
                    return (i % _width > _borderNbr1) ? INVALID_ID : i + 1;
                case 2:
                    return (i < _borderNbr2) ? INVALID_ID : i - _width;
                case 3:
                    return (i % _width < _borderNbr3) ? INVALID_ID : i - 1;
                case 4:
                    return (i > _borderNbr0 || i % _width > _borderNbr1) ? INVALID_ID : i + _width + 1;
                case 5:
                    return (i < _borderNbr2 || i % _width > _borderNbr1) ? INVALID_ID : i - _width + 1;
                case 6:
                    return (i < _borderNbr2 || i % _width < _borderNbr3) ? INVALID_ID : i - _width - 1;
                case 7:
                    return (i > _borderNbr0 || i % _width < _borderNbr3) ? INVALID_ID : i + _width - 1;
            }
            return INVALID_ID;
        }
    }
}