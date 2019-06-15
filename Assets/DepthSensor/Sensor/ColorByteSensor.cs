using System;
using UnityEngine;

namespace DepthSensor.Sensor {
    public class ColorByteSensor : Sensor<byte> {
        public readonly int countPerPixel;
        public readonly TextureFormat format;
        public new event Action<ColorByteSensor> OnNewFrame;
        
        protected internal ColorByteSensor(int width, int height, int countPerPixel, TextureFormat format) : 
            base(width, height, new byte[width * height * countPerPixel]) 
        {
            this.countPerPixel = countPerPixel;
            this.format = format;
            base.OnNewFrame += sensor => {
                if (OnNewFrame != null) OnNewFrame((ColorByteSensor) sensor);
            };
        }
    }
}