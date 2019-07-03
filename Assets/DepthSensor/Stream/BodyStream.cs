using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepthSensor.Stream {
    public class BodyStream : ArrayStream<Body> {
        public int CountTracked { get; private set; }

        public BodyStream(int maxCount)
            : base(CreateBodies(maxCount)) {
        }

        public BodyStream(bool available) : this(0) {
            Available = available;
        }

        private static Body[] CreateBodies(int maxCount) {
            var bodies = new Body[maxCount];
            for (int i = 0; i < maxCount; i++) {
                bodies[i] = new Body();
            }
            return bodies;
        }

        public class Internal<T> : Internal where T : class {
            protected internal delegate bool GetIdFunc(T body, out ulong id);
                
            private class UpdateInfo {
                public Body body;
                public T newBody;
            }

            private readonly BodyStream _stream;
            private readonly Dictionary<ulong, UpdateInfo> _updateInfo;
            private readonly Dictionary<Body, Body.Internal> internalBody;

            protected internal Internal(BodyStream stream) : base(stream) {
                _stream = stream;
                _updateInfo = new Dictionary<ulong, UpdateInfo>(stream.data.Length);
                for (int i = 0; i < stream.data.Length; i++) {
                    _updateInfo[(ulong) i] = new UpdateInfo {body = stream.data[i]};
                }

                internalBody = stream.data.ToDictionary(
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
                        info.newBody = null;
                        ++countTracked;
                    } else {
                        internalBody[info.body].Set(false);
                    }
                }
                _stream.CountTracked = countTracked;
            }
        }
    }
}