using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class MemUtils {
    public static NativeArray<T> ConvertPtrToNativeArray<T>(IntPtr ptr, int length, Allocator allocator = Allocator.Invalid) where T : struct {
        //TODO: ConvertExistingDataToNativeArray not create m_Safety that provides errors read/write 
        unsafe {
            var array =  NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr.ToPointer(), length, allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var m_Safety = allocator == Allocator.Temp ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref array, m_Safety);
#endif
            return array;
        }
    }

    public static IntPtr IntPtr<T>(this NativeArray<T> a) where T : struct {
        unsafe {
            return new IntPtr(a.GetUnsafePtr());
        }
    }
    
    public static long GetLengthInBytes<T>(this NativeArray<T> a) where T : struct {
        return (long)Marshal.SizeOf<T>() * a.Length;
    }
    
    public static long GetLengthInBytes<T>(this T[] a) {
        return Marshal.SizeOf<T>() * a.LongLength;
    }
    
    public static void Copy<T>(IntPtr src, T[] dst) {
        var dstBytes = dst.GetLengthInBytes();
        CopyBytes(src, dst, dstBytes);
    }
    
    public static void Copy<T>(IntPtr src, NativeArray<T> dst) where T : struct {
        var dstBytes = dst.GetLengthInBytes();
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
    
    public static void CopyBytes<T1, T2>(T1[] src, T2[] dst, long copyBytes = -1) {
        var srcBytes = src.GetLengthInBytes();
        if (copyBytes < 0)
            copyBytes = srcBytes;
        var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
        CopyBytes(handle.AddrOfPinnedObject(), dst, srcBytes);
        handle.Free();
    }
    
    public static void CopyBytes<T1, T2>(T1[] src, NativeArray<T2> dst, long copyBytes = -1) where T2 : struct {
        var srcBytes = src.GetLengthInBytes();
        if (copyBytes < 0)
            copyBytes = srcBytes;
        var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
        CopyBytes(handle.AddrOfPinnedObject(), dst, copyBytes);
        handle.Free();
    }
    
    public static void CopyBytes<T>(IntPtr src, T[] dst, long copyBytes) {
        var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
        var dstBytes = dst.GetLengthInBytes();
        CopyBytes(src, handle.AddrOfPinnedObject(), dstBytes, copyBytes);
        handle.Free();
    }
    
    public static void CopyBytes<T>(IntPtr src, NativeArray<T> dst, long copyBytes) where T : struct {
        unsafe {
            var dstBytes = dst.GetLengthInBytes();
            CopyBytes(src, dst.GetUnsafePtr(), dstBytes, copyBytes);
        }
    }
    
    public static void CopyBytes(IntPtr src, IntPtr dst, long destBytes, long copyBytes) {
        unsafe {
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), destBytes, copyBytes);
        }
    }

    public static void CopyBytes<T>(NativeArray<T> src, Stream stream, long copyBytes) where T : struct {
        CopyBytes(src.IntPtr(), stream, copyBytes);
    }
    
    public static void CopyBytes<T>(T[] src, Stream stream, long copyBytes) {
        var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
        CopyBytes(handle.AddrOfPinnedObject(), stream, copyBytes);
        handle.Free();
    }

    public static void CopyBytes(IntPtr src, Stream stream, long copyBytes) {
        unsafe {
            using (var mem = new UnmanagedMemoryStream((byte*) src.ToPointer(), copyBytes, copyBytes, FileAccess.Read)) {
                mem.CopyTo(stream);
            }
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