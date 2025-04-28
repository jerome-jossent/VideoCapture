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
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using OpenCvSharp.WpfExtensions;

namespace VideoCapture
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        const string version = "version 2023/06/15";

        #region VARIABLES & PARAMETERS
        bool AUTORELOAD = true;

        bool configLoading = false;
        CameraConfiguration cameraConfiguration = null;

        string fichier_config = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.json";
        string fichier_filtres = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\filtres.json";
        Dictionary<string, CameraConfiguration> CONFIGURATIONS = new Dictionary<string, CameraConfiguration>();
        Dictionary<string, ObservableCollection<Filtre>> FILTRES = new Dictionary<string, ObservableCollection<Filtre>>();

        // AUDIO
        Dictionary<string, CSCore.CoreAudioAPI.MMDevice> audioDevices;
        CSCore.CoreAudioAPI.MMDevice currentAudioDevice;
        Thread threadCaptureAudio;
        bool isRunningCaptureAudio = false;
        public float AudioVolume { get; set; } = 1f;

        // VIDEO
        Thread threadCapture;
        bool isRunning = false;
        DirectShowLib.DsDevice[] devices;
        DirectShowLib.DsDevice current_device;
        string deviceName;
        int indexDevice;
        Dictionary<string, VideoInInfo.Format> formats;
        OpenCvSharp.VideoCapture capture;
        VideoInInfo.Format format;
        bool newFormat;
        string formatName;

        // Image capturée
        object lockobject = new object();
        Mat frame;
        Mat frameShowed;
        long framereallygrabbed = 0;
        double frame_ratio = 1;
        double _actualWidth;
        // flips & rotations
        bool flip_h;
        bool flip_v;
        RotateFlags? rotation;
        // region of interest (crop)
        OpenCvSharp.Rect roi = new OpenCvSharp.Rect();
        bool roi_enabled = false;

        // FPS
        long T0;
        System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();

        // Filtre
        Thread threadFiltre;
        bool isRunningFiltre = false;
        Filtre_Manager filtre_manager;
        Filtre currentFilter
        {
            get => filtre_manager.currentFilter; set
            {
                filtre_manager.currentFilter = value;
            }
        }

        Mat filterframe;
        Mat filterframe_dynamic;
        bool filterPositionning = false;
        bool filtres_aumoins1dynamic;
        System.Windows.Point XY_previous;

        // Fenêtre
        bool _HideMenu_previousstatus;
        System.Windows.Threading.DispatcherTimer mouseEnterEventDelayTimer = new System.Windows.Threading.DispatcherTimer();
        double MouseEnter_Delay_sec = 3;
        double WindowsScreenScale;
        [System.Runtime.InteropServices.DllImport("User32.dll")] static extern bool SetCursorPos(int X, int Y);

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


        public System.Windows.Media.ImageSource IMS
        {
            get
            {
                return ims;
            }
            set
            {
                ims = value;
                OnPropertyChanged("IMS");
            }
        }
        System.Windows.Media.ImageSource ims;

        public System.Windows.Media.ImageSource IMS_calque
        {
            get
            {
                return ims_calque;
            }
            set
            {
                ims_calque = value;
                OnPropertyChanged(nameof(IMS_calque));
            }
        }
        System.Windows.Media.ImageSource ims_calque;





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

        // Filtres         
        public ObservableCollection<Filtre> filtres
        {
            get { return __filtres; }
            set
            {
                __filtres = value;
                OnPropertyChanged(nameof(filtres));
            }
        }
        ObservableCollection<Filtre> __filtres = new ObservableCollection<Filtre>();

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

        bool screenshot_inprogress = false;

        System.Drawing.Bitmap imageCalque;
        #endregion

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

        bool window_size_change_by_code = false;
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (window_size_change_by_code) return;

            //qd avec barre windows bord H+45 => 30 *1.5
            //qd sans barre windows bord H+8  =>  5 *1.5

            window_size_change_by_code = true;
            if (sizeInfo.WidthChanged && sizeInfo.HeightChanged)
            {
                //alors changer (arbitrairement) la hauteur en fonction de la largeur et du ratio
                //Height = sizeInfo.NewSize.Width / frame_ratio + (HideWindowBar ? (30 * WindowsScreenScale) : 5 * WindowsScreenScale);
                Height = sizeInfo.NewSize.Width / frame_ratio;
                //this.RenderSize = new System.Windows.Size(sizeInfo.NewSize.Width, sizeInfo.NewSize.Width / frame_ratio);
            }
            else if (sizeInfo.WidthChanged)
            {
                //alors changer aussi la hauteur
                //Height = sizeInfo.NewSize.Width / frame_ratio + (HideWindowBar ? (30 * WindowsScreenScale) : 5 * WindowsScreenScale);
                Height = sizeInfo.NewSize.Width / frame_ratio;
                //this.RenderSize = new System.Windows.Size(sizeInfo.NewSize.Width, sizeInfo.NewSize.Width / frame_ratio);
            }
            else
            {
                //Width = sizeInfo.NewSize.Height * frame_ratio - (HideWindowBar ? (30 * WindowsScreenScale) : 5 * WindowsScreenScale);
                Width = sizeInfo.NewSize.Height * frame_ratio;
                //this.RenderSize = new System.Windows.Size(sizeInfo.NewSize.Height * frame_ratio, sizeInfo.NewSize.Height);
            }

            _actualWidth = Width;
            window_size_change_by_code = false;
        }

        void img_mousedown(object sender, MouseButtonEventArgs e)
        {
            if (currentFilter != null && currentFilter._type == Filtre.FiltreType.ruller)
            {
                Filtre_Ruller fr = currentFilter as Filtre_Ruller;
                currentFilter = fr.MouseDown(filterframe, filterframe_dynamic);
                if (currentFilter == null) //fin processus ruller
                    Filter_Update();
                return;
            }

            if (filterPositionning)
            {
                //reset positionning
                if (e.ChangedButton == MouseButton.Right)
                    currentFilter.XY = XY_previous;

                _HideMenu = _HideMenu_previousstatus;
                filterPositionning = false;
                currentFilter = null; //2025/04/24
            }
            else
            {
                //déplacement de la fenêtre
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
                currentFilter.XY = GetMousePosition();
                return;
            }

            if (currentFilter != null && currentFilter._type == Filtre.FiltreType.ruller)
            {
                currentFilter.XY = GetMousePosition();
                (currentFilter as Filtre_Ruller).MouseMove(filterframe_dynamic);
                return;
            }

            if (!_HideMenu)
            {
                grd_visu.Visibility = Visibility.Visible;
                mouseEnterEventDelayTimer.Start();
            }
        }

        System.Windows.Point GetMousePosition()
        {
            System.Windows.Point GetMousePos = Mouse.GetPosition(this);
            return new System.Windows.Point(GetMousePos.X / ActualWidth, GetMousePos.Y / ActualHeight);
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
                Width = frame.Width;
                Height = frame.Height;
                _actualWidth = Width;
                if (_actualWidth == double.NaN)
                {
                    bool quoi = true;
                }
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

        void INITS()
        {
            ListVideoDevices();
            ListAudioDevices();
            //UpdateFilers();
            //ManageFilter("");
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

        void QUIT()
        {
            isRunning = false;
            CaptureCameraStop();
            FiltreCameraStop();
            CaptureAudioStop();

            if (filtre_manager != null)
                filtre_manager.ReallyClose();
        }

        #region DEVICES VIDEO MANAGEMENT
        void AllDevices_Click(object sender, MouseButtonEventArgs e)
        {
            ListVideoDevices();
            ListAudioDevices();
        }

        void ListVideoDevices()
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
            CaptureCameraStart(indexDevice);

            isRunningFiltre = true;
            FiltreCameraStart();
        }

        void Combobox_CaptureDevice_Change(object sender, SelectionChangedEventArgs e)
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
            if (formats == null)
            {
                cbx_deviceFormat.IsEnabled = false;
            }
            else
            {
                cbx_deviceFormat.IsEnabled = true;
                cbx_deviceFormat.ItemsSource = formats.OrderBy(f => f.Value.format).ThenByDescending(f => f.Value.w).Select(f => f.Key);
            }

            deviceName = cbx_device.Items[cbx_device.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            if (CONFIGURATIONS.ContainsKey(deviceName))
            {
                cbx_deviceFormat.SelectedValue = CONFIGURATIONS[deviceName].format;
                LoadConfiguration();
            }
            else
            {
                if (formats != null)
                {
                    //set default format
                    cbx_deviceFormat.SelectedIndex = 0;
                }

                CONFIGURATIONS.Add(deviceName, cameraConfiguration);
            }
            cameraConfiguration = CONFIGURATIONS[deviceName];

            if (FILTRES.ContainsKey(deviceName))
            {
                LoadFiltres();
            }
        }

        void Combobox_CaptureDeviceFormat_Change(object sender, SelectionChangedEventArgs e)
        {
            if (cbx_deviceFormat.SelectedValue == null)
                return;

            format = formats[cbx_deviceFormat.SelectedValue as string];
            formatName = cbx_deviceFormat.Items[cbx_deviceFormat.SelectedIndex].ToString();
            OnPropertyChanged("_title");

            //cameraConfiguration = new CameraConfiguration(deviceName, format, ActualWidth, ActualHeight);
            cameraConfiguration = new CameraConfiguration(deviceName, format, format.w, format.h);

            if (!isRunning)
                Play();

            newFormat = true;
        }
        #endregion

        #region VIDEO MANAGEMENT : CAPTURE & FILTRES
        void CaptureCameraStart(int index)
        {
            if (threadCapture != null && threadCapture.IsAlive)
            {
                threadCapture.Abort();
                Thread.Sleep(100);
            }
            indexDevice = index;
            threadCapture = new Thread(new ThreadStart(CaptureCameraThread));
            threadCapture.Start();
        }

        void CaptureCameraThread()
        {
            int current_indexDevice = indexDevice;
            frame = new Mat();
            capture = new OpenCvSharp.VideoCapture(indexDevice);

            capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
            capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString(format.format));
            capture.Set(VideoCaptureProperties.FrameWidth, format.w);
            capture.Set(VideoCaptureProperties.FrameHeight, format.h);
            capture.Set(VideoCaptureProperties.Fps, format.fr);
            capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString(format.format));

            frame_ratio = (double)format.w / format.h;

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
                        Width = cameraConfiguration.width;
                        Height = cameraConfiguration.height;
                    });

                    Filter_Update();
                }

                while (isRunning)
                {
                    //changement de device
                    if (indexDevice != current_indexDevice)
                    {
                        capture = new OpenCvSharp.VideoCapture(indexDevice);
                        capture.Open(indexDevice, VideoCaptureAPIs.DSHOW);
                        current_indexDevice = indexDevice;
                        newFormat = true;
                    }

                    //changement de format
                    if (newFormat)
                    {
                        capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString(format.format));
                        capture.Set(VideoCaptureProperties.FrameWidth, format.w);
                        capture.Set(VideoCaptureProperties.FrameHeight, format.h);
                        capture.Set(VideoCaptureProperties.Fps, format.fr);
                        capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString(format.format));


                        // int fourcc = VideoWriter.FourCC('M', 'J', 'P', 'G');
                        //capture.Set(VideoCaptureProperties.FourCC, fourcc);

                        frame_ratio = (double)format.w / format.h;

                        _actualWidth = format.w;
                        if (_actualWidth == double.NaN)
                        {
                            bool quoi = true;
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Width = format.w;// _actualWidth; 
                            Height = format.h;
                        });

                        capture.Read(frame);

                        newFormat = false;
                    }

                    while (screenshot_inprogress)
                        Thread.Sleep(10);
                    frame.Dispose();
                    frame = new Mat();

                    capture.Read(frame);

                    if (frame.Empty())
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    //Filter_Update();

                    framereallygrabbed++;

                    if (roi_enabled)
                        frame = ROI(frame, roi);

                    frame = RotationFlip(frame, flip_h, flip_v, rotation);

                    Show(frame);
                }
            }
            capture.Dispose();
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

        void FiltreCameraStart()
        {
            if (threadFiltre != null && threadFiltre.IsAlive)
            {
                threadFiltre.Abort();
                Thread.Sleep(100);
            }
            threadFiltre = new Thread(new ThreadStart(FiltreCameraThread));
            threadFiltre.Start();
        }

        void FiltreCameraThread()
        {
            while (isRunningFiltre)
            {
                try
                {
                    int wait_ms = 100;
                    DateTime T0 = DateTime.Now;
                    if (filtres.Count > 0 && filterframe != null)
                    {
                        filterframe_dynamic = filterframe.Clone();

                        if (filtres_aumoins1dynamic)
                        {
                            DateTime dt = DateTime.Now;
                            foreach (Filtre filtre in filtres)
                            {
                                if (filtre.Dynamic && filtre.enable)
                                {
                                    switch (filtre._type)
                                    {
                                        #region "texte"
                                        case Filtre.FiltreType.texte:
                                            Filtre_TXT ft = (Filtre_TXT)filtre;

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
                                            ft.InsertText(filterframe_dynamic, txt);

                                            break;
                                        #endregion

                                        #region "image"
                                        case Filtre.FiltreType.image:
                                            Filtre_IMAGE fi = (Filtre_IMAGE)filtre;
                                            OpenCvSharp.Point pi = new OpenCvSharp.Point(filtre.XY.X * filterframe.Width, filtre.XY.Y * filterframe.Height);

                                            //lit la prochaine frame
                                            fi.videoCapture.Read(fi.mat);
                                            if (fi.mat.Empty())
                                            {
                                                fi.videoCapture.Set(VideoCaptureProperties.PosFrames, 0);
                                                fi.videoCapture.Read(fi.mat);
                                            }
                                            fi.InsertMat(filterframe_dynamic);
                                            wait_ms = (int)(1000 / fi.videoCapture.Fps);
                                            break;
                                        #endregion

                                        #region "ruller"
                                        case Filtre.FiltreType.ruller:
                                            Filtre_Ruller fr = (Filtre_Ruller)filtre;
                                            fr.DrawPointsAndLine(filterframe_dynamic);

                                            break;
                                            #endregion
                                    }
                                }
                            }
                        }
                        if (!filterframe_dynamic.Empty())
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                IMS_calque = OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(filterframe_dynamic);
                            });
                        }
                    }
                    int need_to_wait_ms = wait_ms - (int)DateTime.Now.Subtract(T0).TotalMilliseconds;
                    if (_FPS > 0)
                        Thread.Sleep(wait_ms);//100
                    //Thread.Sleep((int)(1000 / _FPS));
                }
                catch (Exception ex)
                {
                }
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
        #endregion

        #region AUDIO
        void ListAudioDevices()
        {
            cbx_deviceAudio.Items.Clear();
            audioDevices = new Dictionary<string, CSCore.CoreAudioAPI.MMDevice>();

            using (CSCore.CoreAudioAPI.MMDeviceCollection deviceCollection =
                new CSCore.CoreAudioAPI.MMDeviceEnumerator().EnumAudioEndpoints(
                    CSCore.CoreAudioAPI.DataFlow.Capture,
                    CSCore.CoreAudioAPI.DeviceState.Active))
            {
                foreach (var device in deviceCollection)
                {
                    audioDevices.Add(device.FriendlyName, device);
                    cbx_deviceAudio.Items.Add(device.FriendlyName);
                }
            }
        }

        void Combobox_CaptureDeviceAudio_Change(object sender, SelectionChangedEventArgs e)
        {
            currentAudioDevice = audioDevices[cbx_deviceAudio.SelectedItem.ToString()];
            CaptureAudio();
        }

        void CaptureAudio()
        {
            if (threadCaptureAudio != null && threadCaptureAudio.IsAlive)
            {
                threadCaptureAudio.Abort();
                Thread.Sleep(100);
            }
            threadCaptureAudio = new Thread(new ThreadStart(CaptureAudioCallback));
            threadCaptureAudio.Start();
        }

        void CaptureAudioStop()
        {
            if (isRunningCaptureAudio)
            {
                isRunningCaptureAudio = false;
                Thread.Sleep(100);
                threadCaptureAudio?.Abort();
            }
        }

        void CaptureAudioCallback()
        {
            using (var soundIn = new CSCore.SoundIn.WasapiCapture(true, CSCore.CoreAudioAPI.AudioClientShareMode.Shared, 30))
            {
                soundIn.Device = currentAudioDevice;
                soundIn.Initialize();
                CSCore.IWaveSource source = new CSCore.Streams.SoundInSource(soundIn) { FillWithZeros = true };

                soundIn.Start();

                using (var soundOut = new CSCore.SoundOut.WasapiOut())
                {
                    soundOut.Initialize(source);
                    soundOut.Play();

                    isRunningCaptureAudio = true;
                    while (isRunningCaptureAudio)
                    {
                        Thread.Sleep(100);
                        soundOut.Volume = AudioVolume;
                    }
                }
            }
        }
        #endregion

        #region IMAGE MANAGEMENT
        void Show(Mat frame)
        {
            GC.Collect();

            if (!frame.Empty())
            {
                GC.Collect();

                lock (lockobject)
                {
                    //if (_actualWidth < frame.Width && _actualWidth > 0)
                    if (_actualWidth / WindowsScreenScale < frame.Width)
                    {
                        frameShowed = new Mat();
                        Cv2.Resize(frame, frameShowed,
                            new OpenCvSharp.Size(_actualWidth, _actualWidth / frame_ratio),
                            interpolation: InterpolationFlags.Cubic);
                    }
                    else
                        frameShowed = frame.Clone();

                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                IMS = OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(frameShowed);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                if (ShowCPUMem)
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

        void ctxm_flipANDrotateMenu_Enter(object sender, MouseEventArgs e)
        {
            ComputeFlippedAndRotatedMiniatures();
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

        void ComputeFlippedAndRotatedMiniatures()
        {
            Mat miniature = new Mat();
            OpenCvSharp.Size s = new OpenCvSharp.Size();
            float ratio = (float)frameShowed.Width / frameShowed.Height;

            if (ratio > 1)
            {
                s.Width = 120;
                s.Height = (int)(120 / ratio);
            }
            else
            {
                s.Width = (int)(120 / ratio);
                s.Height = 120;
            }
            Cv2.Resize(frameShowed, miniature, s, interpolation: InterpolationFlags.Cubic);

            Mat miniature_r90 = new Mat(); Cv2.Rotate(miniature, miniature_r90, RotateFlags.Rotate90Clockwise);
            Mat miniature_r270 = new Mat(); Cv2.Rotate(miniature, miniature_r270, RotateFlags.Rotate90Counterclockwise);
            Mat miniature_r180 = new Mat(); Cv2.Rotate(miniature, miniature_r180, RotateFlags.Rotate180);
            Mat miniature_fv = new Mat(); Cv2.Flip(miniature, miniature_fv, FlipMode.X);
            Mat miniature_fh = new Mat(); Cv2.Flip(miniature, miniature_fh, FlipMode.Y);

            Dispatcher.Invoke(() =>
            {
                this.miniature_r90.Source = miniature_r90.ToBitmapSource();
                this.miniature_r270.Source = miniature_r270.ToBitmapSource();
                this.miniature_r180.Source = miniature_r180.ToBitmapSource();
                this.miniature_fv.Source = miniature_fv.ToBitmapSource();
                this.miniature_fh.Source = miniature_fh.ToBitmapSource();
            });
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
        void FilterManager_INIT()
        {
            filtre_manager = new Filtre_Manager();
            filtre_manager._Link(this);
        }

        private void FilterManager_Click(object sender, MouseButtonEventArgs e)
        {
            filtre_manager.Show();
            filtre_manager.Activate();
        }

        public void Filter_Update()
        {
            try
            {
                if (frame == null || frame.Empty())
                    return;

                filterframe = new Mat(frame.Size(), MatType.CV_8UC4);
                filtres_aumoins1dynamic = false;
                foreach (Filtre f in filtres)
                {
                    if (!f.enable)
                        continue;

                    switch (f._type)
                    {
                        case Filtre.FiltreType.texte:
                            Filtre_TXT ft = (Filtre_TXT)f;
                            ft.InsertText(filterframe, current_device);
                            break;

                        case Filtre.FiltreType.image:
                            Filtre_IMAGE fi = (Filtre_IMAGE)f;
                            fi.InsertMat(filterframe);
                            break;

                        case Filtre.FiltreType.ruller:
                            Filtre_Ruller fr = (Filtre_Ruller)f;
                            if (fr.defined)
                            {
                                fr.DrawPointsAndLine(filterframe);
                                Filtre_TXT mesure = fr.DisplayMesure();
                                fr.mesure.InsertText(filterframe, fr.distance.ToString("f4"));
                            }
                            break;
                    }

                    if (f.Dynamic)
                        filtres_aumoins1dynamic = true;
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void ShowMatDebug(Mat frame)
        {
            Cv2.NamedWindow("Debug");
            Cv2.ImShow("Debug", frame);
            Cv2.WaitKey();
        }

        public void SetFilterPosition(Filtre movingFilter)
        {
            currentFilter = movingFilter;
            currentFilter.enable = true;
            XY_previous = currentFilter.XY;

            //positionner la souris sur la video
            double x = WindowsScreenScale * (this.Left + currentFilter.XY.X * ActualWidth);
            double y = WindowsScreenScale * (this.Top + currentFilter.XY.Y * ActualHeight);
            SetCursorPos((int)x, (int)y);

            _HideMenu_previousstatus = _HideMenu;
            _HideMenu = false;
            filterPositionning = true;
            this.Activate();
        }
        #endregion

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
                    screenshot_inprogress = true;
                    capture.Read(frame);

                    Mat screenshot = frame.Clone();
                    if (ckb_savewithfilter.IsChecked == true && !filterframe_dynamic.Empty())
                    {
                        Mat filter_frame = filterframe_dynamic.Clone();

                        //convertir frame en 4 channels
                        Mat frame4 = new Mat(screenshot.Size(), MatType.CV_8UC4);
                        Cv2.CvtColor(screenshot, frame4, ColorConversionCodes.RGB2RGBA);

                        var mat_capture = new Mat<Vec4b>(frame4);
                        var indexer_capture = mat_capture.GetIndexer();
                        var mat_filter = new Mat<Vec4b>(filter_frame);
                        var indexer_filter = mat_filter.GetIndexer();

                        //pixel par pixel
                        for (int y = 0; y < screenshot.Height; y++)
                        {
                            for (int x = 0; x < screenshot.Width; x++)
                            {
                                Vec4b pix_filter = indexer_filter[y, x];
                                byte alpha = pix_filter.Item3;

                                float coeff = (float)alpha / 255;

                                pix_filter = new Vec4b((byte)(pix_filter.Item0 * coeff),
                                                       (byte)(pix_filter.Item1 * coeff),
                                                       (byte)(pix_filter.Item2 * coeff),
                                                       alpha);
                                indexer_filter[y, x] = pix_filter;

                                coeff = 1 - coeff;

                                Vec4b pix_capture = indexer_capture[y, x];
                                pix_capture = new Vec4b((byte)(pix_capture.Item0 * coeff),
                                                        (byte)(pix_capture.Item1 * coeff),
                                                        (byte)(pix_capture.Item2 * coeff),
                                                        (byte)(255 - alpha));
                                indexer_capture[y, x] = pix_capture;
                            }
                        }

                        //Cv2.AddWeighted(mat_capture, 1, mat_filter, 1, 0, screenshot);
                        screenshot = mat_capture + mat_filter;
                    }
                    string extension = ".jpg";
                    string filename = ScreenshotFolder + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fff") + extension;
                    screenshot.SaveImage(filename);
                    screenshotCount++;
                    screenshotFile_Last = filename;
                    screenshot_inprogress = false;
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

        void CAMERA_SETTINGS_Click(object sender, MouseButtonEventArgs e)
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
        DirectShowLib.IBaseFilter CreateFilter(Guid category, string friendlyname)
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

        #region SAVE/LOAD   CONFIG & FILTRES

        #region CONFIG
        private void Save_Click(object sender, MouseButtonEventArgs e) { Config_Save(); }
        private void Load_Click(object sender, MouseButtonEventArgs e) { Config_Load(); }

        void Config_Save()
        {
            cameraConfiguration = new CameraConfiguration(deviceName, format, ActualWidth, ActualHeight);
            CONFIGURATIONS[deviceName] = cameraConfiguration;

            string json = JsonConvert.SerializeObject(CONFIGURATIONS, Formatting.Indented, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
            File.WriteAllText(fichier_config, json);
        }

        void Config_Load()
        {
            string json = File.Exists(fichier_config) ? File.ReadAllText(fichier_config) : null;
            if (json == null || json == "")
                return;

            CONFIGURATIONS = (Dictionary<string, CameraConfiguration>)JsonConvert.DeserializeObject(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
            LoadConfiguration();
        }
        void LoadConfiguration()
        {
            if (deviceName != null && CONFIGURATIONS.ContainsKey(deviceName))
            {
                cameraConfiguration = CONFIGURATIONS[deviceName];

                cbx_device.SelectedValue = cameraConfiguration.deviceName;
                Thread.Sleep(100);
                cbx_deviceFormat.SelectedValue = cameraConfiguration.format.Name;
                configLoading = true;
            }
        }
        #endregion

        #region FILTRES
        public void Config_Filters_Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(FILTRES, Formatting.Indented,
                    new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                File.WriteAllText(fichier_filtres, json);
            }
            catch (Exception ex)
            {

            }
        }

        public void Config_Filters_Load()
        {
            string json = File.Exists(fichier_filtres) ? File.ReadAllText(fichier_filtres) : null;
            if (json == null || json == "")
                return;

            FILTRES = (Dictionary<string, ObservableCollection<Filtre>>)JsonConvert.DeserializeObject(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
            LoadFiltres();
        }

        void LoadFiltres()
        {
            if (deviceName != null && FILTRES.ContainsKey(deviceName))
            {
                filtres = FILTRES[deviceName];

                filtres_aumoins1dynamic = false;
                foreach (Filtre item in filtres)
                {
                    item.PropertyChanged += filtre_manager.FilterPropertyChanged;

                    //if (item.isTxt && ((Filtre_TXT)item).Dynamic)
                    if (item._type == Filtre.FiltreType.texte && ((Filtre_TXT)item).Dynamic)
                        filtres_aumoins1dynamic = true;
                }
            }
            filtre_manager._ListFiltersUpdate();
        }
        #endregion

        #endregion

    }
}