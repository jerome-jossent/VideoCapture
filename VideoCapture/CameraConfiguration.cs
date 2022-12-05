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
        public string format;
        public int width, height;

        public CameraConfiguration(string txt)
        {
            string[] config = txt.Split(new string[] { "{}" }, StringSplitOptions.None);
            deviceName = config[0];
            format = config[1];
            width = Convert.ToInt32(config[2]);
            height = Convert.ToInt32(config[3]);
        }

        public CameraConfiguration(string deviceName, string format, int width, int height)
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
