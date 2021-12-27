using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImageProcessing
{
    public class ImageMetaData_tips : ImageMetaDATA
    {
        public new string DATAtype = "divers";
        public string comment;
        public new int DATAsize;
        public new int DATAstartindex;

        //constructeur vide pour Deserialization JSON 
        public ImageMetaData_tips() { }

        public ImageMetaData_tips(byte[] data, int startindex, string infodivers) : base(data, startindex)
        {
            this.comment = infodivers;
            this.DATAsize = data.Length;
            this.DATAstartindex = startindex;
        }

        public static new List<ImageMetaData_tips> FromJSON(string JSON)
        {
            return JsonConvert.DeserializeObject<List<ImageMetaData_tips>>(JSON);
        }

        public static string GetJSON(List<ImageMetaData_tips> metaDATA_list)
        {
            return JsonConvert.SerializeObject(metaDATA_list, Formatting.Indented);
        }
    }

    public class Image_ImageMetaData_tips
    {
        public System.Drawing.Bitmap bitmap;
        public ImageMetaData_tips metaData_Tips;
    }

}
