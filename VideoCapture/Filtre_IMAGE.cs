using Newtonsoft.Json;
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
            isImage = true;
            OnPropertyChanged("isTxt");
            OnPropertyChanged("isImage");
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
        public OpenCvSharp.Mat mat;

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

        public override void UpdateTitle()
        {
            title = "xy [" + XY.X.ToString("0.000") + "|" + XY.Y.ToString("0.000") + "]";
        }
        
        void LoadImage()
        {
            if (!System.IO.File.Exists(fileName)) return;
            System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
            title3 = fi.Name;
            mat = OpenCvSharp.Cv2.ImRead(fileName, OpenCvSharp.ImreadModes.Unchanged);
            _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        }
    }
}