using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utilities {
    public class DelayedDisposeNativeObject<T> : IDisposable {
        public delegate void DisposeAction(ref T o);
        
        public T o;
        public bool DontDisposeObject = false;

        private int _locks;
        private bool _canDispose;
        private readonly DisposeAction _disposeAction;
        private GCHandle _handle;
        private bool _isDisposed;

        public DelayedDisposeNativeObject(DisposeAction disposeAction) {
            _disposeAction = disposeAction;
            _handle = GCHandle.Alloc(this);
        }
        
        private DelayedDisposeNativeObject() {}

        public void LockDispose() {
            ++_locks;
            if (_canDispose) {
                Debug.LogWarning("DelayedDisposeNativeObject LockDispose invokes after Dispose");
            }
        }

        public void UnLockDispose() {
            --_locks;
            if (_locks < 0) {
                Debug.LogWarning("DelayedDisposeNativeObject UnLockDispose invokes more times then locks");
                _locks = 0;
            }
            DisposeIfNeed();
        }

        public bool Unlocked() => _locks == 0;
        
        public static bool operator ==(DelayedDisposeNativeObject<T> o1, DelayedDisposeNativeObject<T> o2) {
            if (ReferenceEquals(o1, null)) {
                if (!ReferenceEquals(o2, null)) {
                    return o2._isDisposed;
                }
            } else {
                if (ReferenceEquals(o2, null)) {
                    return o1._isDisposed;
                }
            }

            return ReferenceEquals(o1, o2);
        }

        public static bool operator !=(DelayedDisposeNativeObject<T> o1, DelayedDisposeNativeObject<T> o2) {
            return !(o1 == o2);
        }

        public void Dispose() {
            _canDispose = true;
            DisposeIfNeed();
        }

        private void DisposeIfNeed() {
            if (_canDispose && Unlocked()) {
                if (!DontDisposeObject)
                    _disposeAction(ref o);
                _isDisposed = true;
                _handle.Free();
            }
        }
    }
    
    public static class DelayedDisposeNativeObject {
        public static void DisposeUnityObject<T>(ref T o) where T : Object {
            if (o != null) {
                Object.Destroy(o);
                o = null;
            }
        }
        
        public static void DisposeRenderTexture(ref RenderTexture o) {
            if (o != null) {
                o.Release();
                o = null;
            }
        }
        
        public static void DisposeNativeArray<T>(ref NativeArray<T> o) where T : struct {
            if (o.IsCreated) {   
                o.Dispose();
            }
        }

        public static void DefaultDispose(ref IDisposable o) {
            if (o != null) {   
                o.Dispose();
                o = null;
            }
        }
    }

    public class DelayedDisposeNativeArray<T> : DelayedDisposeNativeObject<NativeArray<T>> where T : struct {
        public DelayedDisposeNativeArray() : base(DelayedDisposeNativeObject.DisposeNativeArray) {}
    }
    
    public class DelayedDisposeRenderTexture : DelayedDisposeNativeObject<RenderTexture> {
        public DelayedDisposeRenderTexture() : base(DelayedDisposeNativeObject.DisposeRenderTexture) {}
    }
}