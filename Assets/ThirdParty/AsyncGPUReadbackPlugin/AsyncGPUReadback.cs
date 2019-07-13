using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AsyncGPUReadbackPluginNs {
    // Tries to match the official API
    public class AsyncGPUReadback {

        public static AsyncGPUReadbackRequest Request(Texture src, int mipLevel = 0, 
            Action<AsyncGPUReadbackRequest> onDone = null) 
        {
            var request = new AsyncGPUReadbackRequest();
            request.Init(src, mipLevel, onDone);
            return request;
        }

        public static AsyncGPUReadbackRequest RequestIntoNativeArray<T>(ref NativeArray<T> dst, Texture src, int mipLevel = 0, 
            Action<AsyncGPUReadbackRequest> onDone = null) where T : struct 
        {
            var request = new AsyncGPUReadbackRequest();
            request.Init(src, ref dst, 0, onDone);
            return request;
        }
    }

    public class AsyncGPUReadbackRequest : IDisposable {
        /// <summary>
        /// Tell if we are using the plugin api or the official api
        /// </summary>
        private bool usePlugin;

        /// <summary>
        /// Official api request object used if supported
        /// </summary>
        private UnityEngine.Rendering.AsyncGPUReadbackRequest gpuRequest;

        /// <summary>
        /// Event Id used to tell what texture is targeted to the render thread
        /// </summary>
        private int eventId;

        /// <summary>
        /// Check if the request is done
        /// </summary>
        public bool done {
            get {
                if (usePlugin) {
                    return isRequestDone(eventId);
                } else {
                    return gpuRequest.done;
                }
            }
        }

        /// <summary>
        /// Check if the request has an error
        /// </summary>
        public bool hasError {
            get {
                if (usePlugin) {
                    return isRequestError(eventId);
                } else {
                    return gpuRequest.hasError;
                }
            }
        }

        public bool manualDispose;

        /// <summary>
        /// Create an AsyncGPUReadbackPluginRequest.
        /// Use official AsyncGPUReadback.Request if possible.
        /// If not, it tries to use OpenGL specific implementation
        /// Warning! Can only be called from render thread yet (not main thread)
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public bool Init(Texture src, int mipLevel, Action<AsyncGPUReadbackRequest> onDone) {
            if (SystemInfo.supportsAsyncGPUReadback) {
                usePlugin = false;
                gpuRequest = UnityEngine.Rendering.AsyncGPUReadback.Request(src, mipLevel, CreateCallback(onDone));
            } else if (isCompatible()) {
                unsafe {
                    MakePluginRequest(src, mipLevel, null, 0);
                }

                AsyncGPURequestLifeTimeManager.Instance.Add(this, onDone);
            } else {
                Debug.LogError("AsyncGPUReadback is not supported on your system.");
                return false;
            }

            return true;
        }

        public bool Init<T>(Texture src, ref NativeArray<T> dst, int mipLevel,
            Action<AsyncGPUReadbackRequest> onDone) where T : struct {
            if (SystemInfo.supportsAsyncGPUReadback) {
                usePlugin = false;
                gpuRequest = UnityEngine.Rendering.AsyncGPUReadback.RequestIntoNativeArray(ref dst, src, mipLevel, CreateCallback(onDone));
            } else if (isCompatible()) {
                unsafe {
                    var bytes = dst.Length * UnsafeUtility.SizeOf<T>();
                    MakePluginRequest(src, mipLevel, dst.GetUnsafePtr(), bytes);
                }

                AsyncGPURequestLifeTimeManager.Instance.Add(this, onDone);
            } else {
                Debug.LogError("AsyncGPUReadback is not supported on your system.");
                return false;
            }

            return true;
        }

        private Action<UnityEngine.Rendering.AsyncGPUReadbackRequest> CreateCallback(Action<AsyncGPUReadbackRequest> onDone) {
            if (onDone == null)
                return null;
            return req => onDone(this);
        }

        private unsafe void MakePluginRequest(Texture src, int mipLevel, void* dataDst, int lengthBytes) {
            usePlugin = true;
            int textureId = (int) (src.GetNativeTexturePtr());
            eventId = makeRequest_mainThread(textureId, mipLevel, dataDst, lengthBytes);
            GL.IssuePluginEvent(getfunction_makeRequest_renderThread(), this.eventId);
        }

        public unsafe NativeArray<T> GetData<T>() where T : struct {
            if (usePlugin) {
                // Get data from cpp plugin
                void* ptr = null;
                int length = 0;

                getData_mainThread(eventId, ref ptr, ref length);
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());

                return array;
            } else {
                return gpuRequest.GetData<T>();
            }
        }

        /// <summary>
        /// Has to be called regularly to update request status.
        /// Call this from Update() or from a corountine
        /// </summary>
        /// <param name="force">Update is automatic on official api,
        /// so we don't call the Update() method except on force mode.</param>
        public void Update(bool force = false) {
            if (usePlugin) {
                GL.IssuePluginEvent(getfunction_update_renderThread(), this.eventId);
            } else if (force) {
                gpuRequest.Update();
            }
        }

        /// <summary>
        /// Has to be called to free the allocated buffer after it has been used
        /// </summary>
        public void Dispose() {
            if (usePlugin) {
                dispose(this.eventId);
            }
        }


        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern bool isCompatible();

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern unsafe int makeRequest_mainThread(int texture, int miplevel, void* dataDst,
            int lengthBytes);

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern IntPtr getfunction_makeRequest_renderThread();

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern void makeRequest_renderThread(int event_id);

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern IntPtr getfunction_update_renderThread();

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern unsafe void getData_mainThread(int event_id, ref void* buffer, ref int length);

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern bool isRequestError(int event_id);

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern bool isRequestDone(int event_id);

        [DllImport("AsyncGPUReadbackPlugin")]
        private static extern void dispose(int event_id);
    }
}