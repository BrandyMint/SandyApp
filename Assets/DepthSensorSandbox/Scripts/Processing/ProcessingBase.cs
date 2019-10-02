using System;
using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public abstract class ProcessingBase : IDisposable {
        public const int INVALID_ID = -1;
        public const ushort INVALID_DEPTH = 0;
        
        public bool OnlyRawBuffersIsInput = true;
        public bool Active = true;
        
        protected DepthBuffer[] _rawBuffers;
        protected DepthBuffer _inOut;
        protected DepthBuffer _inDepth;

        public void Process(DepthBuffer[] rawBuffers, DepthBuffer inOut) {
            if (Active) {
                _rawBuffers = rawBuffers;
                _inOut = inOut;
                _inDepth = OnlyRawBuffersIsInput ? _rawBuffers[0] : _inOut;
                ProcessInternal();
            }
        }

        protected abstract void ProcessInternal();

        protected bool ReCreateIfNeed<T>(ref T[] a, int len) {
            if (a == null || a.Length != len) {
                a = new T[len];
                return true;
            }
            return false;
        }
        
        protected static ushort SafeGet(DepthBuffer depth, int x, int y) {
            if (x < 0 || x >= depth.width
                || y < 0 || y >= depth.height)
                return INVALID_DEPTH;
            return depth.data[depth.GetIFrom(x, y)];
        }

        protected static ushort SafeGet(DepthBuffer depth, Vector2Int xy) {
            return SafeGet(depth, xy.x, xy.y);
        }
        
        //  7 0 4
        //  3 i 1
        //  6 2 5
        public int GetIndexOfNeighbor(int i, int nbr) {
            var borderW1 = 0;
            var borderW2 =  _inOut.width;
            var borderH1 = 0;
            var borderH2 =  _inOut.height;
            var borderNbr0 = (borderH1 + 1) *  _inOut.width;
            var borderNbr2 = borderW2 - 2;
            var borderNbr4 =  _inOut.width * (borderH2 - 1);
            var borderNbr6 = borderW1 + 1;
            switch (nbr) {
                case 0:
                    return (i < borderNbr0) ? -1 : i - _inOut.width;
                case 1:
                    return (i % _inOut.width > borderNbr2) ? -1 : i + 1;
                case 2:
                    return (i > borderNbr4) ? -1 : i + _inOut.width;
                case 3:
                    return (i % _inOut.width < borderNbr6) ? -1 : i - 1;
                case 4:
                    return (i < borderNbr0 || i % _inOut.width > borderNbr2) ? -1 : i - _inOut.width + 1;
                case 5:
                    return (i > borderNbr4 || i % _inOut.width > borderNbr2) ? -1 : i + _inOut.width + 1;
                case 6:
                    return (i > borderNbr4 || i % _inOut.width < borderNbr6) ? -1 : i + _inOut.width - 1;
                case 7:
                    return (i < borderNbr0 || i % _inOut.width < borderNbr6) ? -1 : i - _inOut.width - 1;
            }
            return INVALID_ID;
        }

        public virtual void Dispose() {}
    }
}