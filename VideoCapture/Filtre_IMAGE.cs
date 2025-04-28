using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapture
{
    public class Filtre_IMAGE : Filtre
    {
        public Filtre_IMAGE()
        {
            _type = FiltreType.image;
            XY = new System.Windows.Point(0.5, 0.5);
            origine = TypeOrigine.Middle;
            //isImage = true;
            //OnPropertyChanged("isTxt");
            //OnPropertyChanged("isImage");
        }

        public double Alpha
        {
            get { return alpha; }
            set
            {
                alpha = value;
                UpdateTitle();
                OnPropertyChanged("Alpha");
            }
        }
        double alpha = 1;

        public double ScaleFactor
        {
            get { return scaleFactor; }
            set
            {
                scaleFactor = value;
                UpdateTitle();
                OnPropertyChanged("ScaleFactor");
            }
        }
        double scaleFactor = 1;

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                LoadImage();
                UpdateTitle();
                OnPropertyChanged("FileName");
            }
        }
        string fileName;

        [JsonIgnore]
        public Mat mat;

        [JsonIgnore]
        public System.Drawing.Bitmap _imageSource
        {
            get
            {
                if (imageSource == null)
                    return null;
                return imageSource;
            }
            set
            {
                if (imageSource != value)
                {
                    imageSource = value;
                    OnPropertyChanged("_imageSource");
                }
            }
        }
        System.Drawing.Bitmap imageSource;

        [JsonIgnore]
        public OpenCvSharp.VideoCapture videoCapture;
        [JsonIgnore]
        public int frameCount;

        public override void UpdateTitle()
        {
            title = "xy [" + XY.X.ToString("0.000") + "|" + XY.Y.ToString("0.000") + "]";
        }

        void LoadImage()
        {
            if (!System.IO.File.Exists(fileName)) return;
            System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
            title3 = fi.Name;

            if (fi.Extension.ToLower() == ".gif" || fi.Extension.ToLower() == ".mp4")
            {
                videoCapture = new OpenCvSharp.VideoCapture(fileName);
                mat = new Mat();
                videoCapture.Read(mat);
                frameCount = videoCapture.FrameCount;
                Dynamic = true;
            }
            else
            {
                mat = Cv2.ImRead(fileName, ImreadModes.Unchanged);
                Dynamic = false;
            }
            _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        }

        int i = 100;
        double j = 0;

        internal void InsertMat(Mat filterframe)
        {
            try
            {
                if (mat == null)
                    return;

                //resize de l'image du filtre image
                Mat fi_mat_resized = new Mat();
                double w_targeted = filterframe.Width * ScaleFactor;
                double h_targeted = w_targeted * mat.Height / mat.Width;
                if (h_targeted > filterframe.Height)
                {
                    h_targeted = filterframe.Height * ScaleFactor;
                    w_targeted = h_targeted * mat.Width / mat.Height;
                }
                Size = new System.Windows.Size((double)w_targeted / filterframe.Width, (double)h_targeted / filterframe.Height);

                Cv2.Resize(mat, fi_mat_resized, new Size(w_targeted, h_targeted), interpolation: InterpolationFlags.Cubic);
                System.Windows.Point V = OrigineToDirection(origine);

                switch (mat.Channels())
                {
                    case 3:
                        Cv2.CvtColor(fi_mat_resized, fi_mat_resized, ColorConversionCodes.RGB2RGBA);
                        break;

                    case 4:
                        break;

                    default:
                        throw new Exception("TODO : fi.mat.Channels() = " + mat.Channels().ToString());
                }

                //todo => dépassement des limites

                Rect rect = new Rect((int)(XY.X * filterframe.Width - fi_mat_resized.Width * (V.X + 1) / 2),
                                     (int)(XY.Y * filterframe.Height - fi_mat_resized.Height * (V.Y + 1) / 2),
                                     (int)(Size.Width * filterframe.Width),
                                     (int)(Size.Height * filterframe.Height));

                var roi = new Mat(filterframe, rect);
                fi_mat_resized.CopyTo(roi);

                #region VIEUX CODE MANIPULATION DE PIXEL
                //var filterMat4 = new Mat<Vec4b>(filterframe);
                //var filterIndexer = filterMat4.GetIndexer();
                //byte alpha;
                //System.Windows.Point V = Filtre.OrigineToDirection(fi.origine);

                //switch (fi.mat.Channels())
                //{
                //    case 3:
                //        var mat3 = new Mat<Vec3b>(fi_mat_resized);
                //        var indexer3 = mat3.GetIndexer();

                //        alpha = (byte)(255 * fi.Alpha);
                //        for (int y = 0; y < fi_mat_resized.Height; y++)
                //        {
                //            for (int x = 0; x < fi_mat_resized.Width; x++)
                //            {
                //                //changement de repère : Dépend de l'origine
                //                int X = (int)(fi.XY.X * frame.Width) + x - (int)(fi_mat_resized.Width * (V.X + 1) / 2);
                //                int Y = (int)(fi.XY.Y * frame.Height) + y - (int)(fi_mat_resized.Height * (V.Y + 1) / 2);

                //                //coordonné du pixel dans l'image ?
                //                if (X < 0 || Y < 0 || X > frame.Width - 1 || Y > frame.Height - 1) continue;

                //                Vec3b color_origine = indexer3[y, x];
                //                Vec4b color_dest = new Vec4b() { Item0 = color_origine.Item0, Item1 = color_origine.Item1, Item2 = color_origine.Item2, Item3 = alpha };
                //                filterIndexer[Y, X] = color_dest;
                //            }
                //        }
                //        break;

                //    case 4:
                //        var mat4 = new Mat<Vec4b>(fi_mat_resized);
                //        var indexer4 = mat4.GetIndexer();

                //        for (int y = 0; y < fi_mat_resized.Height; y++)
                //        {
                //            for (int x = 0; x < fi_mat_resized.Width; x++)
                //            {
                //                Vec4b color = indexer4[y, x];

                //                alpha = color.Item3;
                //                if (alpha == 0) continue;

                //                //changement de repère : Dépend de l'origine
                //                int X = (int)(fi.XY.X * frame.Width) + x - (int)(fi_mat_resized.Width * (V.X + 1) / 2);
                //                int Y = (int)(fi.XY.Y * frame.Height) + y - (int)(fi_mat_resized.Height * (V.Y + 1) / 2);

                //                //coordonné du pixel dans l'image ?
                //                if (X < 0 || Y < 0 || X > frame.Width - 1 || Y > frame.Height - 1) continue;
                //                color.Item3 = (byte)(fi.Alpha * color.Item3);
                //                filterIndexer[Y, X] = color;
                //            }
                //        }
                //        break;

                //    default:
                //        throw new Exception("TODO : fi.mat.Channels() = " + fi.mat.Channels().ToString());
                //}
                #endregion
            }
            catch (Exception ex)
            {
            }
        }
    }
}