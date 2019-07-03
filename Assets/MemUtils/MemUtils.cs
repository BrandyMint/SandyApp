using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class MemUtils {
    public static void Copy<T>(IntPtr src, T[] dst) {
        var dstBytes = dst.LongLength * Marshal.SizeOf<T>();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void Copy<T>(IntPtr src, NativeArray<T> dst) where T : struct {
        var dstBytes = dst.Length * Marshal.SizeOf<T>();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void Copy<T>(IntPtr src, T[] dst, long len) {
        var dstBytes = len * Marshal.SizeOf<T>();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void Copy<T>(IntPtr src, NativeArray<T> dst, long len) where T : struct {
        var dstBytes = len * Marshal.SizeOf<T>();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void CopyBytes<T>(IntPtr src, T[] dst, long copyBytes) {
        var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
        var dstBytes = dst.LongLength * Marshal.SizeOf<T>();
        CopyBytes(src, handle.AddrOfPinnedObject(), dstBytes, copyBytes);
        handle.Free();
    }
    
    public static void CopyBytes<T>(IntPtr src, NativeArray<T> dst, long copyBytes) where T : struct {
        unsafe {
            var dstBytes = dst.Length * Marshal.SizeOf<T>();
            CopyBytes(src, dst.GetUnsafePtr(), dstBytes, copyBytes);
        }
    }
    
    public static void CopyBytes(IntPtr src, IntPtr dst, long destBytes, long copyBytes) {
        unsafe {
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), destBytes, copyBytes);
        }
    }
    
    private static unsafe void CopyBytes(IntPtr src, void* dst, long destBytes, long copyBytes) {
        Buffer.MemoryCopy(src.ToPointer(), dst, destBytes, copyBytes);
    }
    
    private static unsafe void CopyBytes(void* src, IntPtr dst, long destBytes, long copyBytes) {
        Buffer.MemoryCopy(src, dst.ToPointer(), destBytes, copyBytes);
    }
    
    private static unsafe void CopyBytes(void* src, void* dst, long destBytes, long copyBytes) {
        Buffer.MemoryCopy(src, dst, destBytes, copyBytes);
    }
}