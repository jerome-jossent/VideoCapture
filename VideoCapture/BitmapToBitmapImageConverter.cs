using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace VideoCapture
{
   public class BitmapToBitmapImageConverter : IValueConverter
    {
        public BitmapImage ConvertBitmapToBitMapImage(System.Drawing.Bitmap bitmap)
        {
            return ImageProcessing.ImageConversion.Bitmap_to_ImageSource(bitmap);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage img = new BitmapImage();
            if (value != null)
            {
                img = this.ConvertBitmapToBitMapImage(value as System.Drawing.Bitmap);
            }
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
