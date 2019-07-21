using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Utilities {
    public static class TexturesHelper {
        public static bool ReCreateIfNeed(ref Texture2D t, int width, int height,
            TextureFormat format = TextureFormat.RGBA32, bool mipmap = false) 
        {
            if (t == null) {
                t = new Texture2D(width, height, format, mipmap);
                return true;
            } else {
                if (t.width != width || t.height != height 
                                     || t.format != format || (t.mipmapCount > 1 != mipmap)) {
                    t.Resize(width, height, format, mipmap);
                    t.Apply(mipmap);
                    return true;
                }
            }

            return false;
        }
        
        public static bool ReCreateIfNeed(ref RenderTexture t, int width, int height, int depth = 0,
            RenderTextureFormat format = RenderTextureFormat.ARGB32) 
        {
            if (t == null || t.width != width || t.height != height || t.format != format) {
                if (t != null)
                    t.Release();
                t = new RenderTexture(width, height, depth, format);
                return true;
            }
            return false;
        }
        
        public static bool ReCreateIfNeed<T>(ref NativeArray<T> a, int len, 
            Allocator allocator = Allocator.Persistent, 
            NativeArrayOptions opt = NativeArrayOptions.ClearMemory) where T : struct 
        {
            if (!a.IsCreated || a.Length != len) {
                if (a.IsCreated)
                    a.Dispose();
                a = new NativeArray<T>(len, allocator, opt);
                return true;
            }
            return false;
        }
        
        public static int GetPixelsCount(this Texture t) {
            var len = t.width * t.height;
            switch (t.dimension) {
                case TextureDimension.Tex2D:
                    break;
                case TextureDimension.Tex3D:
                    len *= ((Texture3D) t).depth;
                    break;
                case TextureDimension.Cube:
                    len *= 6;
                    break;
                case TextureDimension.Tex2DArray:
                    len *= ((Texture2DArray) t).depth;
                    break;
                case TextureDimension.CubeArray:
                    len *= ((CubemapArray) t).cubemapCount * 6;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return len;
        }
        
        public static int GetLengthInBytes(this Texture t) {
            return t.GetPixelsCount() * t.GetBytesPerPixel();
        }
        
        public static int GetBytesPerPixel(this Texture t) {
            return (int) GraphicsFormatUtility.GetBlockSize(t.graphicsFormat);
        }
    }
}