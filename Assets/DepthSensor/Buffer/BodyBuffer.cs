using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepthSensor.Buffer {
    public class BodyBuffer : ArrayBuffer<Body> {
        public int CountTracked { get; private set; }

        public BodyBuffer(int maxCount)
            : base(CreateBodies(maxCount)) {
        }

        private static Body[] CreateBodies(int maxCount) {
            var bodies = new Body[maxCount];
            for (int i = 0; i < maxCount; i++) {
                bodies[i] = new Body();
            }
            return bodies;
        }

        public class Internal<T> {
            protected internal delegate bool GetIdFunc(T body, out ulong id);
                
            private class UpdateInfo {
                public Body body;
                public T newBody;
            }

            private readonly BodyBuffer _buffer;
            private readonly Dictionary<ulong, UpdateInfo> _updateInfo;
            private readonly Dictionary<Body, Body.Internal> internalBody;

            protected internal Internal(BodyBuffer buffer) {
                _buffer = buffer;
                _updateInfo = new Dictionary<ulong, UpdateInfo>(buffer.length);
                for (int i = 0; i < buffer.length; i++) {
                    _updateInfo[(ulong) i] = new UpdateInfo {body = buffer.data[i]};
                }

                internalBody = buffer.data.ToDictionary(
                    body => body, 
                    body => new Body.Internal(body));
            }

            protected internal void UpdateBodiesIndexed(T[] newBodies, GetIdFunc GetId, 
                Action<Body.Internal, T> Update) 
            {
                foreach (var newBody in newBodies) {
                    ulong id;
                    UpdateInfo info;
                    if (GetId(newBody, out id) && _updateInfo.TryGetValue(id, out info))
                        info.newBody = newBody;
                }

                foreach (var newBody in newBodies) {
                    ulong id;
                    if (GetId(newBody, out id) && !_updateInfo.ContainsKey(id)) {
                        var idxInfo = _updateInfo.First(pair => pair.Value.newBody == null);
                        var info = idxInfo.Value;
                        _updateInfo.Remove(idxInfo.Key);
                        _updateInfo.Add(id, info);
                        Debug.Log("BodySensor: new user id " + id);
                        idxInfo.Value.newBody = newBody;
                    }
                }

                int countTracked = 0;
                foreach (var info in _updateInfo.Values) {
                    if (info.newBody != null) {
                        Update(internalBody[info.body], info.newBody);
                        info.newBody = default(T);
                        ++countTracked;
                    } else {
                        internalBody[info.body].Set(false);
                    }
                }

                _buffer.CountTracked = countTracked;
            }
        }
    }
}