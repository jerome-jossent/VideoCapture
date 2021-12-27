using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Data;

namespace VideoCapture
{
    #region DOC
    //First you will need to make vis a Property:

    //public bool Vis
    //{
    //    get { return _vis; }
    //    set
    //    {
    //        if (_vis != value)
    //        {
    //            _vis = value;
    //        }
    //    }
    //}
    //private bool _vis;

    //You will need to create an instance of the converter like so in your resources:
    //< UserControl.Resources >
    //    < cvt:VisibilityConverter x:Key = "VisibilityConverter" />
    // </ UserControl.Resources >

    // Then you can bind your border like so:
    //< Border x: Name = "Border1" Visibility = "{Binding vis, Converter={StaticResource VisibilityConverter}}>
    //  < Grid >
    //     < Label Content = "test" />
    //  </ Grid >
    //</ Border >
    #endregion

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class VisibilityConverter : IValueConverter
    {
        public const string Invert = "Invert";

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a Visibility.");

            bool? bValue = (bool?)value;

            if (parameter != null && parameter as string == Invert)
                bValue = !bValue;

            return bValue.HasValue && bValue.Value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}