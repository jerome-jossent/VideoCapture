using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.IO;

namespace VideoCapture
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        #region Bindings
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string _title
        {
            get { return deviceName + " " + formatName + fps; }
            set
            {
                title = value;
                OnPropertyChanged("_title");
            }
        }
        string title;

        public string _fps
        {
            get { return fps; }
            set
            {
                fps = value;
                OnPropertyChanged("_fps");
                OnPropertyChanged("_title");
            }
        }
        string fps;

        public string _Infos
        {
            get { return Infos; }
            set
            {
                Infos = value;
                OnPropertyChanged("_Infos");
            }
        }
        string Infos;


        public bool _ShowCPUMem
        {
            get { return ShowCPUMem; }
            set
            {
                ShowCPUMem = value;
                OnPropertyChanged("_ShowCPUMem");
            }
        }
        bool ShowCPUMem = true;

        public System.Drawing.Bitmap _imageSource
        {
            get
            {
                if (imageSource == null)
                    return null;
                return imageSource;
            }
            set
            {
                if (imageSource != value)
                {
                    imageSource = value;
                    OnPropertyChanged("_imageSource");
                }
            }
        }
        System.Drawing.Bitmap imageSource;

        public System.Drawing.Bitmap _imageCalque
        {
            get
            {
                if (imageCalque == null)
                    return null;
                return imageCalque;
            }
            set
            {
                if (imageCalque != value)
                {
                    imageCalque = value;
                    OnPropertyChanged("_imageCalque");
                }
            }
        }
        System.Drawing.Bitmap imageCalque;
        #endregion

        #region CPU/Mem
        System.Diagnostics.PerformanceCounter cpuCounterAll;
        System.Diagnostics.PerformanceCounter cpuCounterIHM;
        //PerformanceCounter ramCounter;

        void InfoProcess_INIT()
        {
            cpuCounterAll = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounterIHM = new System.Diagnostics.PerformanceCounter("Process", "% Processor Time", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            //ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            System.Windows.Threading.DispatcherTimer cpu_memory_usage = new System.Windows.Threading.DispatcherTimer();
            cpu_memory_usage.Interval = TimeSpan.FromMilliseconds(cpuUseTime_period); //laisser 1 sec pour calcul CPU
            cpu_memory_usage.Tick += Cpu_memory_usage_Tick;
            cpu_memory_usage.Start();
        }

        private void Cpu_memory_usage_Tick(object sender, EventArgs e)
        {
            // Getting first initial values
            cpuCounterIHM.NextValue();
            cpuCounterAll.NextValue();

            // Creating delay to get correct values of CPU usage during next query
            Thread.Sleep(50);
            OnPropertyChanged("CurrentCpuUsage");
            OnPropertyChanged("AvailableRAM");
        }
        TimeSpan _prevCPUUseTime;
        int cpuUseTime_period = 1000;
        public string CurrentCpuUsage
        {
            get
            {
                TimeSpan endCpuUsage = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                double cpuUsedMs = (endCpuUsage - _prevCPUUseTime).TotalMilliseconds;
                _prevCPUUseTime = endCpuUsage;
                double cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * cpuUseTime_period);
                return "CPU : " + (int)(cpuUsageTotal * 100) + "%";
            }
        }
        public string AvailableRAM
        {
            get
            {
                return "Memory : " + (int)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1048576 + "MB"; //1024*1024
            }
        }
        #endregion

        long T0;
        System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();

        Mat frame;
        double actualWidth;

        Dictionary<string, MenuItem> filtres;
        System.Windows.Controls.Image image_preview_filter;
        string dossierFiltres = AppDomain.CurrentDomain.BaseDirectory + "filters";
        #region CAMERA
        Thread thread;
        int indexDevice;
        VideoInInfo.Format format;
        private string deviceName;
        private string formatName;
        Dictionary<string, VideoInInfo.Format> formats;
        OpenCvSharp.VideoCapture capture;
        bool isRunning = false;
        #endregion

        #region Gestion Window
        public MainWindow()
        {
            InitializeComponent();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
            INITS();
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isRunning = false;
            CaptureCameraStop();
        }

        void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            actualWidth = this.ActualWidth;
        }

        void img_mousedown(object sender, MouseButtonEventArgs e)
        {
            if (ctxm_hideothers.IsChecked)
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
        }
        #endregion

        void INITS()
        {
            InfoProcess_INIT();
            ListDevices();
            actualWidth = 320;
            UpdateFilers();
            ManageFilter("");
        }

        #region CAMERA MANAGEMENT
        void CaptureCamera(int index)
        {
            if (thread != null && thread.IsAlive)
            {
                thread.Abort();
                Thread.Sleep(100);
            }
            indexDevice = index;
            thread = new Thread(new ThreadStart(CaptureCameraCallback));
            thread.Start();
        }

        void CaptureCameraStop()
        {
            thread?.Abort();
        }

        void CaptureCameraCallback()
        {
            int actualindexDevice = indexDevice;
            frame = new Mat();
            capture = new OpenCvSharp.VideoCapture(indexDevice);
            capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);

            if (capture.IsOpened())
            {
                while (isRunning)
                {
                    if (indexDevice != actualindexDevice)
                    {
                        capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
                        actualindexDevice = indexDevice;
                    }

                    if (format != null)
                    {
                        capture.Set(VideoCaptureProperties.FrameWidth, format.w);
                        capture.Set(VideoCaptureProperties.FrameHeight, format.h);
                        capture.Set(VideoCaptureProperties.Fps, format.fr);
                        capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.FourCC.FromString(format.format));
                        format = null;
                    }

                    capture.Read(frame);
                    if (flip_h && flip_v)
                    {
                        frame = frame.Flip(FlipMode.XY);
                    }
                    else
                    {
                        if (flip_h)
                            frame = frame.Flip(FlipMode.Y);
                        if (flip_v)
                            frame = frame.Flip(FlipMode.X);
                    }

                    if (rotation != null)
                        Cv2.Rotate(frame, frame, (RotateFlags)rotation);

                    Show(frame);
                }
            }
        }
        #endregion

        void Show(Mat frame)
        {
            if (!frame.Empty())
            {
                if (actualWidth < frame.Width)
                    Cv2.Resize(frame, frame, new OpenCvSharp.Size(actualWidth, actualWidth / frame.Width * frame.Height), interpolation: InterpolationFlags.Cubic);

                _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);

                _Infos = frame.Size().ToString();
            }

            FPS();
        }

        void FPS()
        {
            long T = chrono.ElapsedMilliseconds;
            float f = 1000f / (T - T0);
            T0 = T;
            _fps = "[" + f.ToString("N1") + " fps]";
        }

        #region Capture Device
        private void AllDevices_Click(object sender, MouseButtonEventArgs e)
        {
            ListDevices();
        }

        private void ListDevices()
        {
            var devices = VideoInInfo.EnumerateVideoDevices_JJ();
            if (cbx_device != null)
                cbx_device.ItemsSource = devices.Select(d => d.Name).ToList();
        }

        void Play()
        {
            chrono.Start();
            isRunning = !isRunning;

            if (isRunning)
            {
                indexDevice = cbx_device.SelectedIndex;
                CaptureCamera(indexDevice);
            }
        }

        private void Combobox_CaptureDevice_Change(object sender, SelectionChangedEventArgs e)
        {
            indexDevice = cbx_device.SelectedIndex;
            formats = VideoInInfo.EnumerateSupportedFormats_JJ(indexDevice);
            cbx_deviceFormat.ItemsSource = formats.OrderBy(f => f.Value.format).ThenByDescending(f => f.Value.w).Select(f => f.Key);

            deviceName = cbx_device.Items[cbx_device.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            //set default format
            cbx_deviceFormat.SelectedIndex = 0;
        }

        private void Combobox_CaptureDeviceFormat_Change(object sender, SelectionChangedEventArgs e)
        {
            format = formats[cbx_deviceFormat.SelectedValue as string];

            formatName = cbx_deviceFormat.Items[cbx_deviceFormat.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            if (!isRunning)
                Play();
        }

        private void ctxm_alwaysontop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = ctxm_alwaysontop.IsChecked;
        }

        private void ctxm_hideothers_Click(object sender, RoutedEventArgs e)
        {
            if (ctxm_hideothers.IsChecked)
            {
                grd_visu.Height = new GridLength(0);
                WindowStyle = WindowStyle.None;
            }
            else
            {
                grd_visu.Height = new GridLength(1, GridUnitType.Auto);
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        private void ctxm_quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        #region Filters

        void UpdateFilers()
        {
            filtres = new Dictionary<string, MenuItem>();
            ctxm_calque.Items.Clear();

            //image_preview_filter = new System.Windows.Controls.Image();
            //image_preview_filter.Width = 100;
            //image_preview_filter.Height = 100;
            //System.Windows.Media.RenderOptions.SetBitmapScalingMode(image_preview_filter, System.Windows.Media.BitmapScalingMode.Fant);
            //ctxm_calque.Items.Add(image_preview_filter);

            // "Pick Filter"
            StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
            System.Windows.Controls.Image im = ImageFileToImageWPF(AppDomain.CurrentDomain.BaseDirectory + "Resources//5129-tOo-Dossierouvert.png");
            im.Width = 20;
            im.Height = 20;
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(im, System.Windows.Media.BitmapScalingMode.Fant);
            sp.Children.Add(im);
            ContentPresenter cp = new ContentPresenter();
            cp.Margin = new Thickness(10, 0, 0, 0);
            cp.Content = "Pick a filter";
            sp.Children.Add(cp);
            MenuItem mi_add = new MenuItem();
            mi_add.Header = sp;
            mi_add.Click += Mi_addfilter_Click;
            ctxm_calque.Items.Add(mi_add);

            // separateur
            ctxm_calque.Items.Add(new Separator());

            // "None"
            MenuItem mi_none = new MenuItem() { Header = "None" };
            mi_none.Click += Mi_none_Click;
            ctxm_calque.Items.Add(mi_none);
            mi_none.IsChecked = false;
            filtres.Add("", mi_none);

            // "Filters"
            DirectoryInfo di = new DirectoryInfo(dossierFiltres);

            Style stackPanelStyle = this.FindResource("HorizontalStackPanel") as Style;

            foreach (FileInfo fi in di.GetFiles())
            {
                sp = new StackPanel();
                sp.Style = stackPanelStyle;
                sp.Orientation = Orientation.Horizontal;
                im = ImageFileToImageWPF(fi.FullName);
                im.Width = 100;
                im.Height = 100;
                sp.Children.Add(im);
                cp = new ContentPresenter();
                //cp.Margin = new Thickness(10, 0, 0, 0);
                cp.Content = fi.Name;
                sp.Children.Add(cp);
                MenuItem mi = new MenuItem();
                mi.Header = sp;
                mi.IsChecked = false;
                mi.Click += Mi_filter_Click;
                ctxm_calque.Items.Add(mi);
                filtres.Add(fi.Name, mi);
            }
        }

        System.Windows.Controls.Image ImageFileToImageWPF(string fullfilename)
        {
            return ImageFileToImageWPF(new Uri(fullfilename));
        }

        System.Windows.Controls.Image ImageFileToImageWPF(Uri uri)
        {
            System.Windows.Controls.Image myImage3 = new System.Windows.Controls.Image();
            BitmapImage bi3 = new BitmapImage();
            bi3.BeginInit();
            bi3.UriSource = uri;
            bi3.EndInit();
            myImage3.Stretch = System.Windows.Media.Stretch.Fill;
            myImage3.Source = bi3;
            return myImage3;
        }

        private void Mi_addfilter_Click(object sender, RoutedEventArgs e)
        {
            //selectionne fichier image
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string nom_fichier = openFileDialog.FileName;
                FileInfo fi = new FileInfo(nom_fichier);
                //copie fichier
                fi = fi.CopyTo(AppDomain.CurrentDomain.BaseDirectory + "filters\\" + fi.Name, overwrite: true);
                UpdateFilers();
                ManageFilter(fi.Name);
            }
        }

        private void Mi_none_Click(object sender, RoutedEventArgs e)
        {
            ManageFilter("");
        }

        void Mi_filter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            StackPanel sp = (StackPanel)mi.Header;
            string filtername = "";
            foreach (var item in sp.Children)
            {
                string child_typ = item.GetType().ToString();
                if (child_typ == "System.Windows.Controls.ContentPresenter")
                {
                    filtername = (string)((ContentPresenter)item).Content;
                    break;
                }
            }
            ManageFilter(filtername);
        }

        void ManageFilter(string filtername)
        {
            if (filtername == "")
                imagecalque.Source = null;
            else
                imagecalque.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "filters\\" + filtername));

            foreach (var item in filtres)
                item.Value.IsChecked = (item.Key == filtername);
        }
        #endregion

        bool flip_h;
        bool flip_v;
        RotateFlags? rotation;

        private void ctxm_rotate90_Click(object sender, RoutedEventArgs e)
        {
            if (rotation == null)
                rotation = RotateFlags.Rotate90Clockwise;
            else
                rotation = null;
            MenuItemRotationCheck();
        }

        private void ctxm_rotate270_Click(object sender, RoutedEventArgs e)
        {
            if (rotation == null)
                rotation = RotateFlags.Rotate90Counterclockwise;
            else
                rotation = null;
            MenuItemRotationCheck();
        }

        private void ctxm_rotate180_Click(object sender, RoutedEventArgs e)
        {
            if (rotation == null)
                rotation = RotateFlags.Rotate180;
            else
                rotation = null;
            MenuItemRotationCheck();
        }

        private void MenuItemRotationCheck()
        {

        }

        private void ctxm_flip_h_Click(object sender, RoutedEventArgs e)
        {
            flip_h = !flip_h;
            ((MenuItem)sender).IsChecked = flip_h;
        }

        private void ctxm_flip_v_Click(object sender, RoutedEventArgs e)
        {
            flip_v = !flip_v;
            ((MenuItem)sender).IsChecked = flip_v;
        }
    }
}