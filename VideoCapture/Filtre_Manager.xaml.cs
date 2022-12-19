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
    public partial class Filtre_Manager : Window, INotifyPropertyChanged
    {
        MainWindow mainWindow;
        bool forceClosing;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void _ListFiltersUpdate()
        {
            OnPropertyChanged("_ListFilters");
        }

        public Filtre currentFilter
        {
            get
            {
                return _currentFilter;
            }
            set
            {
                if (_currentFilter == value) return;

                _currentFilter = value;
                if (_currentFilter != null)
                {
                    if (_currentFilter.isTxt)
                    {
                        Filtre_TXT ft = (Filtre_TXT)_currentFilter;
                        colorPicker._SetColor(ft.color);
                        colorPicker_Border._SetColor(ft.color_Border);

                        gridTXT = Visibility.Visible;
                        gridIMG = Visibility.Collapsed;
                    }

                    if (_currentFilter.isImage)
                    {
                        gridTXT = Visibility.Collapsed;
                        gridIMG = Visibility.Visible;
                    }
                }
                OnPropertyChanged("gridTXT");
                OnPropertyChanged("gridIMG");
                OnPropertyChanged("currentFilter");
                OnPropertyChanged("oneFiltreIsSelected");
            }
        }
        Filtre _currentFilter = null;

        public Visibility gridTXT
        {
            get
            {
                return _gridTXT;
            }
            set
            {
                _gridTXT = value;
                OnPropertyChanged("gridTXT");
            }
        }
        Visibility _gridTXT = Visibility.Collapsed;

        public Visibility gridIMG
        {
            get
            {
                return _gridIMG;
            }
            set
            {
                _gridIMG = value;
                OnPropertyChanged("gridIMG");
            }
        }
        Visibility _gridIMG = Visibility.Collapsed;

        public bool oneFiltreIsSelected
        {
            get { return currentFilter != null; }
        }

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
            colorPicker_Border._ColorNew += ColorPicker_Border__ColorNew;

            OnPropertyChanged("gridTXT");
        }

        internal void _Link(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            _ListFilters.CollectionChanged += _ListFilters_CollectionChanged;
        }

        public void ReallyClose()
        {
            forceClosing = true;
            Close();
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!forceClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        void _ListFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (Filtre item in e.NewItems)
                    item.PropertyChanged += FilterPropertyChanged;

            if (e.OldItems != null)
                foreach (Filtre item in e.OldItems)
                    item.PropertyChanged -= FilterPropertyChanged;
        }

        public void FilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            mainWindow.Filter_Update();
        }

        void ColorPicker__ColorNew(object sender, Standard_UC_JJO.ColorPickerJJO.NewColorEventArgs e)
        {
            if (currentFilter == null) return;
            if (currentFilter._type == Filtre.FiltreType.texte)
            {
                Filtre_TXT ft = (Filtre_TXT)currentFilter;
                ft.color = colorPicker._Color;
            }
        }

        void ColorPicker_Border__ColorNew(object sender, Standard_UC_JJO.ColorPickerJJO.NewColorEventArgs e)
        {
            if (currentFilter == null) return;
            if (currentFilter._type == Filtre.FiltreType.texte)
            {
                Filtre_TXT ft = (Filtre_TXT)currentFilter;
                ft.color_Border = colorPicker_Border._Color;
            }
        }

        void btn_add_image_click(object sender, RoutedEventArgs e)
        {
            Filtre_IMAGE f = new Filtre_IMAGE();
            _ListFilters.Add(f);
            currentFilter = f;
            f.PropertyChanged += FilterPropertyChanged;
            mainWindow.Filter_Update();
        }

        void btn_add_text_click(object sender, RoutedEventArgs e)
        {
            Enum.TryParse(((MenuItem)sender).Header.ToString(), out Filtre_TXT.Filtre_TXT_Type ftt);

            Filtre_TXT f = new Filtre_TXT();
            f.filtre_TXT_Type = ftt;

            _ListFilters.Add(f);
            currentFilter = f;
            f.PropertyChanged += FilterPropertyChanged;

            mainWindow.Filter_Update();
        }

        void btn_filtre_moins_Click(object sender, RoutedEventArgs e)
        {
            if (currentFilter != null)
            {
                int index = _ListFilters.IndexOf(currentFilter);
                currentFilter.PropertyChanged -= FilterPropertyChanged;
                _ListFilters.Remove(currentFilter);
                mainWindow.Filter_Update();
                if (_ListFilters.Count > 0)
                    currentFilter = (_ListFilters.Count - 1 >= index) ? _ListFilters[index] : _ListFilters[index - 1];
            }
        }

        void btn_filtre_duplicate_Click(object sender, RoutedEventArgs e)
        {
            if (currentFilter == null) return;

            Filtre f = (Filtre)currentFilter.Clone();
            _ListFilters.Add(f);
            currentFilter = f;
            f.PropertyChanged += FilterPropertyChanged;
            mainWindow.Filter_Update();
        }

        void btn_filtre_moveup_Click(object sender, RoutedEventArgs e)
        {
            if (currentFilter == null) return;
            int index = _ListFilters.IndexOf(currentFilter);
            if (index < 1) return;
            _ListFilters.Move(index, index - 1);
            mainWindow.Filter_Update();
        }

        void btn_filtre_movedown_Click(object sender, RoutedEventArgs e)
        {
            if (currentFilter == null) return;
            int index = _ListFilters.IndexOf(currentFilter);
            if (index >= _ListFilters.Count - 1) return;
            _ListFilters.Move(index, index + 1);
            mainWindow.Filter_Update();
        }

        void SetFilterPosition_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.SetFilterPosition(currentFilter);
        }

        void btn_filtre_save_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Config_Filters_Save();
        }

        void btn_filtre_load_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Config_Filters_Load();
            OnPropertyChanged("_ListFilters");
        }

        void SelectFilterOnDisk_Click(object sender, MouseButtonEventArgs e)
        {
            if (currentFilter == null) return;
            if (currentFilter._type != Filtre.FiltreType.image) return;
            Filtre_IMAGE fi = (Filtre_IMAGE)currentFilter;

            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    fi.FileName = dialog.FileName;
                }
            }
        }
    }
}
