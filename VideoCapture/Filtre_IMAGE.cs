﻿using System;
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
            isImage = true;
        }

        public override void UpdateTitle()
        {
            title = "Filtre_IMAGE";
        }
    }
}
