using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepthSensor.Sensor {
    public class BodySensor : Sensor<Body> {
        public int CountTracked { get; private set; }
        public new event Action<BodySensor> OnNewFrame;

        protected internal BodySensor(int maxCount)
            : base(maxCount, 1, CreateBodies(maxCount)) {
            base.OnNewFrame += sensor => {
                if (OnNewFrame != null) OnNewFrame((BodySensor) sensor);
            };
        }

        public BodySensor(bool available) : this(0) {
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

            private readonly BodySensor _sensor;
            private readonly Dictionary<ulong, UpdateInfo> _updateInfo;
            private readonly Dictionary<Body, Body.Internal> internalBody;

            protected internal Internal(BodySensor sensor) : base(sensor) {
                _sensor = sensor;
                _updateInfo = new Dictionary<ulong, UpdateInfo>(sensor.data.Length);
                for (int i = 0; i < sensor.data.Length; i++) {
                    _updateInfo[(ulong) i] = new UpdateInfo {body = sensor.data[i]};
                }

                internalBody = sensor.data.ToDictionary(
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
                _sensor.CountTracked = countTracked;
            }
        }
    }
}