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
        string version = "2022/04/28";

        #region VARIABLES & PARAMETERS
        // FPS
        long T0;
        System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();

        // Image capturée
        Mat frame;
        double frame_ratio = 3;
        double actualWidth;

        bool flip_h;
        bool flip_v;
        RotateFlags? rotation;

        // Filtres
        Dictionary<string, MenuItem> filtres;
        string dossierFiltres = AppDomain.CurrentDomain.BaseDirectory + "filters";

        // CAMERA
        Thread thread;
        int indexDevice;
        VideoInInfo.Format format;
        private bool newFormat;
        private string deviceName;
        private string formatName;
        Dictionary<string, VideoInInfo.Format> formats;
        OpenCvSharp.VideoCapture capture;
        bool isRunning = false;

        System.Windows.Threading.DispatcherTimer mouseEnterEventDelayTimer = new System.Windows.Threading.DispatcherTimer();
        double MouseEnter_Delay_sec = 3;
        long framegrabbed = 0;
        long framereallygrabbed = 0;
        #endregion

        #region VARIABLES BINDINGS
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
        bool ShowCPUMem = false;

        public bool _HideWindowBar
        {
            get { return HideWindowBar; }
            set
            {
                HideWindowBar = value;
                ctxm_hideothers.IsChecked = _HideWindowBar;
                OnPropertyChanged("_HideWindowBar");
                WindowBarManagement();
            }
        }
        bool HideWindowBar;


        public bool _fullScreen
        {
            get { return fullScreen; }
            set
            {
                fullScreen = value;
                FullScreenManagement();
                OnPropertyChanged("_fullScreen");
            }
        }
        bool fullScreen;

        public string _version
        {
            get { return version; }
        }

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

        #region WINDOW MANAGEMENT
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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
                this.Width = sizeInfo.NewSize.Height * frame_ratio;
            else
                this.Height = sizeInfo.NewSize.Width / frame_ratio;

            actualWidth = this.Width;
        }

        void img_mousedown(object sender, MouseButtonEventArgs e)
        {
            if (ctxm_hideothers.IsChecked)
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
        }

        void MouseEnterEventDelay_Init()
        {
            mouseEnterEventDelayTimer.Interval = TimeSpan.FromSeconds(MouseEnter_Delay_sec);
            mouseEnterEventDelayTimer.Tick += Window_MouseEnter_Tick;
        }

        void Window_MouseEnter_Tick(object sender, EventArgs e)
        {
            grd_visu_Collapse();
        }

        void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            grd_visu_Collapse();
        }

        void Window_MouseMove(object sender, MouseEventArgs e)
        {
            grd_visu.Visibility = Visibility.Visible;
            mouseEnterEventDelayTimer.Start();
        }

        void grd_visu_Collapse()
        {
            if (cbx_device.IsDropDownOpen || cbx_deviceFormat.IsDropDownOpen)
            {
                mouseEnterEventDelayTimer.Start();
            }
            else
            {
                grd_visu.Visibility = Visibility.Collapsed;
                mouseEnterEventDelayTimer.Stop();
            }
        }

        void FullScreenManagement()
        {
            if (fullScreen)
            {
                //icon
                ctxm_fullscreen_max.Visibility = Visibility.Collapsed;
                ctxm_fullscreen_min.Visibility = Visibility.Visible;
                ctxm_fullscreen_txt.Content = "Window mode";
                //fenetre
                WindowState = WindowState.Maximized;
            }
            else
            {
                //icon
                ctxm_fullscreen_max.Visibility = Visibility.Visible;
                ctxm_fullscreen_min.Visibility = Visibility.Collapsed;
                ctxm_fullscreen_txt.Content = "Fullscreen";
                //fenetre
                WindowState = WindowState.Normal;
            }
        }
        #endregion

        #region WINDOW MANAGEMENT - Contextual Menu
        private void ctxm_alwaysontop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = ctxm_alwaysontop.IsChecked;
        }
        private void ctxm_fullscreen_Switch_Click(object sender, RoutedEventArgs e)
        {
            _fullScreen = !_fullScreen;
        }

        private void ctxm_hideothers_Click(object sender, RoutedEventArgs e)
        {
            _HideWindowBar = !_HideWindowBar;
            ctxm_hideothers.IsChecked = _HideWindowBar;
        }

        void WindowBarManagement()
        {
            if (_HideWindowBar)
            {
                grd_visu.Visibility = Visibility.Collapsed;
                WindowStyle = WindowStyle.None;
            }
            else
            {
                grd_visu.Visibility = Visibility.Visible;
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        private void ctxm_quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        void INITS()
        {
            ListDevices();
            UpdateFilers();
            ManageFilter("");
            FullScreenManagement();
            MouseEnterEventDelay_Init();
            _HideWindowBar = true;
        }

        #region DEVICES MANAGEMENT
        private void AllDevices_Click(object sender, MouseButtonEventArgs e)
        {
            ListDevices();
        }

        void ListDevices()
        {
            var devices = VideoInInfo.EnumerateVideoDevices_JJ();
            if (cbx_device != null)
                cbx_device.ItemsSource = devices.Select(d => d.Name).ToList();
        }

        void Play()
        {
            chrono.Start();
            isRunning = true;
            indexDevice = cbx_device.SelectedIndex;
            CaptureCamera(indexDevice);
        }

        private void Combobox_CaptureDevice_Change(object sender, SelectionChangedEventArgs e)
        {
            indexDevice = cbx_device.SelectedIndex;
            try
            {
                formats = VideoInInfo.EnumerateSupportedFormats_JJ(indexDevice);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            cbx_deviceFormat.ItemsSource = formats.OrderBy(f => f.Value.format).ThenByDescending(f => f.Value.w).Select(f => f.Key);

            deviceName = cbx_device.Items[cbx_device.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            //set default format
            cbx_deviceFormat.SelectedIndex = 0;
        }

        private void Combobox_CaptureDeviceFormat_Change(object sender, SelectionChangedEventArgs e)
        {
            format = formats[cbx_deviceFormat.SelectedValue as string];
            newFormat = true;
            formatName = cbx_deviceFormat.Items[cbx_deviceFormat.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            if (!isRunning)
                Play();
        }
        #endregion

        #region CAPTURE MANAGEMENT
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
            if (isRunning)
            {
                isRunning = false;
                Thread.Sleep(100);
                thread?.Abort();
            }
        }

        void CaptureCameraCallback()
        {
            int actualindexDevice = indexDevice;
            frame = new Mat();
            capture = new OpenCvSharp.VideoCapture(indexDevice);

            capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);

            int w = format.w;
            int h = format.h;
            float fps = format.fr;
            string current_fourcc = format.format;

            capture.Set(VideoCaptureProperties.FrameWidth, w);
            capture.Set(VideoCaptureProperties.FrameHeight, h);
            capture.Set(VideoCaptureProperties.Fps, fps);
            capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.FourCC.FromString(current_fourcc));

            newFormat = false;

            if (capture.IsOpened())
            {
                while (isRunning)
                {
                    if (indexDevice != actualindexDevice)
                    {
                        capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
                        actualindexDevice = indexDevice;
                    }

                    if (newFormat)
                    {
                        w = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                        h = (int)capture.Get(VideoCaptureProperties.FrameHeight);
                        fps = (float)capture.Get(VideoCaptureProperties.Fps);

                        int intfourcc = (int)capture.Get(VideoCaptureProperties.FourCC);

                        byte[] bytesfourcc = BitConverter.GetBytes(intfourcc);
                        char c1 = Convert.ToChar(bytesfourcc[0]);
                        char c2 = Convert.ToChar(bytesfourcc[1]);
                        char c3 = Convert.ToChar(bytesfourcc[2]);
                        char c4 = Convert.ToChar(bytesfourcc[3]);
                        current_fourcc = new string(new char[] { c1, c2, c3, c4 });


                        capture.Set(VideoCaptureProperties.FrameWidth, format.w);
                        capture.Set(VideoCaptureProperties.FrameHeight, format.h);
                        capture.Set(VideoCaptureProperties.Fps, format.fr);

                        int remainingtest = 5;
                        while (current_fourcc != format.format && remainingtest > 0)
                        {
                            remainingtest--;

                            capture.Dispose();
                            capture = new OpenCvSharp.VideoCapture(indexDevice);
                            capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
                            capture.Set(VideoCaptureProperties.FrameWidth, format.w);
                            capture.Set(VideoCaptureProperties.FrameHeight, format.h);
                            capture.Set(VideoCaptureProperties.Fps, format.fr);
                            capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.FourCC.FromString(format.format));

                            intfourcc = (int)capture.Get(VideoCaptureProperties.FourCC);
                            bytesfourcc = BitConverter.GetBytes(intfourcc);
                            c1 = Convert.ToChar(bytesfourcc[0]);
                            c2 = Convert.ToChar(bytesfourcc[1]);
                            c3 = Convert.ToChar(bytesfourcc[2]);
                            c4 = Convert.ToChar(bytesfourcc[3]);
                            current_fourcc = new string(new char[] { c1, c2, c3, c4 });
                        }

                        if (remainingtest <= 0)
                        {
                            MessageBox.Show("Fail to switch to " + format.format + " format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        frame_ratio = (double)format.w / format.h;
                        actualWidth = format.w;
                        Application.Current.Dispatcher.Invoke(() => { Width = actualWidth; });
                        newFormat = false;
                    }

                    capture.Read(frame);
                    framegrabbed++;

                    if (!frame.Empty())
                    {
                        framereallygrabbed++;

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
            capture.Dispose();
        }
        #endregion

        #region IMAGE MANAGEMENT
        void Show(Mat frame)
        {
            if (!frame.Empty())
            {
                if (actualWidth < frame.Width)
                    Cv2.Resize(frame, frame, new OpenCvSharp.Size(actualWidth, actualWidth / frame.Width * frame.Height), interpolation: InterpolationFlags.Cubic);

                _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);

                _Infos = "(" + capture.FourCC + ") " + capture.FrameWidth + "*" + capture.FrameHeight + " [" + (int)capture.Fps + "fps] " + frame.Width + "*" + frame.Height;
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

        void ctxm_rotate90_Click(object sender, RoutedEventArgs e)
        {
            if (rotation == RotateFlags.Rotate90Clockwise)
                rotation = null;
            else
                rotation = RotateFlags.Rotate90Clockwise;
            MenuItemRotationCheck();
        }

        void ctxm_rotate270_Click(object sender, RoutedEventArgs e)
        {
            if (rotation == RotateFlags.Rotate90Counterclockwise)
                rotation = null;
            else
                rotation = RotateFlags.Rotate90Counterclockwise;
            MenuItemRotationCheck();
        }

        void ctxm_rotate180_Click(object sender, RoutedEventArgs e)
        {
            if (rotation == RotateFlags.Rotate180)
                rotation = null;
            else
                rotation = RotateFlags.Rotate180;
            MenuItemRotationCheck();
        }

        void MenuItemRotationCheck()
        {
            ctxm_rotate90.IsChecked = false;
            ctxm_rotate180.IsChecked = false;
            ctxm_rotate270.IsChecked = false;
            switch (rotation)
            {
                case RotateFlags.Rotate90Clockwise:
                    ctxm_rotate90.IsChecked = true;
                    frame_ratio = (double)format.h / format.w;
                    break;
                case RotateFlags.Rotate90Counterclockwise:
                    ctxm_rotate270.IsChecked = true;
                    frame_ratio = (double)format.h / format.w;
                    break;
                case RotateFlags.Rotate180:
                    ctxm_rotate180.IsChecked = true;
                    frame_ratio = (double)format.w / format.h;
                    break;
                case null:
                    frame_ratio = (double)format.w / format.h;
                    break;
            }
            //force resize
            Width++;
        }

        void ctxm_flip_h_Click(object sender, RoutedEventArgs e)
        {
            flip_h = !flip_h;
            ((MenuItem)sender).IsChecked = flip_h;
        }

        void ctxm_flip_v_Click(object sender, RoutedEventArgs e)
        {
            flip_v = !flip_v;
            ((MenuItem)sender).IsChecked = flip_v;
        }
        #endregion

        #region FILTERS \ Calque par dessus image
        void UpdateFilers()
        {
            filtres = new Dictionary<string, MenuItem>();
            ctxm_calque.Items.Clear();

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

        void Mi_addfilter_Click(object sender, RoutedEventArgs e)
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

        void Mi_none_Click(object sender, RoutedEventArgs e)
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

    }
}