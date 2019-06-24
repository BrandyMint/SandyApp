using System;
using System.Runtime.InteropServices;

public static class MemUtils {
    public static void Copy<T>(IntPtr src, T[] dst) {
        var dstBytes = dst.LongLength * Marshal.SizeOf<T>();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void Copy<T>(IntPtr src, T[] dst, long len) {
        var dstBytes = len * Marshal.SizeOf<T>();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void CopyBytes<T>(IntPtr src, T[] dst, long copyBytes) {
        var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
        var dstBytes = dst.LongLength * Marshal.SizeOf<T>();
        CopyBytes(src, handle.AddrOfPinnedObject(), dstBytes, copyBytes);
        handle.Free();
    }
    
    public static void CopyBytes(IntPtr src, IntPtr dst, long destBytes, long copyBytes) {
        unsafe {
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), destBytes, copyBytes);
        }
    }
}