﻿using OpenCvSharp;
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
using System.Collections.ObjectModel;
using Newtonsoft.Json;

// Disable Dpi awareness in the application assembly.
//[assembly: System.Windows.Media.DisableDpiAwareness]

namespace VideoCapture
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        bool AUTORELOAD = true;
        bool configLoading = false;
        CameraConfiguration cameraConfiguration = null;

        const string version = "version 2022/12/05";

        string fichier_config = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.json";
        string fichier_filtres = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\filtres.json";


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
        public ObservableCollection<Filtre> filtres
        {
            get { return __filtres; }
            set
            {
                __filtres = value;
                OnPropertyChanged("filtres");
            }
        }
        ObservableCollection<Filtre> __filtres = new ObservableCollection<Filtre>();
        bool filtres_aumoins1dynamic;

        Dictionary<string, MenuItem> _filtres;
        string dossierFiltres = AppDomain.CurrentDomain.BaseDirectory + "Filters";
        string filtername;

        Mat filterframe;
        Mat frame_augmentation;

        //region of interest (crop)
        OpenCvSharp.Rect roi = new OpenCvSharp.Rect();
        bool roi_enabled = false;

        // CAMERA
        Thread threadCapture;
        Thread threadFiltre;
        int indexDevice;
        VideoInInfo.Format format;
        private bool newFormat;
        private string deviceName;
        private string formatName;
        Dictionary<string, VideoInInfo.Format> formats;
        OpenCvSharp.VideoCapture capture;
        bool isRunning = false;
        bool isRunningFiltre = false;

        System.Windows.Threading.DispatcherTimer mouseEnterEventDelayTimer = new System.Windows.Threading.DispatcherTimer();
        double MouseEnter_Delay_sec = 3;
        long framegrabbed = 0;
        long framereallygrabbed = 0;
        #endregion

        DirectShowLib.DsDevice[] devices;
        DirectShowLib.DsDevice current_device;

        bool forceResize;
        double WindowsScreenScale;
        Filtre_Manager filtre_manager;
        Filtre currentFilter;

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);


        #region VARIABLES BINDINGS
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string _title
        {
            get { return "VideoCapture " + version + " - " + deviceName + " - " + formatName + " " + fps; }
        }

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

        float _FPS;

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
                OnPropertyChanged("_HideWindowBar");
                WindowBarManagement();

                Properties.Settings.Default.HideWindowBar = _HideWindowBar;
                Properties.Settings.Default.Save();
            }
        }
        bool HideWindowBar = Properties.Settings.Default.HideWindowBar;

        public bool _HideMenu
        {
            get { return HideMenu; }
            set
            {
                HideMenu = value;
                OnPropertyChanged("_HideMenu");
                if (filterPositionning) return;

                Properties.Settings.Default.HideMenu = _HideMenu;
                Properties.Settings.Default.Save();
            }
        }
        bool HideMenu = Properties.Settings.Default.HideMenu;


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

        public string ScreenshotFolder
        {
            get
            {
                return Properties.Settings.Default.ScreenshotFolder;
            }
            set
            {
                string rep = value;
                if (rep.Substring(rep.Length - 2) != "\\")
                    rep += "\\";

                Properties.Settings.Default.ScreenshotFolder = rep;
                Properties.Settings.Default.Save();
                OnPropertyChanged("ScreenshotFolder");
            }
        }

        string screenshotFile_Last = "";

        public string ScreenshotCount
        {
            get
            {
                return screenshotCount.ToString();
            }
        }

        public int screenshotCount
        {
            get
            {
                return screenshot_Count;
            }
            set
            {
                screenshot_Count = value;
                OnPropertyChanged("ScreenshotCount");
            }
        }
        int screenshot_Count = 0;

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
            QUIT();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (WindowsScreenScale == 0) return;
            if (format == null) return;



            frame_ratio = (double)format.w / format.h;

            if (sizeInfo.WidthChanged)
            {
                Width = Width - image.Width + (sizeInfo.NewSize.Height * frame_ratio);
                //Width = image.ActualWidth;
            }
            else
            {
                Height = Height - image.Height + (sizeInfo.NewSize.Width / frame_ratio);
            }
        }

        void img_mousedown(object sender, MouseButtonEventArgs e)
        {
            if (filterPositionning)
            {
                _HideMenu = _HideMenu_previousstatus;
                filterPositionning = false;
            }
            else
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
            if (filterPositionning)
            {
                System.Windows.Point GetMousePos = Mouse.GetPosition(this);

                currentFilter.X = (GetMousePos.X) / (ActualWidth);
                currentFilter.Y = (GetMousePos.Y) / (ActualHeight);
                UpdateFilers();

                return;
            }

            if (!_HideMenu)
            {
                grd_visu.Visibility = Visibility.Visible;
                mouseEnterEventDelayTimer.Start();
            }
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

        private void ctxm_nativesize_Switch_Click(object sender, RoutedEventArgs e)
        {
            if (frame != null && !frame.Empty())
            {
                forceResize = true;
                Width = frame.Width;
                Height = frame.Height;
                actualWidth = Width;
                forceResize = false;
            }
        }

        private void ctxm_overlaidMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_HideMenu)
                grd_visu.Visibility = Visibility.Collapsed;
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
            QUIT();
            Close();
        }
        #endregion

        void QUIT()
        {
            isRunning = false;
            CaptureCameraStop();
            FiltreCameraStop();

            if (filtre_manager != null)
                filtre_manager.ReallyClose();
        }

        void INITS()
        {
            ListDevices();
            UpdateFilers();
            ManageFilter("");
            FullScreenManagement();
            MouseEnterEventDelay_Init();
            WindowBarManagement();

            Get_WindowsScreenScale();
            FilterManager_INIT();

            if (AUTORELOAD)
            {
                Config_Load();
                Config_Filters_Load();
            }
        }

        #region SCREENSHOT
        void Screenshot_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                Screenshot();

            if (e.RightButton == MouseButtonState.Pressed)
                Open_ScreenshotFolder();
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    Screenshot();
                    break;
            }
        }

        void Screenshot()
        {
            if (ScreenshotFolder != null && System.IO.Directory.Exists(ScreenshotFolder))
                if (frame != null && !frame.Empty())
                {
                    Mat screenshot = null;
                    if (ckb_savewithfilter.IsChecked == true && !filterframe.Empty())
                    {

                        throw new Exception("TODO");
                        //frame
                        //filterframe

                        //screenshot
                    }
                    else
                    {
                        screenshot = frame.Clone();
                    }
                    string extension = ".jpg";
                    string filename = ScreenshotFolder + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fff") + extension;
                    screenshot.SaveImage(filename);
                    screenshotCount++;
                    screenshotFile_Last = filename;
                }
        }

        void ScreenshotFolder_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                Set_ScreenshotFolder();

            if (e.RightButton == MouseButtonState.Pressed)
                Open_ScreenshotFolder();
        }

        void Open_ScreenshotFolder()
        {
            if (Directory.Exists(ScreenshotFolder))
            {
                if (File.Exists(screenshotFile_Last))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "/select, \"" + screenshotFile_Last + "\""));
                else
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", ScreenshotFolder));
            }
        }

        void Set_ScreenshotFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (ScreenshotFolder != null)
                    dialog.SelectedPath = ScreenshotFolder;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ScreenshotFolder = dialog.SelectedPath;
                }
            }
        }
        #endregion

        #region DEVICES MANAGEMENT
        private void AllDevices_Click(object sender, MouseButtonEventArgs e)
        {
            ListDevices();
        }

        void ListDevices()
        {
            devices = VideoInInfo.EnumerateVideoDevices_JJ();
            if (cbx_device != null)
                cbx_device.ItemsSource = devices.Select(d => d.Name).ToList();
        }

        void Play()
        {
            chrono.Start();
            isRunning = true;
            indexDevice = cbx_device.SelectedIndex;
            CaptureCamera(indexDevice);

            isRunningFiltre = true;
            FiltreCamera();
        }

        private void Combobox_CaptureDevice_Change(object sender, SelectionChangedEventArgs e)
        {
            indexDevice = cbx_device.SelectedIndex;
            current_device = devices[indexDevice];
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
            //newFormat = true;
        }

        private void Combobox_CaptureDeviceFormat_Change(object sender, SelectionChangedEventArgs e)
        {
            if (cbx_deviceFormat.SelectedValue == null)
                return;

            format = formats[cbx_deviceFormat.SelectedValue as string];
            formatName = cbx_deviceFormat.Items[cbx_deviceFormat.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            if (!isRunning)
                Play();

            newFormat = true;
        }
        #endregion

        #region CAPTURE MANAGEMENT
        void CaptureCamera(int index)
        {
            if (threadCapture != null && threadCapture.IsAlive)
            {
                threadCapture.Abort();
                Thread.Sleep(100);
            }
            indexDevice = index;
            threadCapture = new Thread(new ThreadStart(CaptureCameraCallback));
            threadCapture.Start();
        }

        void FiltreCamera()
        {
            if (threadFiltre != null && threadFiltre.IsAlive)
            {
                threadFiltre.Abort();
                Thread.Sleep(100);
            }
            threadFiltre = new Thread(new ThreadStart(FiltreCameraCallback));
            threadFiltre.Start();
        }

        void CaptureCameraStop()
        {
            if (isRunning)
            {
                isRunning = false;
                Thread.Sleep(100);
                threadCapture?.Abort();
            }
        }

        void FiltreCameraStop()
        {
            if (isRunningFiltre)
            {
                isRunningFiltre = false;
                Thread.Sleep(100);
                threadFiltre?.Abort();
            }
        }

        string Get_current_fourcc()
        {
            int intfourcc = (int)capture.Get(VideoCaptureProperties.FourCC);

            byte[] bytesfourcc = BitConverter.GetBytes(intfourcc);
            char c1 = Convert.ToChar(bytesfourcc[0]);
            char c2 = Convert.ToChar(bytesfourcc[1]);
            char c3 = Convert.ToChar(bytesfourcc[2]);
            char c4 = Convert.ToChar(bytesfourcc[3]);
            return new string(new char[] { c1, c2, c3, c4 });
        }

        void CaptureCameraCallback()
        {
            int current_indexDevice = indexDevice;
            frame = new Mat();
            capture = new OpenCvSharp.VideoCapture(indexDevice);

            capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
            capture.Set(VideoCaptureProperties.FrameWidth, format.w);
            capture.Set(VideoCaptureProperties.FrameHeight, format.h);
            capture.Set(VideoCaptureProperties.Fps, format.fr);
            capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString(format.format));

            if (capture.IsOpened())
            {
                if (configLoading && cameraConfiguration != null)
                {
                    while (frame.Empty())
                    {
                        Thread.Sleep(10);
                        capture.Read(frame);
                    }
                    newFormat = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        forceResize = true;
                        Width = cameraConfiguration.width;
                        Height = cameraConfiguration.height;
                        configLoading = false;
                    });
                    Filter_Update();
                }

                while (isRunning)
                {
                    if (indexDevice != current_indexDevice)
                    {
                        capture = new OpenCvSharp.VideoCapture(indexDevice);
                        capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
                        current_indexDevice = indexDevice;
                        newFormat = true;
                    }

                    if (newFormat)
                    {
                        capture.Set(VideoCaptureProperties.FrameWidth, format.w);
                        capture.Set(VideoCaptureProperties.FrameHeight, format.h);
                        capture.Set(VideoCaptureProperties.Fps, format.fr);
                        capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString(format.format));

                        frame_ratio = (double)format.w / format.h;
                        actualWidth = format.w;
                        Application.Current.Dispatcher.Invoke(() => { Width = actualWidth; });

                        capture.Read(frame);

                        Filter_Update();

                        newFormat = false;
                    }

                    capture.Read(frame);
                    framegrabbed++;

                    if (!frame.Empty())
                    {
                        framereallygrabbed++;

                        if (roi_enabled)
                        {
                            frame = ROI(frame, roi);
                        }

                        frame = RotationFlip(frame, flip_h, flip_v, rotation);

                        Show(frame);
                    }
                }
            }
            capture.Dispose();
        }

        void FiltreCameraCallback()
        {
            while (isRunningFiltre)
            {
                try
                {
                    if (filtres.Count > 0)
                    {
                        Mat f;
                        if (!filterframe.Empty())
                            f = filterframe.Clone();
                        else
                            f = new Mat(frame.Size(), MatType.CV_8UC4);

                        if (filtres_aumoins1dynamic)
                        {
                            DateTime dt = DateTime.Now;
                            foreach (Filtre filtre in filtres)
                            {
                                if (filtre.Dynamic && filtre.enable)
                                {
                                    OpenCvSharp.Point p = new OpenCvSharp.Point(filtre.X * filterframe.Width, filtre.Y * filterframe.Height);
                                    switch (filtre._type)
                                    {
                                        case Filtre.FiltreType.texte:
                                            Filtre_TXT ft = (Filtre_TXT)filtre;

                                            Scalar ftcolor = new Scalar(ft.color.B, ft.color.G, ft.color.R, ft.color.A);
                                            string txt = "";
                                            switch (ft.filtre_TXT_Type)
                                            {
                                                case Filtre_TXT.Filtre_TXT_Type.Date:
                                                    txt = dt.ToString("yyyy-MM-dd");
                                                    break;
                                                case Filtre_TXT.Filtre_TXT_Type.Time:
                                                    txt = dt.ToString("HH:mm:ss");
                                                    break;
                                                case Filtre_TXT.Filtre_TXT_Type.Time_ms:
                                                    txt = dt.ToString("HH:mm:ss.fff");
                                                    break;
                                                case Filtre_TXT.Filtre_TXT_Type.Date_Time:
                                                    txt = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                                    break;
                                                case Filtre_TXT.Filtre_TXT_Type.Date_Time_ms:
                                                    txt = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                                    break;
                                                case Filtre_TXT.Filtre_TXT_Type.FrameNumber:
                                                    txt = framereallygrabbed.ToString();
                                                    break;
                                                case Filtre_TXT.Filtre_TXT_Type.FPS:
                                                    txt = _FPS.ToString("f2");
                                                    break;
                                            }

                                            int FontThickness_MAX;
                                            if (ft.Border)
                                            {
                                                FontThickness_MAX = ft.FontThickness_Border;
                                                if (ft.FontThickness > FontThickness_MAX)
                                                    FontThickness_MAX = ft.FontThickness;
                                            }
                                            else
                                            {
                                                FontThickness_MAX = ft.FontThickness;
                                            }
                                            OpenCvSharp.Size textsize = Cv2.GetTextSize(txt, ft.font, ft.FontScale, FontThickness_MAX, out int Y_baseline);

                                            switch (ft.origine)
                                            {
                                                case Filtre.TypeOrigine.UpLeft: p.Y += textsize.Height; break;
                                                case Filtre.TypeOrigine.UpMiddle: p.Y += textsize.Height; p.X -= textsize.Width / 2; break;
                                                case Filtre.TypeOrigine.UpRight: p.Y += textsize.Height; p.X -= textsize.Width; break;
                                                case Filtre.TypeOrigine.MiddleLeft: p.Y += textsize.Height / 2; break;
                                                case Filtre.TypeOrigine.Middle: p.Y += textsize.Height / 2; p.X -= textsize.Width / 2; break;
                                                case Filtre.TypeOrigine.MiddleRight: p.Y += textsize.Height / 2; p.X -= textsize.Width; break;
                                                case Filtre.TypeOrigine.DownLeft: break;
                                                case Filtre.TypeOrigine.DownMiddle: p.X -= textsize.Width / 2; break;
                                                case Filtre.TypeOrigine.DownRight: p.X -= textsize.Width; break;
                                            }
                                            if (ft.Border)
                                            {
                                                //bordure
                                                Scalar ftcolor_border = new Scalar(ft.color_Border.B, ft.color_Border.G, ft.color_Border.R, ft.color_Border.A);
                                                Cv2.PutText(f, txt, p, ft.font, ft.FontScale, ftcolor_border, ft.FontThickness_Border, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                                                Cv2.PutText(f, txt, p, ft.font, ft.FontScale, ftcolor, ft.FontThickness, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                                            }
                                            else
                                            {
                                                Cv2.PutText(f, txt, p, ft.font, ft.FontScale, ftcolor, ft.FontThickness, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                                            }
                                            break;

                                        case Filtre.FiltreType.image:

                                            break;
                                    }
                                }
                            }
                        }
                        if (!f.Empty())
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                imagecalque.Source = ImageProcessing.ImageConversion.Bitmap_to_ImageSource_2(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(f));
                            });
                    }

                    if (_FPS > 0)
                        Thread.Sleep((int)(1000 / _FPS));
                }
                catch (Exception ex)
                {

                }
            }
        }
        #endregion

        #region IMAGE MANAGEMENT
        void Show(Mat frame)
        {
            if (!frame.Empty())
            {
                Mat frameShowed;


                if (actualWidth < frame.Width && actualWidth > 0)
                {
                    frameShowed = new Mat();
                    Cv2.Resize(frame, frameShowed, new OpenCvSharp.Size(actualWidth, actualWidth / frame.Width * frame.Height), interpolation: InterpolationFlags.Cubic);
                }
                else
                    frameShowed = frame.Clone();

                _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameShowed);

                _Infos = "(" + capture.FourCC + ") " + capture.FrameWidth + "*" + capture.FrameHeight + " [" + (int)capture.Fps + "fps] " + frameShowed.Width + "*" + frameShowed.Height;
            }

            FPS();
        }

        void FPS()
        {
            long T = chrono.ElapsedMilliseconds;
            _FPS = 1000f / (T - T0);
            T0 = T;
            _fps = "[" + _FPS.ToString("N1") + " fps]";
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


        Mat RotationFlip(Mat frame, bool flip_h, bool flip_v, RotateFlags? rotation)
        {
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

            return frame;
        }

        #endregion

        #region CROP
        Mat ROI(Mat frame, OpenCvSharp.Rect roi)
        {
            Mat _out = new Mat();
            if (roi.Width > 0 &&
                    frame.Height - roi.Y > 0 &&
                    frame.Width - roi.X > 0 &&
                    frame.Height - (roi.Y + roi.Height) > 0 &&
                    frame.Width - (roi.X + roi.Width) > 0)
                _out = new Mat(frame, roi);
            else
                _out = frame;
            return _out;
        }

        void ctxm_cropSet_Click(object sender, RoutedEventArgs e)
        {
            if (frame.Empty())
                return;

            string window_name = "Valid ROI with 'Enter' or 'Space', Cancel with 'c'";
            OpenCvSharp.Rect newroi = Cv2.SelectROI(window_name, frame, true);
            if (newroi.Width > 0 && newroi.Height > 0)
            {
                roi = newroi;
                roi_enabled = true;
            }

            Cv2.DestroyWindow(window_name);
        }

        void ctxm_cropNone_Click(object sender, RoutedEventArgs e)
        {
            roi = new OpenCvSharp.Rect();
            roi_enabled = false;
        }
        #endregion

        #region FILTERS \ Calque par dessus image

        private void ctxm_filtreManager_Click(object sender, RoutedEventArgs e)
        {
            //Filtre_Manager filtre_manager = new Filtre_Manager();
            //filtre_manager._Link(this);
        }

        void UpdateFilers()
        {
            _filtres = new Dictionary<string, MenuItem>();
            ctxm_calque.Items.Clear();

            // "Pick Filter"
            StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
            System.Windows.Controls.Image im = ImageFileToImageWPF("pack://application:,,,/Resources/folder.png");
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
            _filtres.Add("", mi_none);

            // "Filters"
            if (Directory.Exists(dossierFiltres))
            {
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
                    _filtres.Add(fi.Name, mi);
                }
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
            this.filtername = filtername;
            if (filtername == "")
            {
                filterframe = new Mat();
                imagecalque.Source = null;
            }
            else
            {
                //imagecalque.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "filters\\" + filtername));


                //par mat : (EN DEV)
                filterframe = Cv2.ImRead(AppDomain.CurrentDomain.BaseDirectory + "filters\\" + filtername, ImreadModes.LoadGdal);//, ImreadModes.AnyColor);

                imagecalque.Source = ImageProcessing.ImageConversion.Bitmap_to_ImageSource_2(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(filterframe));
            }

            foreach (var item in _filtres)
                item.Value.IsChecked = (item.Key == filtername);
        }


        //NEW GENERATION
        void FilterManager_INIT()
        {
            filtre_manager = new Filtre_Manager();
            filtre_manager._Link(this);
        }

        private void FilterManager_Click(object sender, MouseButtonEventArgs e)
        {
            filtre_manager.Show();
        }

        public void Filter_Update()
        {
            if (frame == null || frame.Empty())
                return;
            try
            {
                filterframe = new Mat(frame.Size(), MatType.CV_8UC4);
                filtres_aumoins1dynamic = false;
                foreach (Filtre f in filtres)
                {
                    if (!f.enable)
                        continue;

                    OpenCvSharp.Point p = new OpenCvSharp.Point(f.X * filterframe.Width, f.Y * filterframe.Height);

                    switch (f._type)
                    {
                        case Filtre.FiltreType.texte:
                            Filtre_TXT ft = (Filtre_TXT)f;
                            if (ft.Static)
                            {
                                Scalar ftcolor = new Scalar(ft.color.B, ft.color.G, ft.color.R, ft.color.A);
                                string txt = "";
                                switch (ft.filtre_TXT_Type)
                                {
                                    case Filtre_TXT.Filtre_TXT_Type.Free:
                                        if (ft.txt == null || ft.txt == "")
                                            continue;

                                        txt = ft.txt;
                                        break;
                                    case Filtre_TXT.Filtre_TXT_Type.DeviceName:
                                        txt = current_device.Name;
                                        break;
                                    default:
                                        break;
                                }

                                int FontThickness_MAX;
                                if (ft.Border)
                                {
                                    FontThickness_MAX = ft.FontThickness_Border;
                                    if (ft.FontThickness > FontThickness_MAX)
                                        FontThickness_MAX = ft.FontThickness;
                                }
                                else
                                {
                                    FontThickness_MAX = ft.FontThickness;
                                }
                                OpenCvSharp.Size textsize = Cv2.GetTextSize(txt, ft.font, ft.FontScale, FontThickness_MAX, out int Y_baseline);

                                switch (ft.origine)
                                {
                                    case Filtre.TypeOrigine.UpLeft: p.Y += textsize.Height; break;
                                    case Filtre.TypeOrigine.UpMiddle: p.Y += textsize.Height; p.X -= textsize.Width / 2; break;
                                    case Filtre.TypeOrigine.UpRight: p.Y += textsize.Height; p.X -= textsize.Width; break;
                                    case Filtre.TypeOrigine.MiddleLeft: p.Y += textsize.Height / 2; break;
                                    case Filtre.TypeOrigine.Middle: p.Y += textsize.Height / 2; p.X -= textsize.Width / 2; break;
                                    case Filtre.TypeOrigine.MiddleRight: p.Y += textsize.Height / 2; p.X -= textsize.Width; break;
                                    case Filtre.TypeOrigine.DownLeft: break;
                                    case Filtre.TypeOrigine.DownMiddle: p.X -= textsize.Width / 2; break;
                                    case Filtre.TypeOrigine.DownRight: p.X -= textsize.Width; break;
                                }
                                if (ft.Border)
                                {
                                    //bordure
                                    Scalar ftcolor_border = new Scalar(ft.color_Border.B, ft.color_Border.G, ft.color_Border.R, ft.color_Border.A);
                                    Cv2.PutText(filterframe, txt, p, ft.font, ft.FontScale, ftcolor_border, ft.FontThickness_Border, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                                    Cv2.PutText(filterframe, txt, p, ft.font, ft.FontScale, ftcolor, ft.FontThickness, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                                }
                                else
                                {
                                    Cv2.PutText(filterframe, txt, p, ft.font, ft.FontScale, ftcolor, ft.FontThickness, lineType: LineTypes.AntiAlias, bottomLeftOrigin: false);
                                }
                            }
                            if (ft.Dynamic)
                            {
                                filtres_aumoins1dynamic = true;
                            }

                            break;

                        case Filtre.FiltreType.image:

                            break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        bool filterPositionning = false;
        bool _HideMenu_previousstatus;

        public void SetFilterPosition(Filtre currentFilter)
        {
            this.currentFilter = currentFilter;
            //positionner la souris sur la video
            double x = WindowsScreenScale * (this.Left + currentFilter.X * ActualWidth);
            double y = WindowsScreenScale * (this.Top + currentFilter.Y * ActualHeight);
            SetCursorPos((int)x, (int)y);


            _HideMenu_previousstatus = _HideMenu;
            _HideMenu = false;
            filterPositionning = true;
            this.Activate();
        }


        #endregion

        #region DPI


        [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        public enum DeviceCap
        {
            /// <summary>
            /// Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            /// Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90

            // Other constants may be founded on pinvoke.net
        }

        void Get_WindowsScreenScale()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int Xdpi = GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSX);
            int Ydpi = GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSY);
            WindowsScreenScale = (double)Xdpi / 96;
        }
        #endregion

        #region CAMERA SETTINGS


        //A (modified) definition of OleCreatePropertyFrame found here: http://groups.google.no/group/microsoft.public.dotnet.languages.csharp/browse_thread/thread/db794e9779144a46/55dbed2bab4cd772?lnk=st&q=[DllImport(%22olepro32.dll%22)]&rnum=1&hl=no#55dbed2bab4cd772
        [System.Runtime.InteropServices.DllImport("oleaut32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, ExactSpelling = true)]
        public static extern int OleCreatePropertyFrame(
            IntPtr hwndOwner,
            int x,
            int y,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpszCaption,
            int cObjects,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Interface, ArraySubType=System.Runtime.InteropServices.UnmanagedType.IUnknown)]
            ref object ppUnk,
            int cPages,
            IntPtr lpPageClsID,
            int lcid,
            int dwReserved,
            IntPtr lpvReserved);





        private void CAMERA_SETTINGS_Click(object sender, MouseButtonEventArgs e)
        {
            string name = current_device.Name;















            DirectShowLib.IBaseFilter theDevice = null;

            //Release COM objects
            if (theDevice != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(theDevice);
                theDevice = null;
            }
            //Create the filter for the selected video input device
            string devicepath = current_device.Name; // comboBox1.SelectedItem.ToString();
            theDevice = CreateFilter(DirectShowLib.FilterCategory.VideoInputDevice, devicepath);






            DirectShowLib.IBaseFilter dev = theDevice;
            //Get the ISpecifyPropertyPages for the filter
            DirectShowLib.ISpecifyPropertyPages pProp = dev as DirectShowLib.ISpecifyPropertyPages;
            int hr = 0;

            if (pProp == null)
            {
                //If the filter doesn't implement ISpecifyPropertyPages, try displaying IAMVfwCompressDialogs instead!
                DirectShowLib.IAMVfwCompressDialogs compressDialog = dev as DirectShowLib.IAMVfwCompressDialogs;
                if (compressDialog != null)
                {

                    hr = compressDialog.ShowDialog(DirectShowLib.VfwCompressDialogs.Config, IntPtr.Zero);
                    DirectShowLib.DsError.ThrowExceptionForHR(hr);
                }
                return;
            }

            //Get the name of the filter from the FilterInfo struct
            DirectShowLib.FilterInfo filterInfo;
            hr = dev.QueryFilterInfo(out filterInfo);
            DirectShowLib.DsError.ThrowExceptionForHR(hr);

            // Get the propertypages from the property bag
            DirectShowLib.DsCAUUID caGUID;
            hr = pProp.GetPages(out caGUID);
            DirectShowLib.DsError.ThrowExceptionForHR(hr);

            // Create and display the OlePropertyFrame
            object oDevice = (object)dev;


            //hr = OleCreatePropertyFrame(this.Handle, 0, 0, filterInfo.achName, 1, ref oDevice, caGUID.cElems, caGUID.pElems, 0, 0, IntPtr.Zero);
            hr = OleCreatePropertyFrame(new System.Windows.Interop.WindowInteropHelper(this).Handle, 0, 0, filterInfo.achName, 1, ref oDevice, caGUID.cElems, caGUID.pElems, 0, 0, IntPtr.Zero);
            DirectShowLib.DsError.ThrowExceptionForHR(hr);



            // Release COM objects
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(caGUID.pElems);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pProp);
            if (filterInfo.pGraph != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(filterInfo.pGraph);
            }
        }

        /// <summary>
        /// Enumerates all filters of the selected category and returns the IBaseFilter for the 
        /// filter described in friendlyname
        /// </summary>
        /// <param name="category">Category of the filter</param>
        /// <param name="friendlyname">Friendly name of the filter</param>
        /// <returns>IBaseFilter for the device</returns>
        private DirectShowLib.IBaseFilter CreateFilter(Guid category, string friendlyname)
        {
            object source = null;
            Guid iid = typeof(DirectShowLib.IBaseFilter).GUID;
            foreach (DirectShowLib.DsDevice device in DirectShowLib.DsDevice.GetDevicesOfCat(category))
            {
                if (device.Name.CompareTo(friendlyname) == 0)
                {
                    device.Mon.BindToObject(null, null, ref iid, out source);
                    break;
                }
            }

            return (DirectShowLib.IBaseFilter)source;
        }
        #endregion

        #region SAVE/LOAD
        private void Save_Click(object sender, MouseButtonEventArgs e) { Config_Save(); }
        private void Load_Click(object sender, MouseButtonEventArgs e) { Config_Load(); }

        void Config_Save()
        {
            CameraConfiguration cc = new CameraConfiguration(deviceName, cbx_deviceFormat.SelectedValue as string, (int)ActualWidth, (int)ActualHeight);
            string txt = cc.ToString();

            File.WriteAllText(fichier_config, txt);
            //Properties.Settings.Default.config = txt;
            //Properties.Settings.Default.Save();
        }

        void Config_Load()
        {
            //string txt = Properties.Settings.Default.config;
            string txt = System.IO.File.Exists(fichier_config)?File.ReadAllText(fichier_config):null;
            if (txt == null || txt == "")
                return;

            cameraConfiguration = new CameraConfiguration(txt);

            cbx_device.SelectedValue = cameraConfiguration.deviceName;
            Thread.Sleep(100);
            cbx_deviceFormat.SelectedValue = cameraConfiguration.format;
            configLoading = true;
        }

        public void Config_Filters_Save()
        {
            var jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
            string json = JsonConvert.SerializeObject(filtres, Formatting.Indented, jset);

            File.WriteAllText(fichier_filtres, json);
            //Properties.Settings.Default.filters = json;
            //Properties.Settings.Default.Save();
        }

        public void Config_Filters_Load()
        {
            //string json = Properties.Settings.Default.filters;
            string json = System.IO.File.Exists(fichier_filtres) ? File.ReadAllText(fichier_filtres) : null;
            if (json == null || json == "")
                return;

            var jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
            ObservableCollection<Filtre> c = (ObservableCollection<Filtre>)JsonConvert.DeserializeObject(json, jset);
            filtres = c;

            filtres_aumoins1dynamic = false;
            foreach (Filtre item in filtres)
            {
                item.PropertyChanged += filtre_manager.FilterPropertyChanged;
                if (item.isTxt && ((Filtre_TXT)item).Dynamic)
                    filtres_aumoins1dynamic = true;
            }
        }
        #endregion
    }
}