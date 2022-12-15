using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapture
{
    public abstract class Filtre : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        [JsonIgnore]
        public bool isImage;
        [JsonIgnore]
        public bool isTxt;

        public bool Dynamic;

        public enum TypeOrigine { UpLeft, UpMiddle, UpRight, MiddleLeft, Middle, MiddleRight, DownLeft, DownMiddle, DownRight}
        public TypeOrigine origine {

            get { return _origine; }
            set
            {
                _origine = value;
                OnPropertyChanged("origine");
            }
        }
        TypeOrigine _origine;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract void UpdateTitle();

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

        public double X
        {
            get { return x; }
            set
            {
                x = value;
                UpdateTitle();
                OnPropertyChanged("X");
            }
        }
        double x;

        public double Y
        {
            get { return y; }
            set
            {
                y = value;
                UpdateTitle();
                OnPropertyChanged("Y");
            }
        }
        double y;

        public enum FiltreType { texte, image }
        public FiltreType _type;
    }
}
