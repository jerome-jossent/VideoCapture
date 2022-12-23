using Newtonsoft.Json;
using OpenCvSharp;
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
            XY = new System.Windows.Point(0.5, 0.5);
            _type = FiltreType.texte;
            origine = TypeOrigine.DownLeft;
            isTxt = true;
            OnPropertyChanged("isTxt");
            OnPropertyChanged("isImage");
        }

        public override void UpdateTitle()
        {
            title = "xy [" + XY.X.ToString("0.000") + "|" + XY.Y.ToString("0.000") + "]";
            string o = origine.ToString();
            title1 = new string(o.Where(c => char.IsUpper(c)).ToArray());
            title2 = (Dynamic ? "[D] " : "[S] ");

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
            title3 = t;
        }

        internal void InsertText(Mat filterframe, DirectShowLib.DsDevice current_device)
        {
            try
            {
                if (Static)
                {
                    string txt = "";
                    switch (filtre_TXT_Type)
                    {
                        case Filtre_TXT_Type.Free:
                            if (this.txt == null || this.txt == "")
                                return;
                            txt = this.txt;
                            break;

                        case Filtre_TXT_Type.DeviceName:
                            txt = current_device.Name;
                            break;

                        default:
                            break;
                    }

                    InsertText(filterframe, txt);
                }
            }
            catch (Exception ex)
            {
            }
        }

        internal void InsertText(Mat filterframe, string txt)
        {
            try
            {
                Scalar ftcolor = new Scalar(color.B, color.G, color.R, color.A);
                Point p = new Point(XY.X * filterframe.Width, XY.Y * filterframe.Height);

                int FontThickness_MAX;
                if (Border)
                {
                    FontThickness_MAX = FontThickness_Border;
                    if (FontThickness > FontThickness_MAX)
                        FontThickness_MAX = FontThickness;
                }
                else
                {
                    FontThickness_MAX = FontThickness;
                }

                OpenCvSharp.Size textsize = Cv2.GetTextSize(txt, font, FontScale, FontThickness_MAX, out int Y_baseline);
                Size = new System.Windows.Size((double)textsize.Width / filterframe.Width, (double)textsize.Height / filterframe.Height);

                switch (origine)
                {
                    case TypeOrigine.UpLeft: p.Y += textsize.Height; break;
                    case TypeOrigine.UpMiddle: p.Y += textsize.Height; p.X -= textsize.Width / 2; break;
                    case TypeOrigine.UpRight: p.Y += textsize.Height; p.X -= textsize.Width; break;
                    case TypeOrigine.MiddleLeft: p.Y += textsize.Height / 2; break;
                    case TypeOrigine.Middle: p.Y += textsize.Height / 2; p.X -= textsize.Width / 2; break;
                    case TypeOrigine.MiddleRight: p.Y += textsize.Height / 2; p.X -= textsize.Width; break;
                    case TypeOrigine.DownLeft: break;
                    case TypeOrigine.DownMiddle: p.X -= textsize.Width / 2; break;
                    case TypeOrigine.DownRight: p.X -= textsize.Width; break;
                }
                if (Border)
                {
                    //bordure
                    Scalar ftcolor_border = new Scalar(color_Border.B, color_Border.G, color_Border.R, color_Border.A);
                    Cv2.PutText(filterframe, txt, p, font, FontScale, ftcolor_border, FontThickness_Border, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                    Cv2.PutText(filterframe, txt, p, font, FontScale, ftcolor, FontThickness, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                }
                else
                {
                    Cv2.PutText(filterframe, txt, p, font, FontScale, ftcolor, FontThickness, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                }

            }
            catch (Exception ex)
            {
            }
        }
    }
}