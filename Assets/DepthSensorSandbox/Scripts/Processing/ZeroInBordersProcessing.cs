using System.Threading.Tasks;

namespace DepthSensorSandbox.Processing {
    public class ZeroInBordersProcessing : ProcessingBase {
        public bool AutoDeactivate;
        
        protected override void ProcessInternal() {
            if (AutoDeactivate)
                Active = false;
            _zeroDepth = (ushort) ((Prefs.Sandbox.ZeroDepth + Prefs.Calibration.Position.z) * 1000f);
            Parallel.For(0, _inDepth.length, ZeroBody);
        }

        private ushort _zeroDepth;
        private void ZeroBody(int i) {
            var p = _s.GetXYiFrom(i);
            if (_s.Rect.Contains(p)) {
                _out.data[i] = _inDepth.data[i];
            } else {
                _out.data[i] = _zeroDepth;
            }
        }
    }
}