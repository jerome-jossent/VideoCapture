using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapture
{
    public class CameraConfiguration
    {
        public string deviceName;
        public VideoInInfo.Format format;
        public double width, height;

        public CameraConfiguration() { }

        public CameraConfiguration(string deviceName, VideoInInfo.Format format, double width, double height)
        {
            this.deviceName = deviceName;
            this.format = format;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return deviceName + "{}" + format + "{}" + width + "{}" + height;
        }
    }
}
