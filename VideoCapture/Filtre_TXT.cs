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

        public Filtre_TXT(string texte)
        {
            txt = texte;
            X = 0.5;
            Y = 0.5;
            H = 0.1;
            W = 0.1;
        }

        public override void UpdateTitle()
        {
            title = "xy(" +
                X.ToString("0.000") +
                "|" +
                Y.ToString("0.000") +
                ")\twh(" +
                W.ToString("0.000") +
                "|" +
                H.ToString("0.000") +
                ")\t" +
                txt +
                "\t" +
                color.ToString();
        }
    }
}
