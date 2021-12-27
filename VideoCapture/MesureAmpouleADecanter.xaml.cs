using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Communication_Série;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace OpenCVSharpJJ
{
    public partial class MesureAmpouleADecanter : System.Windows.Window, INotifyPropertyChanged
    {

        #region BINDINGS
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string _title
        {
            get { return title + _fps; }
            set
            {
                title = value;
                OnPropertyChanged("_title");
            }
        }
        string title = "Mesure ampoule à décanter";

        public string _fps
        {
            get { return fps; }
            set
            {
                fps = value;
                OnPropertyChanged("_title");
            }
        }
        string fps;

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
        public System.Drawing.Bitmap _image1
        {
            get
            {
                if (image1 == null)
                    return null;
                return image1;
            }
            set
            {
                if (image1 != value)
                {
                    image1 = value;
                    OnPropertyChanged("_image1");
                }
            }
        }
        System.Drawing.Bitmap image1;

        public System.Drawing.Bitmap _image2
        {
            get
            {
                if (image2 == null)
                    return null;
                return image2;
            }
            set
            {
                if (image2 != value)
                {
                    image2 = value;
                    OnPropertyChanged("_image2");
                }
            }
        }
        System.Drawing.Bitmap image2;

        public System.Drawing.Bitmap _image3
        {
            get
            {
                if (image3 == null)
                    return null;
                return image3;
            }
            set
            {
                if (image3 != value)
                {
                    image3 = value;
                    OnPropertyChanged("_image3");
                }
            }
        }
        System.Drawing.Bitmap image3;

        public ObservableCollection<string> ArduinoMessages
        {
            get
            {
                return arduinoMessages;
            }
            set
            {
                arduinoMessages = value;
                OnPropertyChanged("ArduinoMessages");
            }
        }
        ObservableCollection<string> arduinoMessages = new ObservableCollection<string>();

        #endregion

        #region PARAMETERS
        long T0;
        Mat frame;
        Mat frameGray;
        Mat cannymat;
        Mat BGR;

        Mat[] bgr;
        Thread thread;
        int indexDevice;
        VideoInInfo.Format format;
        Dictionary<string, VideoInInfo.Format> formats;
        VideoCapture capture;
        bool isRunning = false;
        bool first = true;
        System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();

        bool display_in_CV_IHM = false;
        OpenCvSharp.Window window;
        VideoWriter videoWriter;
        OpenCvSharp.Rect roi;

        Communication_Série.Communication_Série cs;
        string buffer;
        char[] split_car = new char[] { '\n' };

        Dictionary<System.Windows.Controls.Image, System.Windows.Controls.ComboBox> ImagesCbxs;
        Dictionary<string, Mat> MatNamesToMats;
        Dictionary<System.Windows.Controls.Image, string> ImagesMatNames;
        Dictionary<System.Windows.Controls.Image, System.Drawing.Bitmap> ImagesBitmaps;
        #endregion

        #region WINDOW MANAGEMENT
        public MesureAmpouleADecanter()
        {
            InitializeComponent();
            DataContext = this;

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //CAMERA
            ListDevices();
            cbx_device.SelectedIndex = 1;
            cbx_deviceFormat.SelectedIndex = 0;
            Capture_Start();

            //ARDUINO
            COMBaudsRefresh();
            string nom = "USB Serial Port (COM4)";
            if (cbx_COM.Items.Contains(nom))
                cbx_COM.Text = nom;
            cbx_bauds.Text = "9600";

            //IHM
            ImagesCbxs = new Dictionary<System.Windows.Controls.Image, ComboBox>();
            ImagesCbxs.Add(image, cbx_image0);
            ImagesCbxs.Add(image_1, cbx_image1);
            ImagesCbxs.Add(image_2, cbx_image2);
            ImagesCbxs.Add(image_3, cbx_image3);

            MatNamesToMats = new Dictionary<string, Mat>();
            MatNamesToMats.Add("frame", frame);
            MatNamesToMats.Add("frameGray", frameGray);
            MatNamesToMats.Add("cannymat", cannymat);
            MatNamesToMats.Add("BGR", BGR);

            foreach (ComboBox cbx in ImagesCbxs.Values)
                foreach (string nomImage in MatNamesToMats.Keys)
                    cbx.Items.Add(nomImage);

            ImagesMatNames = new Dictionary<System.Windows.Controls.Image, string>();
            ImagesMatNames.Add(image, "frame");
            ImagesMatNames.Add(image_1, "frameGray");
            ImagesMatNames.Add(image_2, "cannymat");
            ImagesMatNames.Add(image_3, "BGR");

            ImagesBitmaps = new Dictionary<System.Windows.Controls.Image, Bitmap>();
            ImagesBitmaps.Add(image, _imageSource);
            ImagesBitmaps.Add(image_1, _image1);
            ImagesBitmaps.Add(image_2, _image2);
            ImagesBitmaps.Add(image_3, _image3);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            isRunning = false;
            CaptureCameraStop();
            cs?.PortCom_OFF();
        }

        private void gds_mouseenter(object sender, MouseEventArgs e)
        {
            dottedline.Visibility = Visibility.Visible;
        }

        private void gds_mouseleave(object sender, MouseEventArgs e)
        {
            dottedline.Visibility = Visibility.Hidden;
        }

        #endregion

        #region CAPTURE DEVICE
        private void Button_ListDevices_Click(object sender, MouseButtonEventArgs e)
        {
            ListDevices();
        }

        private void ListDevices()
        {
            var devices = VideoInInfo.EnumerateVideoDevices_JJ();
            if (cbx_device != null)
                cbx_device.ItemsSource = devices.Select(d => d.Name).ToList();
        }

        private void Button_CaptureDevice_Click(object sender, MouseButtonEventArgs e)
        {
            Capture_Start();
        }

        void Capture_Start()
        {
            chrono.Start();
            isRunning = !isRunning;

            if (isRunning)
            {
                indexDevice = cbx_device.SelectedIndex;
                CaptureCamera(indexDevice);
                Button_CaptureDevicePlay.Visibility = Visibility.Collapsed;
                Button_CaptureDeviceStop.Visibility = Visibility.Visible;
            }
            else
            {
                CaptureCameraStop();
                Button_CaptureDevicePlay.Visibility = Visibility.Visible;
                Button_CaptureDeviceStop.Visibility = Visibility.Collapsed;
            }
        }

        private void Combobox_CaptureDevice_Change(object sender, SelectionChangedEventArgs e)
        {
            indexDevice = cbx_device.SelectedIndex;
            formats = VideoInInfo.EnumerateSupportedFormats_JJ(indexDevice);
            cbx_deviceFormat.ItemsSource = formats.OrderBy(f => f.Value.format).ThenByDescending(f => f.Value.w).Select(f => f.Key);
        }

        private void Combobox_CaptureDeviceFormat_Change(object sender, SelectionChangedEventArgs e)
        {
            format = formats[cbx_deviceFormat.SelectedValue as string];
        }

        private void Button_CaptureDeviceRECORD_Click(object sender, MouseButtonEventArgs e)
        {
            if (videoWriter == null)
            {
                //ok
                //videoWriter = new VideoWriter("D:\\video.avi", FourCC.FromString("XVID"), 30, new OpenCvSharp.Size(1080, 1920));
                //ok
                videoWriter = new VideoWriter("D:\\Projets\\video.mp4", FourCC.MP4V, 30, new OpenCvSharp.Size(frame.Width, frame.Height));

                //pas ok
                //videoWriter = new VideoWriter("D:\\Projets\\video.mp4", FourCC.H264, 30, new OpenCvSharp.Size(frame.Width, frame.Height));

                Button_CaptureDeviceRECORD.Visibility = Visibility.Collapsed;
                Button_CaptureDeviceRECORDSTOP.Visibility = Visibility.Visible;
            }
            else
            {
                videoWriter.Release();
                videoWriter = null;
                Button_CaptureDeviceRECORD.Visibility = Visibility.Visible;
                Button_CaptureDeviceRECORDSTOP.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Clic droit sur image        
        private void img_mousedown(object sender, MouseButtonEventArgs e)
        {
            if (ctxm_hideothers.IsChecked)
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
        }

        private void ctxm_alwaysontop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = ctxm_alwaysontop.IsChecked;
        }

        private void ctxm_hideothers_Click(object sender, RoutedEventArgs e)
        {
            if (ctxm_hideothers.IsChecked)
            {
                grd_visu.Width = new GridLength(0);
                WindowStyle = WindowStyle.None;
            }
            else
            {
                grd_visu.Width = new GridLength(1, GridUnitType.Auto);
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        private void ctxm_quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //private void tbc_SelectionChange(object sender, SelectionChangedEventArgs e)
        //{
        //    if (e.Source is TabControl)
        //    {
        //        TabItem ti = (TabItem)e.AddedItems[0];
        //        if (ti == tbi_videocapture)
        //        {
        //            ListDevices();
        //        }
        //    }
        //}

        private void ctxm_calque_Add_Click(object sender, RoutedEventArgs e)
        {
            string file = @"D:\Images\_JJ\_\cadran.png";
            FileInfo fi = new FileInfo(file);

            StackPanel sp = new StackPanel();
            sp.Orientation = System.Windows.Controls.Orientation.Horizontal;

            System.Windows.Controls.Image im = new System.Windows.Controls.Image();
            im.Source = new BitmapImage(new Uri(file));
            im.Width = 20;
            im.Height = 20;
            sp.Children.Add(im);

            Label lbl = new Label();
            lbl.Content = fi.Name;
            sp.Children.Add(lbl);

            MenuItem mi = new MenuItem();
            mi.Header = sp;
            ctxm_calque.Items.Add(mi);


            _imageCalque = new Bitmap(file);
        }
        #endregion

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
            first = true;
            thread = null;
        }

        void CaptureCameraCallback()
        {
            int actualindexDevice = indexDevice;
            frame = new Mat();
            capture = new VideoCapture(indexDevice);
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

                    if (first)
                    {
                        Init();
                        first = false;
                    }
                    Cv2.Rotate(frame, frame, RotateFlags.Rotate90Counterclockwise);

                    if (!frame.Empty())
                        FrameProcessing(frame);

                    videoWriter?.Write(frame);

                    DisplayFPS();
                }
            }
        }
        #endregion

        void FrameProcessing(Mat frame)
        {
            frameGray = new Mat();
            Cv2.CvtColor(frame, frameGray, ColorConversionCodes.RGB2GRAY);

            cannymat = new Mat();
            Cv2.Canny(frameGray, cannymat, 50, 200);

            bgr[0] = cannymat;
            bgr[1] = new Mat(frame.Size(), MatType.CV_8UC1);
            bgr[2] = bgr[1];// new Mat(frame.Size(), MatType.CV_8UC1);

            BGR = new Mat();
            Cv2.Merge(bgr, BGR);

            UpdateDisplayImages();
        }

        #region DISPLAY IMAGE
        void Init()
        {
            bgr = new Mat[] { new Mat(frame.Size(), MatType.CV_8UC1),
                              new Mat(frame.Size(), MatType.CV_8UC1),
                              new Mat(frame.Size(), MatType.CV_8UC1)};
            if (display_in_CV_IHM)
                window = new OpenCvSharp.Window("dst image", frame);
        }

        private void UpdateDisplayImages()
        {
            //foreach (var item in ImagesMatNames)
            //{
            //    System.Windows.Controls.Image i = item.Key;
            //    switch (item.Value)
            //    {
            //        case "frame":

            //            break;
            //        case "frameGray":
            //            break;
            //        case "cannymat":
            //            break;
            //        case "BGR":
            //            break;
            //    }
            //    System.Drawing.Bitmap b = ImagesBitmaps[i];
            //    Mat m = MatNamesToMats[item.Value];
            //    Show(m, ref b);
            //}

            //OnPropertyChanged("_imageSource");
            //OnPropertyChanged("_image1");
            //OnPropertyChanged("_image2");
            //OnPropertyChanged("_image3");

            Show(frame, ref imageSource);
            OnPropertyChanged("_imageSource");

            Show(frameGray, ref image1);
            OnPropertyChanged("_image1");

            Show(cannymat, ref image2);
            OnPropertyChanged("_image2");

            Show(BGR, ref image3);
            OnPropertyChanged("_image3");

        }

        void Show(Mat frame)
        {
            if (display_in_CV_IHM)
            {
                if (window == null)
                    Init();
                window.ShowImage(frame);
                Cv2.WaitKey(1);
            }
            else
            {
                //WPF_IHM
                if (!frame.Empty())
                    _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
            }
        }

        void Show(Mat frame, ref System.Drawing.Bitmap bitmap)
        {
            if (frame != null && !frame.Empty())
                bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
        }

        private void Image_Enter(object sender, MouseEventArgs e)
        {
            System.Windows.Controls.Image im = sender as System.Windows.Controls.Image;
            ComboBox cbx = ImagesCbxs[im];
            cbx.Visibility = Visibility.Visible;
        }

        private void Image_Leave(object sender, MouseEventArgs e)
        {
            System.Windows.Controls.Image im = sender as System.Windows.Controls.Image;
            ComboBox cbx = ImagesCbxs[im];
            cbx.Visibility = Visibility.Collapsed;
        }

        private void ImageCBX_Enter(object sender, MouseEventArgs e)
        {
            ComboBox cbx = sender as ComboBox;
            cbx.Visibility = Visibility.Visible;
        }

        private void ImageCBX_Leave(object sender, MouseEventArgs e)
        {
            ComboBox cbx = sender as ComboBox;
            cbx.Visibility = Visibility.Collapsed;
        }

        private void ImageCBX_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbx = sender as ComboBox;
            string matName = cbx.Text;
            System.Windows.Controls.Image image = null;
            foreach (var item in ImagesCbxs)
            {
                ComboBox item_cbx = item.Value;
                if (item_cbx == cbx)
                {
                    image = item.Key;
                    break;
                }
            }
            ImagesMatNames[image] = matName;
        }

        void DisplayFPS()
        {
            long T = chrono.ElapsedMilliseconds;
            float f = 1000f / (T - T0);
            T0 = T;
            _fps = " [" + f.ToString("N1") + " fps]";
        }
        #endregion

        private void Button_CaptureDeviceROI_Click(object sender, RoutedEventArgs e)
        {
            string window_name = "Valid ROI with 'Enter' or 'Space', cancel with 'c'";
            OpenCvSharp.Rect newroi = Cv2.SelectROI(window_name, frame, true);
            if (newroi.Width > 0)
                roi = newroi;
            _title = roi.ToString();
            Cv2.DestroyWindow(window_name);
        }

        #region ARDUINO
        private void Button_COMRefresh_Click(object sender, MouseButtonEventArgs e)
        {
            COMBaudsRefresh();
        }

        void COMBaudsRefresh()
        {
            Communication_Série.Communication_Série.PortCom_Fill(cbx_COM);
            Communication_Série.Communication_Série.Bauds_Fill(cbx_bauds);
        }

        private void Button_Connexion_Click(object sender, MouseButtonEventArgs e)
        {
            if (cs == null)
            {
                cs = new Communication_Série.Communication_Série(cbx_COM.Text, cbx_bauds.Text, datareceived);
                if (cs.PortCom_ON())
                {
                    Button_DeviceConnect.Visibility = Visibility.Visible;
                    Button_DeviceDisconnect.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                cs.PortCom_OFF();
                cs = null;
                Button_DeviceConnect.Visibility = Visibility.Collapsed;
                Button_DeviceDisconnect.Visibility = Visibility.Visible;
            }
        }

        private void datareceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            sp.Encoding = Encoding.UTF8;
            string indata = sp.ReadExisting();
            indata = indata.Replace("\r", "");
            buffer += indata;

            string[] lignes = buffer.Split(split_car);

            for (int i = 0; i < lignes.Length - 1; i++)
            {
                ArduinoInterpretMessage(lignes[i]);
                AddTextInLBX(lignes[i]);
            }
            buffer = lignes[lignes.Length - 1];
        }

        float camera_pos;
        float camera_pos_max;
        private void ArduinoInterpretMessage(string txt)
        {
            string val_txt;
            if (txt.Contains("Position = "))
            {
                //Serial.print("Position = ");
                //Serial.print(d);
                //Serial.println("mm");
                val_txt = txt.Replace("Position = ", "");
                val_txt = val_txt.Replace("mm", "");
                val_txt = val_txt.Replace(".", ",");
                camera_pos = float.Parse(val_txt);
                SLD_camera_position(camera_pos);
            }
            if (txt.Contains("D max = "))
            {
                //Serial.print("D max = ");
                //Serial.print(d_abs_mm);
                //Serial.println("mm");
                val_txt = txt.Replace("D max = ", "");
                val_txt = val_txt.Replace("mm", "");
                val_txt = val_txt.Replace(".", ",");
                camera_pos_max = float.Parse(val_txt);
                SLD_camera_position_max(camera_pos_max);
            }
        }

        void SLD_camera_position(float value)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (value > sld_camera_position_mm.Maximum)
                        sld_camera_position_mm.Maximum = value;
                    sld_camera_position_mm.Value = value;
                }));
        }
        void SLD_camera_position_max(float value)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    sld_camera_position_mm.Maximum = value;
                }));
        }

        void AddTextInLBX(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    ArduinoMessages.Insert(0, message);
                    while (ArduinoMessages.Count > 100)
                        ArduinoMessages.RemoveAt(ArduinoMessages.Count - 1);
                }));

            OnPropertyChanged("ArduinoMessages");
        }

        private void tbx_txt_to_arduino_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendToArduino();
        }

        private void SendToArduino(object sender, MouseButtonEventArgs e)
        {
            SendToArduino();
        }
        private void SendToArduino()
        {
            string txt = tbx_txt_to_arduino.Text;
            SendToArduino(txt);
            tbx_txt_to_arduino.Text = "";
        }
        private void SendToArduino(string txt)
        {
            if (cs != null)
            {
                if (!txt.Contains('\n'))
                    txt += '\n';
                cs?.Envoyer(txt);
            }
            else
            {
                AddTextInLBX("!! ARDUINO DISCONNECTED !!");
            }
        }

        private void Button_Clear_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    ArduinoMessages.Clear();
                }));
            OnPropertyChanged("ArduinoMessages");
        }

        private void Button_Etalonnage_Click(object sender, RoutedEventArgs e)
        {
            SendToArduino("e");
        }

        private void Button_UP_Click(object sender, MouseButtonEventArgs e)
        {
            SendToArduino("h");
        }

        private void Button_DOWN_Click(object sender, MouseButtonEventArgs e)
        {
            SendToArduino("b");
        }
        private void Button_GetPosition_Click(object sender, RoutedEventArgs e)
        {
            SendToArduino("p");
        }

        private void Button_ARRETURGENCE_Click(object sender, RoutedEventArgs e)
        {
            SendToArduino("a");
        }
        #endregion
    }
}