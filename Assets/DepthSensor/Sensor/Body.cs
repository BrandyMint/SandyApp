using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepthSensor.Sensor {
    public class Body {
        public readonly Dictionary<Joint.Type, Joint> joints;
        public bool IsTracked { get; private set; }
        public ulong TrackedId { get; private set; }

        protected internal Body() {
            joints = new Dictionary<Joint.Type, Joint>(GetJointsCount());
            foreach (Joint.Type jointType in Enum.GetValues(typeof(Joint.Type))) {
                joints.Add(jointType, new Joint(jointType));
            }
        }

        private static int GetJointsCount() {
            return Enum.GetNames(typeof(Joint.Type)).Length;
        } 
        
        public class Internal {
            private readonly Body _body;
            private readonly Dictionary<Joint.Type, Joint.Internal> _joints;

            protected internal Internal(Body body) {
                _body = body;
                _joints = body.joints.Values.ToDictionary(
                    joint => joint.type, 
                    joint => new Joint.Internal(joint));
            }
            
            protected internal void Set(bool isTracked) {
                _body.IsTracked = isTracked;
            }

            protected internal void Set(bool isTracked, ulong trackedId) {
                _body.IsTracked = isTracked;
                _body.TrackedId = trackedId;
            }

            protected internal void SetJoint(Joint.Type type, bool isTracked, Vector3 pos) {
                _joints[type].Set(isTracked, pos);
            }
        }
    }
}