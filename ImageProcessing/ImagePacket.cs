﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
    /// <summary>
    /// Packet with image
    /// </summary>
    public class ImagePacket
    {
        public string hash { get; set; } = string.Empty;
        public int len { get; set; } = 0;
        public string image { get; set; } = string.Empty;
        public ImagePacket() { }
        public ImagePacket(byte[] img_sources)
        {
            hash = ImageConversion.StringHash(img_sources);
            len = img_sources.Length;
            image = ImageConversion.EncodeBytes(img_sources);
        }
        public byte[] GetRawData()
        {
            byte[] data = ImageConversion.DecodeBytes(image);

            if (data.Length != len) throw new Exception("Error data len");
            if (!ImageConversion.StringHash(data).Equals(hash)) throw new Exception("Error hash");

            return data;
        }
    }
}
