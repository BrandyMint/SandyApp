using UnityEngine;

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
    }
}