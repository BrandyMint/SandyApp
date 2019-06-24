using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using DepthSensor.Device;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensor.Emulated {
    public class EmulatedKinectDevice : DepthSensorDevice { 
        private const string _SNAP_PATH = "DepthSensor/Emulated/Snaps/";
        
        private byte[] _index;
        private ushort[] _depth;

        public EmulatedKinectDevice() : base("Kinect2", Init()) {
        }

        private static InitInfo Init() {
            return new InitInfo {
                Color = new ColorByteSensor(1920, 1080, 4, TextureFormat.RGBA32),
                Index = new Sensor<byte>(512, 424),
                Depth = new Sensor<ushort>(512, 424)
            };
        }

        public override bool IsAvailable() {
            return true;
        }

        private static object Deserialize(string f) {
            var fs = new FileStream(Application.dataPath + "/" + _SNAP_PATH + f + ".dat", 
                FileMode.Open);
            try {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(fs);
            } catch (SerializationException e) {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            } finally {
                fs.Close();
            }
        }

        protected override void SensorActiveChanged(AbstractSensor sensor) {
            //TODO: implement
        }

        protected override IEnumerator Update() {
            _index = Deserialize("Index") as byte[];
            _depth = Deserialize("Static") as ushort[];
            for (int i = 0; i < 10; ++i) {
                UpdateSensors();
                yield return new WaitForEndOfFrame();
            }
            _depth = Deserialize("Depth") as ushort[];
            while (true) {
                UpdateSensors();
                yield return new WaitForEndOfFrame();
            }
        }

        private void UpdateSensors() {
            if (Index.Active) _internalIndex.NewFrame(_index);
            if (Depth.Active) _internalDepth.NewFrame(_depth);
        }

        protected override void Close() { }

        public override Vector2 CameraPosToDepthMapPos(Vector3 pos) {
            throw new NotImplementedException();
        }

        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            throw new NotImplementedException();
        }

        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            throw new NotImplementedException();
        }
    }
}