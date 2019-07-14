using NUnit.Framework;
using UnityEngine.Experimental.Rendering;

namespace Test.Editor {
    public class GraphicsFormat {
        [Test]
        public void TestGetBlockSize() {
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SInt),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SRGB),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat),
                2);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UInt),
                2);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8_SInt),
                2);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.B5G6R5_UNormPack16),
                2);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SInt),
                4);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt),
                4);
            Assert.AreEqual(GraphicsFormatUtility.GetBlockSize(UnityEngine.Experimental.Rendering.GraphicsFormat.RGBA_ASTC4X4_UNorm),
                128/8);
        }
        
        [Test]
        public void TestGetComponentCount() {
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SInt),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SRGB),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UInt),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8_SInt),
                2);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.B5G6R5_UNormPack16),
                3);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SInt),
                4);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt),
                1);
            Assert.AreEqual(GraphicsFormatUtility.GetComponentCount(UnityEngine.Experimental.Rendering.GraphicsFormat.RGBA_ASTC4X4_UNorm),
                4);
        }
    }
}