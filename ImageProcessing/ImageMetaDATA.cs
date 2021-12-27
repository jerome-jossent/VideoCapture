using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImageProcessing
{
    public class ImageMetaDATA
    {
        public string DATAtype = "base";
        public int DATAsize;
        public int DATAstartindex;

        //constructeur vide pour Deserialization JSON 
        public ImageMetaDATA() { }

        public ImageMetaDATA(byte[] data, int startindex)
        {
            this.DATAstartindex = startindex;
            DATAsize = data.Length;
        }

        public static List<ImageMetaDATA> FromJSON(string JSON)
        {
            return JsonConvert.DeserializeObject<List<ImageMetaDATA>>(JSON);
        }

        public static string GetJSON(List<ImageMetaDATA> metaDATA_list)
        {
            return JsonConvert.SerializeObject(metaDATA_list, Formatting.Indented);
        }
    }

}
