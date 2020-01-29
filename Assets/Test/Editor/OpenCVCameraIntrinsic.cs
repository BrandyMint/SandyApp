using NUnit.Framework;
using OpenCvSharp;
using UnityEngine;
using Utilities.OpenCVSharpUnity;

namespace Test.Editor {
    public class OpenCVCameraIntrinsic {
        [TestCase(60f, 30f, 600, 400)]
        [TestCase(60f, 30f, 1024, 768)]
        [TestCase(60f, 30f, 1024, 768)]
        [TestCase(1f, 90f, 1024, 768)]
        [TestCase(90f, 1f, 1024, 768)]
        [TestCase(20f, 20f, 1024, 768)]
        public void TestInitWithFov(float fovx, float fovy, int sizex, int sizey) {
            var intrinsic = new CameraIntrinsicParams(
                new Vector2(fovx, fovy), 
                new Size(sizex, sizey)
            );
            var fov = intrinsic.GetFOV();
            Assert.AreEqual(fovx, fov.x, 0.0001f);
            Assert.AreEqual(fovy, fov.y, 0.0001f);
        }
    }
}