using NUnit.Framework;
using OpenCvSharp;
using Unity.Mathematics;
using UnityEngine;
using Utilities.OpenCVSharpUnity;
using Random = UnityEngine.Random;

namespace Test.Editor {
    public class OpenCVSharpUnity {
        [TestCase(60f, 30f, 600, 400)]
        [TestCase(60f, 30f, 1024, 768)]
        [TestCase(60f, 30f, 1024, 768)]
        [TestCase(1f, 90f, 1024, 768)]
        [TestCase(90f, 1f, 1024, 768)]
        [TestCase(20f, 20f, 1024, 768)]
        public void TestCameraIntrinsicFov(float fovx, float fovy, int sizex, int sizey) {
            var intrinsic = new CameraIntrinsicParams(
                new Vector2(fovx, fovy), 
                new Size(sizex, sizey)
            );
            var fov = intrinsic.GetFOV();
            Assert.AreEqual(fovx, fov.x, 0.0001f);
            Assert.AreEqual(fovy, fov.y, 0.0001f);
        }

        [TestCase(10)]
        public void ToFloat3x3(int iterations) {
            for (int i = 0; i < iterations; ++i) {
                using (var m = CreateRandomMat(3, 3)) {
                    var arr = new double[m.Height, m.Width];
                    m.GetArray(0, 0, arr);
                    var a = Convert.ToFloat3x3(m);
                    var b = Convert.ToFloat3x3(arr);
                    Assert.AreEqual(a, b);
                }
            }
        }

        [TestCase(5, 5)]
        public void RodriguesTVecToFloat4x4(int interationsGenMat, int interationsGenPoint) {
            for (int i = 0; i < interationsGenMat; ++i) {
                using(var r = CreateRandomMat(3, 3))
                using(var t = CreateRandomMat(3, 1))
                using (var rt = new Mat(3, 4, MatType.CV_64F)) {
                    //opencv
                    Cv2.HConcat(r, t, rt);

                    for (int j = 0; j < interationsGenPoint; ++j) {
                        var p = CreateRandomMat(4, 1);
                        p.Set(3, 0, 1d);
                        float4 p_cv;
                        using (var ep2 = rt * p) {
                            p_cv = Convert.ToVector4(ep2);
                        }

                        //math
                        var a = Convert.RodriguesTVecToFloat4x4(r, Convert.ToVec3d(t));
                        var p_math = math.mul(a, Convert.ToFloat4(p));

                        for (int c = 0; c < 3; ++c) {
                            Assert.AreEqual(p_cv[c], p_math[c], 0.00001f);
                        }
                    }
                }
            }
        }

        [TestCase(1, 1)]
        public void TestCameraMatrixToAffine4x4(int interationsGenMat, int interationsGenPoint) {
            for (int i = 0; i < interationsGenMat; ++i) {
                using(var cam = CreateRandomMat(3, 3))
                using(var r = CreateRandomMat(3, 3))
                using(var t = CreateRandomMat(3, 1))
                using (var rt = new Mat(3, 4, MatType.CV_64F)) {
                    //opencv
                    Cv2.HConcat(r, t, rt);
                    using (var ecrt = cam * rt)
                    using (var crt = ecrt.ToMat()) {
                        for (int j = 0; j < interationsGenPoint; ++j) {
                            var p = CreateRandomMat(4, 1);
                            p.Set(3, 0, 1d);
                            float4 p_cv;
                            using (var ep2 = crt * p) {
                                p_cv = Convert.ToVector4(ep2);
                            }

                            //math
                            var mcam = new float4x4(Convert.ToFloat3x3(cam), float3.zero);
                            var mrt = Convert.RodriguesTVecToFloat4x4(r, Convert.ToVec3d(t));
                            var mcrt = math.mul(mcam, mrt);
                            var p_math = math.mul(mcrt, Convert.ToFloat4(p));

                            for (int c = 0; c < 3; ++c) {
                                Assert.AreEqual(p_cv[c], p_math[c], 0.00001f);
                            }
                        }
                    }
                }
            }
        }

        private Vec3d CreateRandomMatVec3d() {
            return new Vec3d(Random.value, Random.value, Random.value);
        }

        private Mat CreateRandomMat(int rows, int cols, int type = MatType.CV_64F) {
            var m = new Mat(rows, cols, type);
            for (int x = 0; x < rows; x++) {
                for (int y = 0; y < cols; y++) {
                    m.Set(x, y, (double)Random.value);
                }
            }

            return m;
        }
    }
}