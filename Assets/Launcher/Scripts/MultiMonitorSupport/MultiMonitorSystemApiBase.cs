using System;
using System.Collections.Generic;
using UnityEngine;

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitorSystemApiBase : IDisposable {
        private int _multiRectCacheFrame = -1;
        private bool _multiRectCacheSuccess;
        private Rect _multiRectCache = Rect.zero;
        
        private int _monitorsCacheFrame = -1;
        private bool _monitorsCacheSuccess;
        private List<Rect> _monitorsCache = new List<Rect>();

        public event Action<int> OnNotEnoughMonitors;

        public int UseMonitors = 1;
        
        public bool GetMultiMonitorRect(out Rect rect) {
            if (_multiRectCacheFrame != Time.frameCount) {
                _multiRectCacheFrame = Time.frameCount;
                _multiRectCacheSuccess = GetMultiMonitorRectInternal(out _multiRectCache);
            }
            
            rect = _multiRectCache;
            return _multiRectCacheSuccess;
        }

        public bool GetMonitorRects(out List<Rect> rects) {
            if (_monitorsCacheFrame != Time.frameCount) {
                _monitorsCacheFrame = Time.frameCount;
                _monitorsCacheSuccess = GetMonitorRectsInternal(out _monitorsCache);
            }
            
            rects = _monitorsCache;
            return _monitorsCacheSuccess;
        }

        protected void OnNotEnoughMonitorsInvoke(int count) {
            OnNotEnoughMonitors?.Invoke(count);
        }

        public virtual void MoveMainWindow(Rect rect) {
            throw new NotImplementedException();
        }

        protected virtual bool GetMonitorRectsInternal(out List<Rect> rects) {
            rects = new List<Rect>();
            return false;
        }

        protected virtual bool GetMultiMonitorRectInternal(out Rect rect) {
            if (GetMonitorRects(out var monitors)) {
                if (monitors.Count < UseMonitors) {
                    OnNotEnoughMonitorsInvoke(monitors.Count);
                    rect = Rect.zero;
                    return false;
                }
                
                rect = Rect.MinMaxRect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
                for (int i = 0; i < Math.Min(monitors.Count, UseMonitors); ++i) {
                    var monitor = monitors[i];
                    if (monitor.xMin < rect.xMin) rect.xMin = monitor.xMin;
                    if (monitor.yMin < rect.yMin) rect.yMin = monitor.yMin;
                    if (monitor.xMax > rect.xMax) rect.xMax = monitor.xMax;
                    if (monitor.yMax > rect.yMax) rect.yMax = monitor.yMax;
                }
                return true;
            }

            rect = Rect.zero;
            return false;
        }

        public virtual void Dispose() { }
    }
}