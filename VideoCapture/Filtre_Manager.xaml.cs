using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Collections.ObjectModel;

namespace VideoCapture
{
    /// <summary>
    /// Logique d'interaction pour Filtre_Manager.xaml
    /// </summary>
    public partial class Filtre_Manager : Window, INotifyPropertyChanged
    {
        MainWindow mainWindow;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Filtre currentFilter
        {
            get { return _currentFilter; }
            set
            {
                _currentFilter = value;
                /// _colorPickerJJO._ColorNew += ColorNew;
                /// _colorPickerJJO._SetMouseSelection(0.5, 0.5);
                /// _colorPickerJJO._SetColor(Colors.Magenta);
                OnPropertyChanged("currentFilter");
            }
        }
        Filtre _currentFilter;

        public ObservableCollection<Filtre> _ListFilters
        {
            get { return mainWindow.filtres; }
            set
            {
                mainWindow.filtres = value;
                OnPropertyChanged("_ListFilters");
            }
        }

        public Filtre_Manager()
        {
            InitializeComponent();
            DataContext = this;
            colorPicker._ColorNew += ColorPicker__ColorNew;

            //cbx_font.ItemsSource = Enum.GetValues(typeof(OpenCvSharp.HersheyFonts)).Cast<OpenCvSharp.HersheyFonts>();
        }

        private void _ListFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (Filtre item in e.NewItems)
                    item.PropertyChanged += MyType_PropertyChanged;

            if (e.OldItems != null)
                foreach (Filtre item in e.OldItems)
                    item.PropertyChanged -= MyType_PropertyChanged;
        }

        void MyType_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "MyProperty")
            mainWindow.Filter_Update();
        }

        private void ColorPicker__ColorNew(object sender, Standard_UC_JJO.ColorPickerJJO.NewColorEventArgs e)
        {
            if (currentFilter == null) return;
            if (currentFilter._type == Filtre.FiltreType.texte)
            {
                Filtre_TXT ft = (Filtre_TXT)currentFilter;
                ft.color = colorPicker._Color;
            }
        }

        internal void _Link(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            _ListFilters.CollectionChanged += _ListFilters_CollectionChanged;
        }

        void btn_add_image_click(object sender, RoutedEventArgs e)
        {

        }

        void btn_add_text_click(object sender, RoutedEventArgs e)
        {
            Filtre_TXT f = new Filtre_TXT("Your text here");
            _ListFilters.Add(f);
            currentFilter = f;

            mainWindow.Filter_Update();
        }

        private void btn_filtre_moins_Click(object sender, MouseButtonEventArgs e)
        {
            if (currentFilter != null)
            {
                int index = _ListFilters.IndexOf(currentFilter);
                _ListFilters.Remove(currentFilter);
                mainWindow.Filter_Update();
                if (_ListFilters.Count > 0)
                    currentFilter = (_ListFilters.Count - 1 >= index) ? _ListFilters[index] : _ListFilters[index - 1];
            }
        }

        //private void cbx_font_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (currentFilter == null) return;
        //    if (currentFilter._type == Filtre.FiltreType.texte)
        //    {
        //        Filtre_TXT ft = (Filtre_TXT)currentFilter;

        //        ft.font = (OpenCvSharp.HersheyFonts)cbx_font.SelectedItem;
        //    }
        //}




    }
}
