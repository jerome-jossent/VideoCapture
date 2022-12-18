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
                return Static && filtre_TXT_Type == Filtre_TXT_Type.Free;
            }
        }

        public enum Filtre_TXT_Type { Free, DeviceName, Date, Time, Time_ms, Date_Time, Date_Time_ms, FrameNumber, FPS }
        public Filtre_TXT_Type filtre_TXT_Type
        {
            get { return _filtre_TXT_Type; }
            set
            {
                _filtre_TXT_Type = value;
                switch (filtre_TXT_Type)
                {
                    case Filtre_TXT_Type.Free:
                    case Filtre_TXT_Type.DeviceName:
                        Dynamic = false;
                        break;
                    case Filtre_TXT_Type.Date:
                    case Filtre_TXT_Type.Time:
                    case Filtre_TXT_Type.Time_ms:
                    case Filtre_TXT_Type.Date_Time:
                    case Filtre_TXT_Type.Date_Time_ms:
                    case Filtre_TXT_Type.FrameNumber:
                    case Filtre_TXT_Type.FPS:
                        Dynamic = true;

                        break;
                }

                UpdateTitle();
                OnPropertyChanged("filtre_TXT_Type");
            }
        }
        Filtre_TXT_Type _filtre_TXT_Type;

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

        #region POLICE
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
        int fontThickness = 2;

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
                if (_color == value) return;

                byte a = (byte)(alpha * 255);
                _color = Color.FromArgb(a, value.R, value.G, value.B);

                OnPropertyChanged("color");

                UpdateTitle();
            }
        }
        Color _color = Colors.White;

        #endregion

        #region BORDURE
        public bool Border
        {
            get { return border; }
            set
            {
                border = value;
                UpdateTitle();
                OnPropertyChanged("Border");
            }
        }
        bool border = false;

        public int FontThickness_Border
        {
            get { return fontThickness_Border; }
            set
            {
                fontThickness_Border = value;
                OnPropertyChanged("FontThickness_Border");
            }
        }
        int fontThickness_Border = 5;

        public double Alpha_Border
        {
            get { return alpha_Border; }
            set
            {
                alpha_Border = value;

                byte a = (byte)(alpha_Border * 255);
                color_Border = Color.FromArgb(a, color_Border.R, color_Border.G, color_Border.B);
                UpdateTitle();
                OnPropertyChanged("Alpha_Border");
            }
        }
        double alpha_Border = 1;

        public Color color_Border
        {
            get { return _color_Border; }
            set
            {
                if (_color_Border == value) return;

                byte a = (byte)(alpha_Border * 255);
                _color_Border = Color.FromArgb(a, value.R, value.G, value.B);

                OnPropertyChanged("color_Border");

                UpdateTitle();
            }
        }
        Color _color_Border = Colors.Black;

        #endregion



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
            switch (filtre_TXT_Type)
            {
                case Filtre_TXT_Type.Free:
                    t = txt;
                    break;
                case Filtre_TXT_Type.DeviceName:
                case Filtre_TXT_Type.Date:
                case Filtre_TXT_Type.Time:
                case Filtre_TXT_Type.Time_ms:
                case Filtre_TXT_Type.Date_Time:
                case Filtre_TXT_Type.Date_Time_ms:
                case Filtre_TXT_Type.FrameNumber:
                case Filtre_TXT_Type.FPS:
                    t = filtre_TXT_Type.ToString();
                    break;
            }
            string o = origine.ToString();
            title = "xy(" +
                X.ToString("0.000") +
                "|" +
                Y.ToString("0.000") +
                ") " +
                new String(o.Where(c => char.IsUpper(c)).ToArray()) +
                (Dynamic ? "\t[D] " : "\t[S] ") +
                t;
        }
    }
}