using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VideoCapture
{
    public class Filtre_TXT : Filtre
    {
        public new bool Dynamic
        {
            get { return dynamic; }
            set
            {
                dynamic = value;
                base.Dynamic = dynamic;
                OnPropertyChanged("Dynamic");
                OnPropertyChanged("Filtre_TXT_Static_Free");
                UpdateTitle();
            }
        }
        bool dynamic;

        [JsonIgnore]
        public bool Static
        {
            get { return !Dynamic; }
        }

        [JsonIgnore]
        public bool Filtre_TXT_Static_Free
        {
            get
            {
                return Static && filtre_TXT_Static_Type == Filtre_TXT_Static_Type.Free;
            }
        }

        public enum Filtre_TXT_Static_Type { Free, DeviceName }
        public enum Filtre_TXT_Dynamic_Type { Date, Time, Time_ms, Date_Time, Date_Time_ms, FrameNumber, FPS }

        public Filtre_TXT_Static_Type filtre_TXT_Static_Type
        {
            get { return _filtre_TXT_Static_Type; }
            set
            {
                _filtre_TXT_Static_Type = value;
                UpdateTitle();
                OnPropertyChanged("filtre_TXT_Static_Type");
                OnPropertyChanged("Filtre_TXT_Static_Free");
            }
        }
        Filtre_TXT_Static_Type _filtre_TXT_Static_Type;

        public Filtre_TXT_Dynamic_Type filtre_TXT_Dynamic_Type
        {
            get { return _filtre_TXT_Dynamic_Type; }
            set
            {
                _filtre_TXT_Dynamic_Type = value;
                UpdateTitle();
                OnPropertyChanged("filtre_TXT_Dynamic_Type");
                OnPropertyChanged("Filtre_TXT_Static_Free");
            }
        }
        Filtre_TXT_Dynamic_Type _filtre_TXT_Dynamic_Type;

        public string txt
        {
            get { return _txt; }
            set
            {
                _txt = value;
                UpdateTitle();
                OnPropertyChanged("txt");
            }
        }
        string _txt;

        public OpenCvSharp.HersheyFonts font
        {
            get { return _font; }
            set
            {
                _font = value;
                OnPropertyChanged("font");
            }
        }
        OpenCvSharp.HersheyFonts _font = OpenCvSharp.HersheyFonts.HersheyDuplex;

        public double FontScale
        {
            get { return fontScale; }
            set
            {
                fontScale = value;
                OnPropertyChanged("FontScale");
            }
        }
        double fontScale = 2;

        public int FontThickness
        {
            get { return fontThickness; }
            set
            {
                fontThickness = value;
                OnPropertyChanged("FontThickness");
            }
        }
        int fontThickness = 3;

        public double Alpha
        {
            get { return alpha; }
            set
            {
                alpha = value;

                byte a = (byte)(alpha * 255);
                color = Color.FromArgb(a, color.R, color.G, color.B);
                UpdateTitle();
                OnPropertyChanged("Alpha");
            }
        }
        double alpha = 1;

        public Color color
        {
            get { return _color; }
            set
            {
                byte a = (byte)(alpha * 255);
                _color = Color.FromArgb(a, value.R, value.G, value.B);
                UpdateTitle();
                OnPropertyChanged("color");
            }
        }
        Color _color = Colors.Black;

        public Filtre_TXT()
        {
            X = 0.5;
            Y = 0.5;
            _type = FiltreType.texte;
            isTxt = true;
            origine = TypeOrigine.DownLeft;
        }

        public override void UpdateTitle()
        {
            string t = "";
            if (Dynamic)
            {

                switch (filtre_TXT_Dynamic_Type)
                {
                    case Filtre_TXT_Dynamic_Type.Date:
                        t = "Date";
                        break;
                    case Filtre_TXT_Dynamic_Type.Time:
                        t = "Time";
                        break;
                    case Filtre_TXT_Dynamic_Type.Time_ms:
                        t = "Time_ms";
                        break;
                    case Filtre_TXT_Dynamic_Type.Date_Time:
                        t = "Date_Time";
                        break;
                    case Filtre_TXT_Dynamic_Type.Date_Time_ms:
                        t = "Date_Time_ms";
                        break;
                    case Filtre_TXT_Dynamic_Type.FrameNumber:
                        t = "FrameNumber";
                        break;
                    case Filtre_TXT_Dynamic_Type.FPS:
                        t = "FPS";
                        break;
                }
            }
            else
            {
                switch (filtre_TXT_Static_Type)
                {
                    case Filtre_TXT_Static_Type.Free:
                        t = txt;
                        break;
                    case Filtre_TXT_Static_Type.DeviceName:
                        t = "DeviceName";
                        break;
                }
            }

            title = "xy(" +
                X.ToString("0.000") +
                "|" +
                Y.ToString("0.000") +
                ")\t" +
                color.ToString() +
                (Dynamic ? "\t[D] " : "\t[S] ") +
                t;
        }

        //public override object Clone()
        //{
        //    return this.Clone();
        //}
    }
}
