using OpenCvSharp;
using UnityEngine;

namespace Utilities.OpenCVSharpUnity {
    public static class Convert {
        private static readonly Matrix4x4 _RHS_TO_LHS = new Matrix4x4 { m00 = 1f, m11 = -1f, m22 = 1f, m33 = 1f};
        private static readonly double[] _LHS_FLIP_BACK = new double[] { 
            1.0, 0.0, 0.0, 
            0.0, 1.0, 0.0, 
            0.0, 0.0, -1.0
        };
        
        public static Vector2 ToVector2(Point2f cvTVec) {
            return new Vector2 {
                x = cvTVec.X,
                y = cvTVec.Y
            };
        }
        
        public static Vector2 ToVector2(Point2d cvTVec) {
            return new Vector2 {
                x = (float) cvTVec.X,
                y = (float) cvTVec.Y
            };
        }
        
        public static Vector2 ToVector2(Vector3 p) {
            return new Vector2 {
                x = p.x,
                y = p.y
            };
        }

        public static Vector3 ToVector3(Point3f p) {
            return new Vector3(p.X, p.Y, p.Z);
        }

        public static Point3f ToPoint3f(Vector3 v) {
            return new Point3f(v.x, v.y, v.z);
        }

        public static Vector3 ToVector3(Vec3d cvTVec) {
            return ToVector3(cvTVec.Item0, cvTVec.Item1, cvTVec.Item2);
        }

        public static Vector3 ToVector3(double[] cvTVec, int offset = 0) {
            return ToVector3(cvTVec[offset], cvTVec[offset + 1], cvTVec[offset + 2]);
        }

        public static Point2f ToPoint2f(Vector2 v) {
            return new Point2f(v.x, v.y);
        }

        public static Vec3d ToVec3D(double[] cvTVec, int offset = 0) {
            return new Vec3d(cvTVec[offset], cvTVec[offset + 1], cvTVec[offset + 2]);
        }

        private static Vector3 ToVector3(double x, double y, double z) {
            return new Vector3 {
                x = (float) x,
                y = (float) y,
                z = (float) z
            };
        }

        //TODO: need testing
        public static Matrix4x4 AffineMatToMatrix4x4(Mat mat) {
            var m = Matrix4x4.identity;
            for (int i = 0; i < mat.Rows && i < 4; ++i) {
                for (int j = 0; j < mat.Cols && j < 4; ++j) {
                    m[i, j] = (float) mat.At<double>(i, j);
                }
            }

            return m;
        }

        public static InputArray ToInputArray(Vec3d v) {
            return InputArray.Create(new[] {v.Item0, v.Item1, v.Item2});
        }

        //TODO: need testing
        public static Matrix4x4 RVecTVecToMatrix4X4(Vec3d rvec, Vec3d tvec) {
            using (var rot = new Mat(3, 3, MatType.CV_64F)) {
                using (var r = ToInputArray(rvec)) {
                    Cv2.Rodrigues(r, rot);
                }

                return RodriguesTVecToMatrix4X4(rot, tvec);
            }
        }

        //TODO: need testing
        public static Matrix4x4 RVecTVecToMatrix4X4(double[] rvec, double[] tvec) {
            using (var rot = new Mat(3, 3, MatType.CV_64F)) {
                using (var r = InputArray.Create(rvec)) {
                    Cv2.Rodrigues(r, rot);
                }

                return RodriguesTVecToMatrix4X4(rot, ToVec3D(tvec));
            }
        }

        //TODO: need testing
        private static Matrix4x4 RodriguesTVecToMatrix4X4(Mat rot3x3, Vec3d tvec) {
            using (var lhsFlipBack = new Mat(3, 3, MatType.CV_64F, _LHS_FLIP_BACK)) {
                rot3x3 = rot3x3 * lhsFlipBack;
            }
            
            var m = Matrix4x4.identity;
            for (int i = 0; i < 3; ++i) {
                m[i, 3] = (float) tvec[i];
                for (int j = 0; j < 3; ++j) {
                    m[i, j] = (float) rot3x3.At<double>(i, j);
                }
            }

            return _RHS_TO_LHS * m;
        }
    }
}