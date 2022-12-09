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

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract void UpdateTitle();

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

        public double W
        {
            get { return w; }
            set
            {
                w = value;
                UpdateTitle();
                OnPropertyChanged("W");
            }
        }
        double w;

        public double H
        {
            get { return h; }
            set
            {
                h = value;
                UpdateTitle();
                OnPropertyChanged("H");
            }
        }
        double h;

        public enum FiltreType { texte, image }
        public FiltreType _type;
    }
}
