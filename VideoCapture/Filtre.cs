using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VideoCapture
{
    public abstract class Filtre : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [JsonIgnore]
        public bool isImage// pour Filtre_liste_UC.xaml
        {
            get { return _type == FiltreType.image; }
        }

        [JsonIgnore]
        public bool isTxt// pour Filtre_liste_UC.xaml
        {
            get { return _type == FiltreType.texte; }
        }

        [JsonIgnore]
        public bool isRuller // pour Filtre_liste_UC.xaml
        {
            get { return _type == FiltreType.ruller; }
        }

        //[JsonIgnore]
        //public bool isImage
        //{
        //    get { return _isImage; }
        //    set
        //    {
        //        _isImage = value;
        //        OnPropertyChanged("isImage");
        //    }
        //}
        //bool _isImage;

        //[JsonIgnore]
        //public bool isTxt
        //{
        //    get { return _isTxt; }
        //    set
        //    {
        //        _isTxt = value;
        //        OnPropertyChanged("isTxt");
        //    }
        //}
        //bool _isTxt;

        public bool Dynamic;

        public enum TypeOrigine { UpLeft, UpMiddle, UpRight, MiddleLeft, Middle, MiddleRight, DownLeft, DownMiddle, DownRight }
        public TypeOrigine origine
        {
            get { return _origine; }
            set
            {
                //changement de type d'origine avec filtre initialisé
                if (Size.Width != 2)
                {
                    Point A = OrigineToDirection(origine);
                    Point Z = OrigineToDirection(value);
                    Point V = (Point)(Z - A);
                    XY = new Point(XY.X + Size.Width * V.X / 2, XY.Y + Size.Height * V.Y / 2);
                }

                _origine = value;
                OnPropertyChanged("origine");
                UpdateTitle();
            }
        }
        TypeOrigine _origine;

        public static Point OrigineToDirection(TypeOrigine origine)
        {
            switch (origine)
            {
                case TypeOrigine.UpLeft: return new Point(-1, -1);
                case TypeOrigine.UpMiddle: return new Point(0, -1);
                case TypeOrigine.UpRight: return new Point(1, -1);
                case TypeOrigine.MiddleLeft: return new Point(-1, 0);
                case TypeOrigine.MiddleRight: return new Point(1, 0);
                case TypeOrigine.DownLeft: return new Point(-1, 1);
                case TypeOrigine.DownMiddle: return new Point(0, 1);
                case TypeOrigine.DownRight: return new Point(1, 1);
                case TypeOrigine.Middle:
                default:
                    return new Point(0, 0);
            }
        }

        public enum FiltreType { texte, image, ruller }
        public FiltreType _type;

        public bool enable
        {
            get { return _enable; }
            set
            {
                _enable = value;
                OnPropertyChanged("enable");
            }
        }
        bool _enable = true;

        [JsonIgnore]
        public string title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("title");
            }
        }
        string _title;

        [JsonIgnore]
        public string title1
        {
            get { return _title1; }
            set
            {
                _title1 = value;
                OnPropertyChanged("title1");
            }
        }
        string _title1;

        [JsonIgnore]
        public string title2
        {
            get { return _title2; }
            set
            {
                _title2 = value;
                OnPropertyChanged("title2");
            }
        }
        string _title2;

        [JsonIgnore]
        public string title3
        {
            get { return _title3; }
            set
            {
                _title3 = value;
                OnPropertyChanged("title3");
            }
        }
        string _title3;

        public Point XY
        {
            get { return xy; }
            set
            {
                xy = value;
                UpdateTitle();
                OnPropertyChanged("XY");
            }
        }
        Point xy;

        [JsonIgnore]
        public Size Size = new Size(2, 2);

        public abstract void UpdateTitle();

        public Filtre Clone()
        {
            var jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
            string txt = JsonConvert.SerializeObject(this, Formatting.Indented, jset);
            Filtre f = (Filtre)JsonConvert.DeserializeObject(txt, jset);
            return f;
        }
    }
}