using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
    public class ImagesSerialization
    {
        public static string Message;
        public static char cara = '§';

        public static byte[] SerializeImageFiles(List<System.Drawing.Bitmap> Bitmaps, List<string> MetaDatas, string MetaDataGlobal)
        {
            byte[] img_bytes_append = null;
            List<ImageMetaData_tips> metaDATA_list = new List<ImageMetaData_tips>();

            //chargement des fichiers, lecture en byte
            for (int i = 0; i < Bitmaps.Count; i++)
            {
                byte[] img_bytes = ImageConversion.DrawingImage_to_ByteArray2(Bitmaps[i]);

                //création des métadonnées
                ImageMetaData_tips meta = new ImageMetaData_tips(img_bytes,
                                            (img_bytes_append == null) ? 0 : img_bytes_append.Length,
                                            MetaDatas[i]);

                Console.WriteLine("startindex : " + meta.DATAstartindex + "\tlength : " + img_bytes.Length);

                metaDATA_list.Add(meta);

                //ajout des bytes à la suite des précédents
                Bytes.Add(ref img_bytes_append, img_bytes);
            }

            //la dernière métaDATA en + est global (Nbr d'images = Nbr MetaDatas - 1)
            ImageMetaData_tips metaglobal = new ImageMetaData_tips() { comment = MetaDataGlobal };
            metaDATA_list.Add(metaglobal);

            //json des métadonnées
            string json = ImageProcessing.ImageMetaData_tips.GetJSON(metaDATA_list);

            //Json to ByteArray
            byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(json);

            //Taille Json
            byte[] json_bytes_size = BitConverter.GetBytes(json_bytes.Length);

            //Concatenation finale
            byte[] FullDATA = Bytes.Combine(new byte[][] { json_bytes_size, json_bytes, img_bytes_append });

            return FullDATA;
        }

        public static byte[] SerializeImageFiles(List<System.Drawing.Bitmap> Bitmaps, List<string> MetaDatas)
        {
            byte[] img_bytes_append = null;
            List<ImageMetaData_tips> metaDATA_list = new List<ImageMetaData_tips>();

            //chargement des fichiers, lecture en byte
            for (int i = 0; i < Bitmaps.Count; i++)
            {
                //byte[] img_bytes = ImageConversion.DrawingImage_to_ByteArray(Bitmaps[i]);
                byte[] img_bytes = ImageConversion.DrawingImage_to_ByteArray2(Bitmaps[i]);

                //création des métadonnées
                ImageMetaData_tips meta = new ImageMetaData_tips(img_bytes,
                                            (img_bytes_append == null) ? 0 : img_bytes_append.Length,
                                            MetaDatas[i]);

                metaDATA_list.Add(meta);

                //ajout des bytes à la suite des précédents
                Bytes.Add(ref img_bytes_append, img_bytes);
            }

            //json des métadonnées
            string json = ImageProcessing.ImageMetaData_tips.GetJSON(metaDATA_list);

            //Json to ByteArray
            byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(json);

            //Taille Json
            byte[] json_bytes_size = BitConverter.GetBytes(json_bytes.Length);

            //Concatenation finale
            byte[] FullDATA = Bytes.Combine(new byte[][] { json_bytes_size, json_bytes, img_bytes_append });

            return FullDATA;
        }

        public static List<Image_ImageMetaData_tips> DeserializeImageFilesWithMetaData(byte[] data)
        {
            List<Image_ImageMetaData_tips> i_md_ts = new List<Image_ImageMetaData_tips>();

            //les 4 premiers bytes contiennent la taille du JSON
            byte[] json_bytes_size = new byte[4];
            Buffer.BlockCopy(data, 0, json_bytes_size, 0, 4);

            //récupération du JSON
            byte[] json_bytes = new byte[BitConverter.ToInt32(json_bytes_size, 0)];
            Buffer.BlockCopy(data, 4, json_bytes, 0, json_bytes.Length);
            string json = System.Text.Encoding.UTF8.GetString(json_bytes);

            //cas où il ne s'agit que d'un message
            if (json[0] == cara)
            {
                Message = json.Substring(1);
                return null;
            }

            int firstBracketIndex = json.IndexOf('[') -1;
            int lastBracketIndex = json.LastIndexOf(']') + 1;
            //déserialisation des metaDATA
            List<ImageMetaData_tips> metaDATA_list = ImageMetaData_tips.FromJSON(json.Substring(firstBracketIndex, lastBracketIndex - firstBracketIndex));

            //dans les métaDATAS se trouvent (entre autre) les tailles de chaque DATA
            List<byte[]> img_bytes_list = new List<byte[]>();
            for (int i = 0; i < metaDATA_list.Count; i++)
            {
                Image_ImageMetaData_tips i_md_t = new Image_ImageMetaData_tips();
                i_md_t.metaData_Tips = metaDATA_list[i];

                if (i_md_t.metaData_Tips.DATAsize > 0)
                {
                    //création d'une Array de bytes vide selon la taille de la DATA
                    byte[] img_bytes = new byte[i_md_t.metaData_Tips.DATAsize];
                    //lecture dans les bytes reçus au bon endroit
                    Buffer.BlockCopy(data, 4 + json_bytes.Length + i_md_t.metaData_Tips.DATAstartindex, img_bytes, 0, img_bytes.Length);

                    //conversion en image
                    i_md_t.bitmap = ImageConversion.ByteArray_to_Bitmap(img_bytes);
                }
                i_md_ts.Add(i_md_t);
            }
            return i_md_ts;
        }
    }
}
