using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapture
{
    public class Filtre_IMAGE : Filtre
    {

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

        public Filtre_IMAGE()
        {
            _type = FiltreType.image;
            isImage = true;
            X = 0.5;
            Y = 0.5;
            H = 0.1;
            W = 0.1;
        }

        public override void UpdateTitle()
        {
            title = "Filtre_IMAGE";
        }
    }
}
