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

namespace VideoCapture
{
    /// <summary>
    /// Logique d'interaction pour Filtre_Manager.xaml
    /// </summary>
    public partial class Filtre_Manager : Window
    {
        MainWindow mainWindow;

        public Filtre_Manager()
        {
            InitializeComponent();
        }

        internal void _Link(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
    }
}
