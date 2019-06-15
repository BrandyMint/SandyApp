﻿using UnityEngine;

namespace DepthSensor.Sensor {
    public class Joint {
        public enum Type : int {
            SPINE_BASE                                =0,
            SPINE_MID                                 =1,
            NECK                                      =2,
            HEAD                                      =3,
            SHOULDER_LEFT                             =4,
            ELBOW_LEFT                                =5,
            WRIST_LEFT                                =6,
            HAND_LEFT                                 =7,
            SHOULDER_RIGHT                            =8,
            ELBOW_RIGHT                               =9,
            WRIST_RIGHT                               =10,
            HAND_RIGHT                                =11,
            HIP_LEFT                                  =12,
            KNEE_LEFT                                 =13,
            ANKLE_LEFT                                =14,
            FOOT_LEFT                                 =15,
            HIP_RIGHT                                 =16,
            KNEE_RIGHT                                =17,
            ANKLE_RIGHT                               =18,
            FOOT_RIGHT                                =19,
            SPINE_SHOULDER                            =20,
            HAND_TIP_LEFT                             =21,
            THUMB_LEFT                                =22,
            HAND_TIP_RIGHT                            =23,
            THUMB_RIGHT                               =24
        }

        public readonly Type type;
        public Vector3 Pos { get; private set; }
        public bool IsTracked { get; private set; }

        protected internal Joint(Type type) {
            this.type = type;
        }
        
        public class Internal {
            private readonly Joint _joint;

            protected internal Internal(Joint joint) {
                _joint = joint;
            }

            protected internal void Set(bool isTracked, Vector3 pos) {
                _joint.IsTracked = isTracked;
                _joint.Pos = pos;
            }
        }
    }
}