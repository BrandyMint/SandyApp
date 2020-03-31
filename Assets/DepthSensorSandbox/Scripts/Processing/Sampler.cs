﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public struct Sampler {
        public const int INVALID_ID = -1;
        public const ushort INVALID_DEPTH = 0;
        public static readonly Rect FULL_CROPPING = UnityEngine.Rect.MinMaxRect(0f, 0f, 1f, 1f);

        private int _width;
        private int _height;
        private int _borderNbr0;
        private int _borderNbr1;
        private int _borderNbr2;
        private int _borderNbr3;
        private RectInt _cropping;
        private Rect _cropping01;

        public int width => _width;
        public int height => _height;

        public static Sampler Create() {
            return new Sampler {
                _cropping01 = FULL_CROPPING
            };
        }
        
        public static Sampler Create(int w, int h) {
            var s = Create();
            s.SetDimens(w, h);
            return s;
        }

        public RectInt Rect { get; private set; }
        public Rect Cropping01 { get; private set; }
        
        public void SetDimens(int w, int h) {
            if (_width == w && _height == h)
                return;
            _width = w;
            _height = h;
            SetCropping01(_cropping01);
        }

        public void SetCropping(RectInt rect) {
            _cropping = rect;
            _cropping01 = UnityEngine.Rect.MinMaxRect(
                (float) _cropping.xMin / _width, 
                (float) _cropping.yMin / _height,
                (float) _cropping.xMax / _width,
                (float) _cropping.yMax / _height
            );
            UpdateDimensAndRect();
        }

        public void SetCropping01(Rect rect01) {
            _cropping01 = rect01;
            _cropping.xMin = Mathf.FloorToInt(_cropping01.xMin * _width);
            _cropping.yMin = Mathf.FloorToInt(_cropping01.yMin * _height);
            _cropping.xMax = Mathf.CeilToInt(_cropping01.xMax * _width);
            _cropping.yMax = Mathf.CeilToInt(_cropping01.yMax * _height);
            UpdateDimensAndRect();
        }

        public void ResetCropping() {
            SetCropping01(FULL_CROPPING);
        }

        private void UpdateDimensAndRect() {
            //clamp
            _cropping.xMin = Mathf.Clamp(_cropping.xMin, 0, _width-1);
            _cropping.yMin = Mathf.Clamp(_cropping.yMin, 0, _height-1);
            _cropping.xMax = Mathf.Clamp(_cropping.xMax, 1, _width);
            _cropping.yMax = Mathf.Clamp(_cropping.yMax, 1, _height);
            
            //    0 
            //  3 i 1
            //    2 
            _borderNbr0 = _width * (_cropping.yMax - 1) - 1;
            _borderNbr1 = _cropping.xMax - 2;
            _borderNbr2 = _width * (_cropping.yMin + 1);
            _borderNbr3 = _cropping.xMin + 1;
            
            Rect = _cropping;
            Cropping01 = _cropping01;
        }
        
        public ushort SafeGet(Buffer2D<ushort> depth, int x, int y) {
            if (x < _cropping.xMin || x >= _cropping.xMax
                      || y < _cropping.yMin || y >= _cropping.yMax)
                return INVALID_DEPTH;
            return depth.data[GetIFrom(x, y)];
        }

        public ushort SafeGet(Buffer2D<ushort> depth, Vector2Int xy) {
            return SafeGet(depth, xy.x, xy.y);
        }
        
        public int GetDirToCenter4Diag(int i) {
            return GetDirToCenter4Diag(GetXYFrom(i));
        }
        
        
        //    0 
        //  3 i 1
        //    2 
        public int GetDirToCenter4(Vector2 p) {
            var d = _cropping.center - p;
            if (Math.Abs(d.x) > Math.Abs(d.y)) {
                if (d.x > 0f) {
                    return 1;
                }
                return 3;
            }
            if (d.y > 0f)
                return 0;
            return 2;
        }
        
        //  7   4
        //    i 
        //  6   5
        public int GetDirToCenter4Diag(Vector2 p) {
            var d = _cropping.center - p;
            if (d.x > 0f) {
                if (d.y > 0f)
                    return 4;
                return 5;
            } else {
                if (d.y > 0f)
                    return 7;
                return 6;
            }
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIFrom(int x, int y) {
            return y * _width + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetXYFrom(int i) {
            return new Vector2(
                i % _width,
                i / _width
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int GetXYiFrom(int i) {
            return new Vector2Int(
                i % _width,
                i / _width
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIInRect(int i) {
            var v = GetXYiFrom(i);
            v -= _cropping.min;
            return v.y * _cropping.width + v.x;;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetXYConverted(Sampler from, int i) {
            var p = from.GetXYFrom(i) + new Vector2(0.5f, 0.5f);
            var scale = new Vector2( (float) width / from.width, (float) height / from.height);
            return new Vector2(p.x * scale.x, p.y * scale.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int GetXYiConverted(Sampler from, int i) {
            var p = GetXYConverted(from, i);
            return new Vector2Int((int) p.x, (int) p.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIConverted(Sampler from, int i) {
            var p = GetXYConverted(from, i);
            return GetIFrom((int) p.x, (int) p.y);
        }

        public Vector2 GetXYScaled01(Vector2 p) {
            p.x /= width;
            p.y /= height;
            return p;
        }

#region Processing

        public void EachInHorizontal(int y, Action<int> handler) {
            EachInHorizontal(y, handler, 0, 0);
        }

        public void EachInHorizontal(int y, Action<int> handler, int skipFromStart, int skipOnEnd) {
            var x = Mathf.Clamp(_cropping.xMin + skipFromStart, _cropping.xMin, _cropping.xMax - 1);
            y = Mathf.Clamp(y, _cropping.yMin, _cropping.yMax - 1);
            var start = GetIFrom(x, y);
            var n = _cropping.width - skipFromStart - skipOnEnd;
            EachInLine(start, 1, n, handler);
        }

        public void EachInVertical(int x, Action<int> handler) {
            EachInVertical(x, handler, 0, 0);
        }

        public void EachInVertical(int x, Action<int> handler, int skipFromStart, int skipOnEnd) {
            x = Mathf.Clamp(x, _cropping.xMin, _cropping.xMax - 1);
            var y = Mathf.Clamp(_cropping.yMin  + skipFromStart, _cropping.yMin, _cropping.yMax - 1);
            var start = GetIFrom(x, y);
            var n = _cropping.height - skipFromStart - skipOnEnd;
            EachInLine(start,  _width, n, handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EachInLine(int start, int step, int n, Action<int> handler) {
            var id = start;
            for (int i = 0; i < n; ++i) {
                handler(id);
                id += step;
            }
        }

        public void Each(Action<int> handler) {
            for (var y = _cropping.yMin; y < _cropping.yMax; ++y) {
                EachInHorizontal(y, handler);
            }
        }
        
        public bool EachAre(Func<int, bool> handler) {
            for (var y = _cropping.yMin; y < _cropping.yMax; ++y) {
                var x = _cropping.xMin;
                var id = GetIFrom(x, y);
                var n = _cropping.width;
                for (int i = 0; i < n; ++i) {
                    if (!handler(id))
                        return false;
                    ++id;
                }
            }

            return true;
        }
        
#region Each parallel simple
        private Action<int> _eachHandler;
        private int _downsize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EachParallelHorizontal(Action<int> handler) {
            _eachHandler = handler;
            Parallel.For(_cropping.yMin, _cropping.yMax, EachInHorizontalBody);
        }

        private void EachInHorizontalBody(int y) {
            EachInHorizontal(y, _eachHandler);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EachParallelVertical(Action<int> handler) {
            _eachHandler = handler;
            Parallel.For(_cropping.xMin, _cropping.xMax, EachInVerticalBody);
        }

        private void EachInVerticalBody(int x) {
            EachInVertical(x, _eachHandler);
        }
        
        public void EachParallelDownsizeSafe(Action<int> handler, int downsize) {
            _eachHandler = handler;
            _downsize = downsize;
            Parallel.For(_cropping.xMin/downsize, _cropping.xMax/downsize + 1, EachParallelVerticalDownsizeSafeBody);
        }
        
        private void EachParallelVerticalDownsizeSafeBody(int xDownsized) {
            for (int i = 0; i < _downsize; ++i) {
                var x = xDownsized * _downsize + i;
                if (x >= _cropping.xMin && x < _cropping.xMax)
                    EachInVertical(x, _eachHandler);
            }
        }
        
        public void EachParallelDownsize(Action<int> handler, int downsize) {
            _eachHandler = handler;
            _downsize = downsize;
            Parallel.For(_cropping.xMin/downsize, _cropping.xMax/downsize + 1, EachParallelHorizontalDownsizeBody);
        }
        
        private void EachParallelHorizontalDownsizeBody(int yDownsized) {
            var x = _cropping.xMin;
            var y = Mathf.Clamp(yDownsized * _downsize, _cropping.yMin, _cropping.yMax - 1);
            var start = GetIFrom(x, y);
            var n = _cropping.width/_downsize;
            EachInLine(start, _downsize, n, _eachHandler);
            if (_cropping.width % _downsize != 0) {
                var end = GetIFrom(_cropping.xMax - 1, y);
                _eachHandler(end);
            }
        }
#endregion

#region Each with ParallelLocalState
        public interface IParallelLocalState {
            void Handle(int id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EachParallelHorizontal<TLocalState>(Func<TLocalState> initLocalState, Action<TLocalState> endLocalState) where TLocalState : IParallelLocalState {
            Parallel.For(_cropping.yMin, _cropping.yMax, initLocalState, EachInHorizontalBodyState, endLocalState);
        }

        private TLocalState EachInHorizontalBodyState<TLocalState>(int y, ParallelLoopState loop, TLocalState state) where TLocalState: IParallelLocalState {
            EachInHorizontal(y, state.Handle);
            return state;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EachParallelVertical<TLocalState>(Func<TLocalState> initLocalState, Action<TLocalState> endLocalState) where TLocalState : IParallelLocalState {
            Parallel.For(_cropping.xMin, _cropping.xMax, initLocalState, EachInVerticalBodyState, endLocalState);
        }

        private TLocalState EachInVerticalBodyState<TLocalState>(int x, ParallelLoopState loop, TLocalState state) where TLocalState: IParallelLocalState {
            EachInVertical(x, state.Handle);
            return state;
        }
#endregion

#region Each with line state
        public interface IParallelLineState {
            void Handle(int id);
        }
                
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EachParallelHorizontal<TLineState>(Func<int, TLineState> initLineState) where TLineState : IParallelLineState {
            var body =  new BodyWithLineState<TLineState> {
                initState = initLineState,
                eachInLine = EachInHorizontal
            };
            Parallel.For(_cropping.yMin, _cropping.yMax, body.EachInLineBodyLineState);
        }
                
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EachParallelVertical<TLineState>(Func<int, TLineState> initLineState) where TLineState : IParallelLineState {
            var body =  new BodyWithLineState<TLineState> {
                initState = initLineState,
                eachInLine = EachInVertical
            };
            Parallel.For(_cropping.xMin, _cropping.xMax, body.EachInLineBodyLineState);
        }

        private struct BodyWithLineState<TLineState> where TLineState : IParallelLineState {
            public Func<int, TLineState> initState;
            public Action<int, Action<int>> eachInLine;
                    
            public void EachInLineBodyLineState(int x) {
                var state = initState(x);
                eachInLine(x, state.Handle);
            }
        }
#endregion

#endregion
    }
}