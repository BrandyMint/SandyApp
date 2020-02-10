using OpenCvSharp;
using Unity.Mathematics;
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

        private static float3 ToFloat3(Vec3d v) {
            return new float3((float) v.Item0, (float) v.Item1, (float) v.Item2);
        }
        
        private static Vector4 ToVector4(Vec4d v) {
            return ToVector4(v.Item0, v.Item1, v.Item2, v.Item3);
        }
        

        private static float4 ToFloat4(Vec4d v) {
            return new float4((float) v.Item0, (float) v.Item1, (float) v.Item2, (float) v.Item3);
        }

        public static Point2f ToPoint2f(Vector2 v) {
            return new Point2f(v.x, v.y);
        }

        public static Vec3d ToVec3D(double[] cvTVec, int offset = 0) {
            return new Vec3d(cvTVec[offset], cvTVec[offset + 1], cvTVec[offset + 2]);
        }

        public enum Dir {
            AUTO,
            HORIZONTAL,
            VERTICAL
        }

        public static Vec3d ToVec3d(MatExpr expr, int i = 0, Dir dir = Dir.AUTO) {
            using (Mat m = expr) {
                return ToVec3d(m, i, dir);
            }
        }

        public static Vec3d ToVec3d(Mat m, int i = 0, Dir dir = Dir.AUTO) {
            if (dir == Dir.AUTO)
                dir = m.Rows == 3 ? Dir.VERTICAL : Dir.HORIZONTAL; 
            if (dir == Dir.VERTICAL) {
                return new Vec3d(m.At<double>(0, i), m.At<double>(1, i), m.At<double>(2, i));
            } else {
                return new Vec3d(m.At<double>(i, 0), m.At<double>(i, 1), m.At<double>(i, 2));
            }
        }
        
        public static Vec3d ToVec3d(double[,] m, int i = 0, Dir dir = Dir.AUTO) {
            if (dir == Dir.AUTO)
                dir = m.GetLength(0) == 3 ? Dir.VERTICAL : Dir.HORIZONTAL; 
            if (dir == Dir.VERTICAL) {
                return new Vec3d(m[0, i], m[1, i], m[2, i]);
            } else {
                return new Vec3d(m[i, 0], m[i, 1], m[i, 2]);
            }
        }

        public static Vec4d ToVec4d(Mat m, int i = 0, Dir dir = Dir.AUTO) {
            if (dir == Dir.AUTO)
                dir = m.Rows == 4 ? Dir.VERTICAL : Dir.HORIZONTAL; 
            if (dir == Dir.VERTICAL) {
                return new Vec4d(m.At<double>(0, i), m.At<double>(1, i), m.At<double>(2, i), m.At<double>(3, i));
            } else {
                return new Vec4d(m.At<double>(i, 0), m.At<double>(i, 1), m.At<double>(i, 2), m.At<double>(i, 3));
            }
        }

        public static Vec4d ToVec4d(double[,] m, int i = 0, Dir dir = Dir.AUTO) {
            if (dir == Dir.AUTO)
                dir = m.GetLength(0) == 3 ? Dir.VERTICAL : Dir.HORIZONTAL; 
            if (dir == Dir.VERTICAL) {
                return new Vec4d(m[0, i], m[1, i], m[2, i], m[3, i]);
            } else {
                return new Vec4d(m[i, 0], m[i, 1], m[i, 2], m[i, 3]);
            }
        }

        public static Vector3 ToVector3(MatExpr expr, int i = 0, Dir dir = Dir.AUTO) {
            return ToVector3(ToVec3d(expr, i, dir));
        }

        public static Vector3 ToVector3(Mat m, int i = 0, Dir dir = Dir.AUTO) {
            return ToVector3(ToVec3d(m, i, dir));
        }

        public static float3 ToFloat3(Mat m, int i = 0, Dir dir = Dir.AUTO) {
            return ToFloat3(ToVec3d(m, i, dir));
        }
        
        public static float3 ToFloat3(double[,] m, int i = 0, Dir dir = Dir.AUTO) {
            return ToFloat3(ToVec3d(m, i, dir));
        }
        
        public static Vector4 ToVector4(MatExpr expr, int i = 0, Dir dir = Dir.AUTO) {
            return ToVector4(ToVec4d(expr, i, dir));
        }

        public static Vector4 ToVector4(Mat m, int i = 0, Dir dir = Dir.AUTO) {
            return ToVector4(ToVec4d(m, i, dir));
        }

        public static float4 ToFloat4(Mat m, int i = 0, Dir dir = Dir.AUTO) {
            return ToFloat4(ToVec4d(m, i, dir));
        }
        
        public static float4 ToFloat4(double[,] m, int i = 0, Dir dir = Dir.AUTO) {
            return ToFloat4(ToVec4d(m, i, dir));
        }

        private static Vector3 ToVector3(double x, double y, double z) {
            return new Vector3 {
                x = (float) x,
                y = (float) y,
                z = (float) z
            };
        }
        
        private static Vector4 ToVector4(double x, double y, double z, double w) {
            return new Vector4 {
                x = (float) x,
                y = (float) y,
                z = (float) z,
                w = (float) w,
            };
        }
        
        public static float3x3 ToFloat3x3(Mat m) {
            var a = new float3x3();
            for (int c = 0; c < 3; ++c) {
                for (int r = 0; r < 3; ++r) {
                    a[c][r] = (float) m.At<double>(r, c);
                }
            }
            return a;
        }
        
        public static float3x3 ToFloat3x3(double [,] m) {
            var a = new float3x3();
            for (int c = 0; c < 3; ++c) {
                for (int r = 0; r < 3; ++r) {
                    a[c][r] = (float) m[r,c];
                }
            }
            return a;
        }

        public static InputArray ToInputArray(Vec3d v) {
            return InputArray.Create(new[] {v.Item0, v.Item1, v.Item2});
        }

        public static float4x4 RVecTVecToFloat4x4(Vec3d rvec, Vec3d tvec) {
            using (var rot = new Mat(3, 3, MatType.CV_64F)) {
                using (var r = ToInputArray(rvec)) {
                    Cv2.Rodrigues(r, rot);
                }

                return RodriguesTVecToFloat4x4(rot, tvec);
            }
        }

        public static float4x4 RodriguesTVecToFloat4x4(Mat rot, Vec3d tvec) {
            return new float4x4(ToFloat3x3(rot), ToFloat3(tvec));
        }
        
        public static float4x4 RodriguesTVecToFloat4x4(double[,] rot, Vec3d tvec) {
            return new float4x4(ToFloat3x3(rot), ToFloat3(tvec));
        }

        /*//TODO: need testing
        public static Matrix4x4 AffineMatToMatrix4x4(Mat mat) {
            var m = Matrix4x4.identity;
            for (int i = 0; i < mat.Rows && i < 4; ++i) {
                for (int j = 0; j < mat.Cols && j < 4; ++j) {
                    m[i, j] = (float) mat.At<double>(i, j);
                }
            }

            return m;
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
        }*/
    }
}