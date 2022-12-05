using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapture
{
   public abstract class Filtre
    {
        public double X, Y, W, H;
        public enum FiltreType { texte, image}
        public FiltreType _type;
    }
}
