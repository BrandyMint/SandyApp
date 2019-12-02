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

        public static bool ReCreateIfNeedCompatible(ref Texture2D t, Texture tRef, TextureFormat fallbackFormat = TextureFormat.RGBA32) {
            TryGetCompatibleFormat(tRef.graphicsFormat, out var format, fallbackFormat);
            return ReCreateIfNeed(ref t, tRef.width, tRef.height, format, tRef.mipmapCount > 1);
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
        
        public static bool ReCreateIfNeedCompatible(ref RenderTexture t, Texture tRef, RenderTextureFormat fallbackFormat = RenderTextureFormat.ARGB32) {
            TryGetCompatibleFormat(tRef.graphicsFormat, out var format, fallbackFormat);
            
            var depth = 0;
            var refRend = tRef as RenderTexture;
            if (refRend != null)
                depth = refRend.depth;
            return ReCreateIfNeed(ref t, tRef.width, tRef.height, depth, format);
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

        public static bool TryGetCompatibleFormat(GraphicsFormat src, out TextureFormat dst,
            TextureFormat fallback = TextureFormat.RGBA32) {
            dst = GraphicsFormatUtility.GetTextureFormat(src);
            if (!Enum.IsDefined(typeof(TextureFormat), dst) || !SystemInfo.SupportsTextureFormat(dst)) {
                dst = fallback;
                return false;
            }

            return true;
        }
        
        public static bool TryGetCompatibleFormat(GraphicsFormat src, out RenderTextureFormat dst,
            RenderTextureFormat fallback = RenderTextureFormat.ARGB32) {
            dst = GraphicsFormatUtility.GetRenderTextureFormat(src);
            if (!Enum.IsDefined(typeof(RenderTextureFormat), dst) || !SystemInfo.SupportsRenderTextureFormat(dst)) {
                dst = fallback;
                return false;
            }

            return true;
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

        public static void Copy(RenderTexture src, Texture2D dst) {
            var prevRend = RenderTexture.active;
            RenderTexture.active = src;
            dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            dst.Apply();
            RenderTexture.active = prevRend;
        }

        public static void Clear(RenderTexture dst, Color color = default) {
            var rt = RenderTexture.active;
            RenderTexture.active = dst;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
        }
    }
}