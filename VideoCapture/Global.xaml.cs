using Advise;
using ImageProcessing;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.ComponentModel;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using OpenCvSharp.Text;
using System.IO;
using System.Net;
using System.IO.Compression;
using Tesseract;

namespace OpenCVSharpJJ
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class Global : System.Windows.Window, INotifyPropertyChanged
    {
        #region Bindings
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ChartValues<ObservablePoint> ValuesA { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesB { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesC { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesD { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesE { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesF { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesG { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesH { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesI { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesJ { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesK { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesL { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesM { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesN { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesO { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesP { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesQ { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesR { get; set; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> ValuesS { get; set; } = new ChartValues<ObservablePoint>();

        public string _video_path_in
        {
            get => Properties.Settings.Default.video_path_in;
            set
            {
                Properties.Settings.Default.video_path_in = value;
                OnPropertyChanged("_video_path_in");
                Properties.Settings.Default.Save();
            }
        }

        public string _video_path_out
        {
            get => Properties.Settings.Default.video_path_out;
            set
            {
                Properties.Settings.Default.video_path_out = value;
                OnPropertyChanged("_video_path_out");
                Properties.Settings.Default.Save();
            }
        }

        public string _results_path
        {
            get => Properties.Settings.Default.results_path;
            set
            {
                Properties.Settings.Default.results_path = value;
                OnPropertyChanged("_results_path");
                Properties.Settings.Default.Save();
            }
        }

        public string _results_path_in
        {
            get => Properties.Settings.Default.results_path_in;
            set
            {
                Properties.Settings.Default.results_path_in = value;
                OnPropertyChanged("_results_path_in");
                Properties.Settings.Default.Save();
            }
        }

        public string _IA_ServerIP
        {
            get => Properties.Settings.Default.IA_ServerIP;
            set
            {
                Properties.Settings.Default.IA_ServerIP = value;
                OnPropertyChanged("_IA_ServerIP");
                Properties.Settings.Default.Save();
            }
        }

        public int _IA_ServerPort
        {
            get => Properties.Settings.Default.IA_ServerPort;
            set
            {
                Properties.Settings.Default.IA_ServerPort = value;
                OnPropertyChanged("_IA_ServerPort");
                Properties.Settings.Default.Save();
            }
        }

        public string _title
        {
            get { return title; }
            set
            {
                title = value;
                OnPropertyChanged("_title");
            }
        }
        string title;

        public int _video_end_frame
        {
            get { return video_end_frame; }
            set
            {
                video_end_frame = value;
                OnPropertyChanged("_video_end_frame");
            }
        }
        int video_end_frame;

        public int _video_frame
        {
            get { return video_frame; }
            set
            {
                video_frame = value;
                OnPropertyChanged("_video_frame");
            }
        }
        int video_frame;

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

        double T;
        long T0;
        System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
        char[] sep = new char[] { '+' };

        enum mode { camera, video }
        mode _mode;
        bool display_in_CV_IHM;

        Thread thread;

        #region CAMERA
        int indexDevice;
        VideoInInfo.Format format;
        Dictionary<string, VideoInInfo.Format> formats;
        bool first = true;
        VideoCapture capture;
        bool processingDuringCaptureDevice;
        Mat frame;
        Mat cannymat;
        Mat[] bgr;
        bool isRunning = false;
        OpenCvSharp.Window window;
        #endregion

        #region VIDEO        
        OpenCvSharp.VideoCapture vc;
        SocketNet.ClientAsync client;
        List<Resultat> res;
        Scalar rouge = new Scalar(0, 0, 251);
        Scalar vert = new Scalar(0, 251, 0);

        bool next;
        bool generateAugmentedVideo;
        bool process_all_images = false;
        bool tempsreel = true;
        bool ia_ready;
        DateTime TLastFrame;
        #endregion

        #region GRAPH
        Dictionary<string, Serie> series;
        Dictionary<string, Serie> series2;

        Thread thread_graph;
        System.Collections.Concurrent.BlockingCollection<JJPoint> graph_data_buffer;
        bool graph_data_running;
        object graph_data_lock = new object();

        class Serie
        {
            public string code;
            public double y_val;
            public ChartValues<ObservablePoint> valeurs;
            public ScatterSeries courbe;
            public ihm_code_UC ihm_Code_UC;
            public Serie(string code, double y_val, ChartValues<ObservablePoint> valeurs, ScatterSeries courbe)
            {
                this.code = code;
                this.y_val = y_val;
                this.valeurs = valeurs;
                this.courbe = courbe;
                this.courbe.Title = code;

                ihm_Code_UC = new ihm_code_UC();
                ihm_Code_UC._code.Content = code;

            }
        }
        void InitGraph1()
        {
            //https://lvcharts.net/App/examples/v1/Wpf/Scatter%20Plot
            series = new Dictionary<string, Serie>();
            int i = 0;
            Serie sR = new Serie("BDG", i++, ValuesR, _S_R); series.Add(sR.code, sR);
            Serie sQ = new Serie("BCC", i++, ValuesQ, _S_Q); series.Add(sQ.code, sQ);
            Serie sP = new Serie("BCA", i++, ValuesP, _S_P); series.Add(sP.code, sP);
            Serie sO = new Serie("BBD", i++, ValuesO, _S_O); series.Add(sO.code, sO);
            Serie sN = new Serie("BBC", i++, ValuesN, _S_N); series.Add(sN.code, sN);
            Serie sM = new Serie("BBB", i++, ValuesM, _S_M); series.Add(sM.code, sM);
            Serie sL = new Serie("BBA", i++, ValuesL, _S_L); series.Add(sL.code, sL);
            Serie sK = new Serie("BAP", i++, ValuesK, _S_K); series.Add(sK.code, sK);
            Serie sJ = new Serie("BAO", i++, ValuesJ, _S_J); series.Add(sJ.code, sJ);
            Serie sI = new Serie("BAK", i++, ValuesI, _S_I); series.Add(sI.code, sI);
            Serie sH = new Serie("BAJ", i++, ValuesH, _S_H); series.Add(sH.code, sH);
            Serie sG = new Serie("BAI", i++, ValuesG, _S_G); series.Add(sG.code, sG);
            Serie sF = new Serie("BAH", i++, ValuesF, _S_F); series.Add(sF.code, sF);
            Serie sE = new Serie("BAG", i++, ValuesE, _S_E); series.Add(sE.code, sE);
            Serie sD = new Serie("BAF", i++, ValuesD, _S_D); series.Add(sD.code, sD);
            Serie sC = new Serie("BAC", i++, ValuesC, _S_C); series.Add(sC.code, sC);
            Serie sB = new Serie("BAB", i++, ValuesB, _S_B); series.Add(sB.code, sB);
            Serie sA = new Serie("BAA", i++, ValuesA, _S_A); series.Add(sA.code, sA);
            graph.AxisY[0].MinValue = 0;
            graph.AxisY[0].MaxValue = i;
            //BAA, BAB, BAC, BAF, BAG, BAH, BAI, BAJ, BAK, BAO, BAP, BBA, BBB, BBC, BBD, BCA, BCC, BDG
            this.DataContext = this;
            foreach (var item in series)
                item.Value.valeurs.Clear();
        }
        void InitGraph2()
        {
            //https://lvcharts.net/App/examples/v1/Wpf/Scatter%20Plot
            series2 = new Dictionary<string, Serie>();
            Serie sA = new Serie("BAB", 10, ValuesA, _S_A2); series2.Add(sA.code, sA);
            Serie sB = new Serie("BAC", 9, ValuesB, _S_B2); series2.Add(sB.code, sB);
            Serie sC = new Serie("BAF", 8, ValuesC, _S_C2); series2.Add(sC.code, sC);
            Serie sD = new Serie("BAG", 7, ValuesD, _S_D2); series2.Add(sD.code, sD);
            Serie sE = new Serie("BAH", 6, ValuesE, _S_E2); series2.Add(sE.code, sE);
            Serie sF = new Serie("BAJ", 5, ValuesF, _S_F2); series2.Add(sF.code, sF);
            Serie sG = new Serie("BAO", 4, ValuesG, _S_G2); series2.Add(sG.code, sG);
            Serie sH = new Serie("BBA", 3, ValuesH, _S_H2); series2.Add(sH.code, sH);
            Serie sI = new Serie("BBC", 2, ValuesI, _S_I2); series2.Add(sI.code, sI);
            Serie sJ = new Serie("BCA", 1, ValuesJ, _S_J2); series2.Add(sJ.code, sJ);
            foreach (var item in series2)
                item.Value.valeurs.Clear();
        }

        void InitGraphS2()
        {
            //https://lvcharts.net/App/examples/v1/Wpf/Scatter%20Plot
            series = new Dictionary<string, Serie>();
            int i = 0;
            Serie sR = new Serie("BDG", i++, ValuesR, _S2_R); series.Add(sR.code, sR);
            Serie sQ = new Serie("BCC", i++, ValuesQ, _S2_Q); series.Add(sQ.code, sQ);
            Serie sP = new Serie("BCA", i++, ValuesP, _S2_P); series.Add(sP.code, sP);
            Serie sO = new Serie("BBD", i++, ValuesO, _S2_O); series.Add(sO.code, sO);
            Serie sN = new Serie("BBC", i++, ValuesN, _S2_N); series.Add(sN.code, sN);
            Serie sM = new Serie("BBB", i++, ValuesM, _S2_M); series.Add(sM.code, sM);
            Serie sL = new Serie("BBA", i++, ValuesL, _S2_L); series.Add(sL.code, sL);
            Serie sK = new Serie("BAP", i++, ValuesK, _S2_K); series.Add(sK.code, sK);
            Serie sJ = new Serie("BAO", i++, ValuesJ, _S2_J); series.Add(sJ.code, sJ);
            Serie sI = new Serie("BAK", i++, ValuesI, _S2_I); series.Add(sI.code, sI);
            Serie sH = new Serie("BAJ", i++, ValuesH, _S2_H); series.Add(sH.code, sH);
            Serie sG = new Serie("BAI", i++, ValuesG, _S2_G); series.Add(sG.code, sG);
            Serie sF = new Serie("BAH", i++, ValuesF, _S2_F); series.Add(sF.code, sF);
            Serie sE = new Serie("BAG", i++, ValuesE, _S2_E); series.Add(sE.code, sE);
            Serie sD = new Serie("BAF", i++, ValuesD, _S2_D); series.Add(sD.code, sD);
            Serie sC = new Serie("BAC", i++, ValuesC, _S2_C); series.Add(sC.code, sC);
            Serie sB = new Serie("BAB", i++, ValuesB, _S2_B); series.Add(sB.code, sB);
            Serie sA = new Serie("BAA", i++, ValuesA, _S2_A); series.Add(sA.code, sA);
            graph_s2.AxisY[0].MinValue = 0;
            graph_s2.AxisY[0].MaxValue = i;
            //BAA, BAB, BAC, BAF, BAG, BAH, BAI, BAJ, BAK, BAO, BAP, BBA, BBB, BBC, BBD, BCA, BCC, BDG
            this.DataContext = this;

            foreach (var item in series)
                item.Value.valeurs.Clear();

            lb_codes.Items.Clear();
            foreach (Serie s in series.Values)
                lb_codes.Items.Add(s.ihm_Code_UC);
        }

        void AddPoints(string serie, double x, double y)
        {
            lock (graph_data_lock)
            {
                graph_data_buffer.Add(new JJPoint() { serie = serie, x = x, y = y });
            }
            //AddPoints(series[serie].valeurs, x, y);
        }
        void AddPoints(ChartValues<ObservablePoint> serie, double x, double y)
        {
            serie.Add(new ObservablePoint(x, y));
        }

        class JJPoint
        {
            public string serie;
            public double x, y;
        }

        void Graph_data_Manager()
        {
            graph_data_buffer = new System.Collections.Concurrent.BlockingCollection<JJPoint>();
            graph_data_running = true;

            Dictionary<string, List<ObservablePoint>> series_buffer = new Dictionary<string, List<ObservablePoint>>();

            while (graph_data_running)
            {
                if (graph_data_buffer.Count > 0)
                {
                    lock (graph_data_lock)
                    {
                        while (graph_data_buffer.Count > 0)
                        {
                            JJPoint jjPoint = graph_data_buffer.Take();
                            if (!series_buffer.ContainsKey(jjPoint.serie))
                                series_buffer.Add(jjPoint.serie, new List<ObservablePoint>());
                            series_buffer[jjPoint.serie].Add(new ObservablePoint(jjPoint.x, jjPoint.y));
                        }

                        foreach (var item in series_buffer)
                        {
                            series[item.Key].valeurs.AddRange(item.Value);
                            item.Value.Clear();
                        }
                        //https://lvcharts.net/App/examples/v1/wpf/Performance%20Tips
                    }
                }
                Thread.Sleep(100);
            }
        }
        #endregion

        #region Gestion Window
        public Global()
        {
            InitializeComponent();

            SetTempsReel();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;

            thread_graph = new Thread(new ThreadStart(Graph_data_Manager));
            thread_graph.Start();
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isRunning = false;
            client?.Close();

            CaptureCameraStop();
            VideoIAStop();
        }
        #endregion

        #region IHM Control Events
        private void Button_WEBCAM_Click(object sender, RoutedEventArgs e)
        {
            _mode = mode.camera;
            GO();
        }
        private void Button_VIDEO_Click(object sender, RoutedEventArgs e)
        {
            InitGraph1();
            _mode = mode.video;
            GO();
        }
        void Button_DATA_Click(object sender, RoutedEventArgs e)
        {
            InitGraph2();
            // load results
            Dictionary<double, List<Resultat>> resultats = ReadResultats(_results_path_in);

            // create CSV
            List<string> txt = new List<string>();
            string ligne = "";
            foreach (Resultat code in resultats.First().Value)
                ligne += ";" + code.code;
            txt.Add(ligne);

            foreach (var res in resultats)
            {
                ligne = res.Key.ToString();
                foreach (Resultat code in res.Value)
                    ligne += ";" + code.score_ia_1;
                txt.Add(ligne);
            }
            System.IO.File.WriteAllLines("D:\\Advise_video_IA.csv", txt);

            Dictionary<string, List<ObservablePoint>> series = new Dictionary<string, List<ObservablePoint>>();
            foreach (var res in resultats)
            {
                foreach (Resultat code in res.Value)
                {
                    if (code.score_ia_1 > code.Threshold)
                    {
                        if (!series.ContainsKey(code.code))
                            series.Add(code.code, new List<ObservablePoint>());
                        series[code.code].Add(new ObservablePoint(res.Key, series2[code.code].y_val));
                    }
                }
            }

            // fill Graph2
            foreach (var item in series)
            {
                series2[item.Key].valeurs.AddRange(item.Value);
            }

            // load video
            vc = new VideoCapture(_video_path_in);

            // Frame image buffer
            frame = new Mat();
        }
        void Button_LIVEIA_Click(object sender, RoutedEventArgs e)
        {
            InitGraphS2();

            // load video
            vc = new VideoCapture(_video_path_in);

            graph_s2.AxisX[0].MaxValue = (double)vc.FrameCount / vc.Fps;

            // Frame image buffer
            frame = new Mat();

            thread = new Thread(new ThreadStart(ReadVideoLIVEIA));
            thread.Start();
        }

        new void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        void SelectFile_VideoSource_in(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                _video_path_in = openFileDialog.FileName;
        }
        void SelectFile_VideoSource_out(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Vidéos|*.mp4";
            if (saveFileDialog.ShowDialog() == true)
                _video_path_out = saveFileDialog.FileName;
        }
        void SelectFile_Resultats_out(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Résultats|*.json";
            if (saveFileDialog.ShowDialog() == true)
                _results_path = saveFileDialog.FileName;
        }

        private void SelectFile_Resultats_in(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                _results_path_in = openFileDialog.FileName;
        }

        private void _rb_allframes_samples_Checked(object sender, RoutedEventArgs e)
        {
            process_all_images = _rb_allframes.IsChecked == true;
        }

        private void _ckb_tempsreel_Checked_UnChecked(object sender, RoutedEventArgs e)
        {
            SetTempsReel();
        }

        void visu_change(object sender, RoutedEventArgs e)
        {
            display_in_CV_IHM = _rb_b.IsChecked == true;
        }

        void CartesianChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (vc == null) return;
            var chart = (CartesianChart)sender;

            //lets get where the mouse is at our chart
            var mouseCoordinate = e.GetPosition(chart);

            //use ConverToChartValues : takes a point in pixes and scales it to our chart current scale/values
            var p = chart.ConvertToChartValues(mouseCoordinate);

            //in the Y section, lets use the raw value
            //vm.YPointer = p.Y;

            ////for X in this case we will only highlight the closest point.
            ////lets use the already defined ClosestPointTo extension
            ////it will return the closest ChartPoint to a value according to an axis.
            ////here we get the closest point to p.X according to the X axis

            //en fonction du y on sait quelle série est recherchée

            //ScatterSeries bestserie = (ScatterSeries)chart.Series[0];
            //var closetsPoint = bestserie.ClosestPointTo(p.X, AxisOrientation.X);

            //vm.XPointer = closetsPoint.X;

            string msg = "graph : " + (int)p.X + "," + (int)p.Y;
            int posframe = (int)(vc.Fps * p.X);

            if (vc.FrameCount > posframe)
            {
                vc.PosFrames = posframe;

                if (vc.Read(frame))
                    Show(frame);
                else
                {
                    msg = "Erreur de lecture de la frame : " + posframe + " type de fichier vidéo pas bien pris en charge.";
                }
            }
            else
                msg = "La frame " + posframe + " n'existe pas.";

            _title = msg;
        }

        void ReadVideoLIVEIA()
        {
            client?.Close();
            client = new SocketNet.ClientAsync(SocketNet.Receivemode.voidevent_then_access_to_datacollection, SocketNet.Datamode.bytearray);
            client.on_data_reception_in_data_collection += Client_on_data_reception_in_data_collection;
            client.ConnectToServer(_IA_ServerIP, _IA_ServerPort);

            int i = 0;
            Dictionary<double, List<Resultat>> resultats = new Dictionary<double, List<Resultat>>();

            //reset graph
            foreach (Serie item in series.Values)
                item.valeurs.Clear();

            resultats.Clear();
            isRunning = true;
            ia_ready = true;
            while (isRunning)
            {
                if (next)
                {
                    //processing
                    next = false;
                    if (frame.Width > 10 && !resultats.ContainsKey(T))
                        Send(frame, i);
                    else
                        next = false;// true;
                }
                else
                {
                    if (res != null)
                    {
                        StoreResults2(res, T, resultats);
                        res = null;
                    }
                    Thread.Sleep(10);
                }

                //if (resultats.ContainsKey(T))                
                //    foreach (Resultat code in resultats[T])
                //        series[code.code_P].ihm_Code_UC._SetScore(code.score_ia_1);

            }
            isRunning = false;
        }

        void CartesianChart2_MouseMove(object sender, MouseEventArgs e)
        {
            if (vc == null) return;
            var chart = (CartesianChart)sender;
            var mouseCoordinate = e.GetPosition(chart);
            var p = chart.ConvertToChartValues(mouseCoordinate);

            string msg = "graph : " + (int)p.X + "," + (int)p.Y;
            int posframe = (int)(vc.Fps * p.X);
            T = p.X;

            if (vc.FrameCount > posframe)
            {
                vc.PosFrames = posframe;

                if (vc.Read(frame))
                    Show(frame);
                else
                    msg = "Erreur de lecture de la frame : " + posframe + " type de fichier vidéo pas bien pris en charge.";
            }
            else
                msg = "La frame " + posframe + " n'existe pas.";

            _title = msg;
        }
        #endregion

        void SetTempsReel()
        {
            tempsreel = _ckb_tempsreel.IsChecked == true;
        }

        void GO()
        {
            chrono.Start();
            isRunning = !isRunning;
            switch (_mode)
            {
                case mode.camera:
                    if (isRunning)
                    {
                        processingDuringCaptureDevice = true;
                        CaptureCamera(0);
                    }
                    else
                        CaptureCameraStop();
                    break;

                case mode.video:
                    if (isRunning)
                        VideoIA();
                    else
                        VideoIAStop();
                    break;
            }
        }

        void VideoIA()
        {
            //thread = new Thread(new ThreadStart(ReadVideoSendFrame_GenerageAugmentedVideo));
            thread = new Thread(new ThreadStart(ReadVideoSendFrame_SaveIADATA));
            thread.Start();
        }

        void VideoIAStop()
        {
            thread?.Abort();
            graph_data_running = false;
            thread_graph?.Abort();
            first = true;
        }

        void ReadVideoSendFrame_SaveIADATA()
        {
            client = new SocketNet.ClientAsync(SocketNet.Receivemode.voidevent_then_access_to_datacollection, SocketNet.Datamode.bytearray);
            client.on_data_reception_in_data_collection += Client_on_data_reception_in_data_collection;
            client.ConnectToServer(_IA_ServerIP, _IA_ServerPort);

            vc = new VideoCapture(_video_path_in);

            // Frame image buffer
            frame = new Mat();

            // When the movie playback reaches end, Mat.data becomes NULL.
            int i = 0;
            double T;
            Dictionary<double, List<Resultat>> resultats = new Dictionary<double, List<Resultat>>();

            ia_ready = true;
            while (isRunning)
            {
                #region lit image n°i => T
                vc.Read(frame);
                i++;
                T = (double)i / vc.Fps;
                TLastFrame = DateTime.Now;
                #endregion

                #region fin ?
                if (
                    //i > 100 ||
                    frame.Empty())
                    break;
                #endregion

                #region Init image & visu
                if (first)
                {
                    Init();
                    first = false;
                }
                #endregion

                if (!process_all_images)
                {
                    if (ia_ready)
                    {
                        ia_ready = false;
                        //processing
                        next = false;
                        Send(frame, i);
                    }
                    else // !ia_ready
                    {
                        if (next) //ia processing is over
                        {
                            //Save Resultats
                            StoreResults(res, T, resultats);

                            //Title
                            _title = (100.0 * i / vc.FrameCount).ToString("N1") + "% ";

                            //Graph
                            double x = i / vc.Fps;
                            List<string> codes = new List<string>();
                            foreach (Resultat code in res)
                            {
                                if (code.score_ia_1 > code.Threshold)
                                {
                                    string[] _codes = code.code.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (string _code in _codes)
                                    {
                                        AddPoints(_code, x, series[_code].y_val);
                                        codes.Add(_code);
                                    }
                                }
                            }
                            ia_ready = true;
                        }
                    }
                }
                else // process_all_images
                {
                    #region Send & Wait
                    next = false;
                    Send(frame, i);

                    while (!next)
                    {
                        Thread.Sleep(10);
                        //update IHM ?
                    }
                    #endregion

                    //Save Resultats
                    StoreResults(res, T, resultats);

                    //Title
                    _title = (100.0 * i / vc.FrameCount).ToString("N1") + "% ";

                    //Graph
                    double x = i / vc.Fps;
                    List<string> codes = new List<string>();
                    foreach (Resultat code in res)
                    {
                        if (code.score_ia_1 > code.Threshold)
                        {
                            string[] _codes = code.code.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string _code in _codes)
                            {
                                AddPoints(_code, x, series[_code].y_val);
                                codes.Add(_code);
                            }
                        }
                    }
                }
                //visu
                Show(frame);

                if (!process_all_images && tempsreel)
                {
                    int t_deja_attendu_ms = (int)DateTime.Now.Subtract(TLastFrame).TotalMilliseconds;
                    int t_restant_ms = (int)(1000 / vc.Fps) - t_deja_attendu_ms;
                    if (t_restant_ms > 0)
                        Thread.Sleep(t_restant_ms);
                }
                //Console
                //Debug(i + " / " + vc.FrameCount + " " + string.Join(", ", codes));
            }

            SaveResults(_results_path, resultats);

            isRunning = false;

            MessageBox.Show("Traitement terminé avec succès.", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void StoreResults(List<Resultat> res, double temps_s, Dictionary<double, List<Resultat>> resultats)
        {
            resultats.Add(temps_s, res);
        }
        void StoreResults2(List<Resultat> res, double temps_s, Dictionary<double, List<Resultat>> resultats)
        {
            if (!resultats.ContainsKey(T))
            {
                List<Resultat> codes_add = new List<Resultat>();
                List<Resultat> code_remove = new List<Resultat>();
                for (int i = 0; i < res.Count; i++)
                {
                    Resultat code = res[i];
                    if (code.score_ia_1 > code.Threshold)
                    {
                        if (code.code.Contains(sep[0]))
                        {
                            string[] _codes = code.code.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string _code in _codes)
                            {
                                AddPoints(_code, T, series[_code].y_val);
                                Resultat newres = new Resultat("", _code, "", "", "", "", 0, 0, false, "", code.score_ia_1, code.Threshold, false, "", "", "");
                                codes_add.Add(newres);
                            }
                            code_remove.Add(code);
                        }
                        else
                        {
                            AddPoints(code.code, T, series[code.code].y_val);
                        }
                    }
                }
                foreach (Resultat item in code_remove)
                    res.Remove(item);

                foreach (Resultat item in codes_add)
                    res.Add(item);

                resultats.Add(temps_s, res);
            }
        }

        void SaveResults(string results_path, Dictionary<double, List<Resultat>> resultats)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(resultats, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(results_path, json);
        }

        Dictionary<double, List<Resultat>> ReadResultats(string path)
        {
            string json_precedent = System.IO.File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<double, List<Resultat>>>(json_precedent);
        }

        void ReadVideoSendFrame_GenerateAugmentedVideo()
        {
            client = new SocketNet.ClientAsync(SocketNet.Receivemode.voidevent_then_access_to_datacollection, SocketNet.Datamode.bytearray);
            client.on_data_reception_in_data_collection += Client_on_data_reception_in_data_collection;
            client.ConnectToServer(_IA_ServerIP, _IA_ServerPort);

            vc = new VideoCapture(_video_path_in);

            int sleepTime = (int)Math.Round(1000 / vc.Fps);

            // Frame image buffer
            frame = new Mat();

            // When the movie playback reaches end, Mat.data becomes NULL.
            int i = 0;

            double periode_s = 0.5;
            double T;
            double NextT = 0;

            using (VideoWriter newVideo = new VideoWriter(_video_path_out,
                //FourCC.FromString("X264"),
                //FourCC.FromString(vc.FourCC),
                FourCC.DIVX,
                vc.Fps, new OpenCvSharp.Size(vc.FrameWidth, vc.FrameHeight)))
            {
                while (isRunning)
                {
                    #region lit image n°i => T
                    vc.Read(frame);
                    i++;
                    T = (double)i / vc.Fps;
                    #endregion

                    #region fin ?
                    if (
                        //i > 100 ||
                        frame.Empty())
                        break;
                    #endregion

                    #region ne traite qu'une image toute les "periode_s"
                    if (NextT > T)
                        continue;
                    NextT += periode_s;
                    #endregion

                    #region Init image & visu
                    if (first)
                    {
                        Init();
                        first = false;
                    }
                    #endregion

                    #region Send & Wait
                    next = false;
                    Send(frame, i);

                    while (!next)
                        Thread.Sleep(10);
                    #endregion

                    //Get results
                    List<string> res = AugmentedFrame(frame);

                    //write video
                    if (generateAugmentedVideo)
                        newVideo.Write(frame);

                    //Title
                    this.Dispatcher.Invoke(() => { Title = (100.0 * i / vc.FrameCount).ToString("N1") + "% "; ; });

                    //visu
                    Show(frame);

                    //Console
                    Debug(i + " / " + vc.FrameCount + " " + string.Join(", ", res));

                    //Graph
                    double x = i / vc.Fps;
                    foreach (string code in res)
                    {
                        AddPoints(code, x, series[code].y_val);
                    }
                }
            }
            isRunning = false;

            MessageBox.Show("Traitement terminé avec succès.", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void Debug(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = DateTime.Now.ToString("HH:mm:ss.fff") + " - " + message;
                _lb.Items.Insert(0, lbi);
            });
        }

        List<string> AugmentedFrame(Mat frame)
        {
            List<string> lst = new List<string>();
            if (res == null)
                return lst;

            double text_height = 30;
            double offset = 100;
            double offsetrect = offset - 13;

            int i;
            for (i = 0; i < res.Count; i++)
            {
                Resultat r = res[i];
                if (r.score_ia_1 > r.Threshold)
                    lst.Add(r.code_P);

                Scalar color = (r.score_ia_1 > r.Threshold) ? vert : rouge;
                Cv2.PutText(frame,
                            r.code_P,
                            new OpenCvSharp.Point(0, 20 + i * text_height + offset),
                            HersheyFonts.HersheySimplex,
                            1,
                            color,
                            2);

                Cv2.Rectangle(frame,
                                new OpenCvSharp.Rect(80,
                                                     (int)(20 + i * text_height + offsetrect),
                                                     100,
                                                     10),
                                color,
                                0);

                Cv2.Rectangle(frame,
                                new OpenCvSharp.Rect(80,
                                                     (int)(20 + i * text_height + offsetrect),
                                                     (int)(r.score_ia_1 * 100),
                                                     10),
                                color,
                                -1);
            }
            Cv2.PutText(frame,
                        lst.Count.ToString(),
                        new OpenCvSharp.Point(0, 20 + i * text_height + offset),
                        HersheyFonts.HersheySimplex,
                        1,
                        (lst.Count > 0) ? vert : rouge,
                        2);
            return lst;
        }

        void Client_on_data_reception_in_data_collection(object sender, EventArgs e)
        {
            byte[] data = client.dataCollection_bytearray.Take();

            List<Image_ImageMetaData_tips> i_md_ts = ImagesSerialization.DeserializeImageFilesWithMetaData(data);

            if (i_md_ts != null)
            {
                string json = i_md_ts[0].metaData_Tips.comment;
                Message m = Message.FromJSON(json);
                res = m.res_Operateur;
            }
            next = true;
        }

        void Send(Mat frame, int i)
        {
            Bitmap bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
            List<Bitmap> image = new List<Bitmap>() { bmp };
            List<string> txt = new List<string>();// { i.ToString()};
            List<ImageMetaData_tips> mdts = new List<ImageMetaData_tips>();
            ImageMetaData_tips mdt = new ImageMetaData_tips();

            Dictionary<string, string> dico = new Dictionary<string, string>();
            dico.Add("activation_cards", false.ToString());
            mdt.comment = Newtonsoft.Json.JsonConvert.SerializeObject(dico);
            mdts.Add(mdt);
            txt.Add(ImageMetaData_tips.GetJSON(mdts));
            byte[] data = ImageProcessing.ImagesSerialization.SerializeImageFiles(image, txt, i.ToString());

            //envoi vers le serveur IA
            client?.SendMessage(data);
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
            first = true;
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
                    if (processingDuringCaptureDevice)
                        frame = FrameProcessing(frame);
                    Show(frame);
                    if (videoWriter != null)
                        videoWriter.Write(frame);
                }
            }
        }

        Mat FrameProcessing(Mat frame)
        {
            cannymat = new Mat();
            Cv2.CvtColor(frame, cannymat, ColorConversionCodes.RGB2GRAY);
            Cv2.Canny(cannymat, cannymat, 50, 200);

            bgr[0] = cannymat;
            bgr[1] = new Mat(frame.Size(), MatType.CV_8UC1);
            bgr[2] = new Mat(frame.Size(), MatType.CV_8UC1);

            Mat newmat = new Mat();
            Cv2.Merge(bgr, newmat);
            return newmat;
        }
        #endregion

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
                //Bitmap _image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
                ////this.Dispatcher.Invoke(() => { image.Source = conversion.ToImageSource(_image); });//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                ////_imageSource = conversion.ToImageSource(_image);
                //_image.Dispose();
                if (!frame.Empty())
                    _imageSource = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
            }

            DisplayFPS();
        }

        void DisplayFPS()
        {
            long T = chrono.ElapsedMilliseconds;
            float f = 1000f / (T - T0);
            T0 = T;
            //_title += "[" + f.ToString("N1") + " fps]";
        }

        void Init()
        {
            bgr = new Mat[] { new Mat(frame.Size(), MatType.CV_8UC1),
                              new Mat(frame.Size(), MatType.CV_8UC1),
                              new Mat(frame.Size(), MatType.CV_8UC1)};
            if (display_in_CV_IHM)
                window = new OpenCvSharp.Window("dst image", frame);
        }


        #region OCR
        private void Button_RUNOCR_Click(object sender, RoutedEventArgs e)
        {
            // load video
            vc = new VideoCapture(_video_path_in);

            //graph_s2.AxisX[0].MaxValue = (double)vc.FrameCount / vc.Fps;

            // Frame image buffer
            frame = new Mat();

            thread = new Thread(new ThreadStart(OCR));
            thread.Start();
        }

        private void OCR()
        {
            vc = new VideoCapture(_video_path_in);

            // Frame image buffer
            frame = new Mat();

            // When the movie playback reaches end, Mat.data becomes NULL.
            int i = 0;
            double T;
            isRunning = true;
            using (TesseractEngine TE = new TesseractEngine(@"D:\C#\advise-ihm\tessdata", "eng", EngineMode.Default))
            {

                while (isRunning)
                {
                    #region lit image n°i => T
                    vc.Read(frame);
                    i++;
                    T = (double)i / vc.Fps;
                    TLastFrame = DateTime.Now;
                    #endregion

                    #region fin ?
                    if (
                        //i > 100 ||
                        frame.Empty())
                        break;
                    #endregion

                    #region Init image & visu
                    if (first)
                    {
                        Init();
                        first = false;
                    }
                    #endregion


                    //visu
                    Show(frame);

                    frame.SaveImage(@"D:\test.png");
                    string text = "";


                    //mettre text en blanc sur fond noir
                    try
                    {
                        using (Pix Image = Pix.LoadFromFile(@"D:\test.png"))
                        {
                            text = TE.Process(Image).GetText();
                            Console.WriteLine("=================================================");
                            Console.WriteLine(text);
                            Console.WriteLine("=================================================");
                        }

                    }
                    catch (Exception ex)
                    {

                    }


                    //Title
                    _title = (100.0 * i / vc.FrameCount).ToString("N1") + "% " + text;

                    int t_deja_attendu_ms = (int)DateTime.Now.Subtract(TLastFrame).TotalMilliseconds;
                    int t_restant_ms = (int)(1000 / vc.Fps) - t_deja_attendu_ms;
                    if (t_restant_ms > 0)
                        Thread.Sleep(t_restant_ms);


                }
            }
        }
        #endregion


        #region Capture Device
        private void Button_ListDevices_Click(object sender, RoutedEventArgs e)
        {
            ListDevices();
        }

        private void ListDevices()
        {
            var devices = VideoInInfo.EnumerateVideoDevices_JJ();
            if (cbx_device != null)
                cbx_device.ItemsSource = devices.Select(d => d.Name).ToList();
        }

        private void Button_CaptureDevice_Click(object sender, RoutedEventArgs e)
        {
            thread_graph?.Abort();

            chrono.Start();
            isRunning = !isRunning;

            if (isRunning)
            {
                //string name = cbx_device.Text;
                indexDevice = cbx_device.SelectedIndex;
                processingDuringCaptureDevice = false;
                CaptureCamera(indexDevice);
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
        VideoWriter videoWriter;
        private void Button_CaptureDeviceRECORD_Click(object sender, RoutedEventArgs e)
        {
            if (videoWriter == null)
            {
                videoWriter = new VideoWriter("D:\\video.avi", FourCC.XVID, 30, new OpenCvSharp.Size(1920, 1080));
                //                videoWriter.
            }
            else
            {
                videoWriter.Release();
                videoWriter = null;
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
                grd_visu.Height = new GridLength(0);
                grd_tabs.Height = new GridLength(0);
                WindowStyle = WindowStyle.None;
            }
            else
            {
                grd_visu.Height = new GridLength(1, GridUnitType.Auto);
                grd_tabs.Height = new GridLength(1, GridUnitType.Auto);
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        private void img_mousedown(object sender, MouseButtonEventArgs e)
        {
            if (ctxm_hideothers.IsChecked)
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
        }

        private void ctxm_quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tbc_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                TabItem ti = (TabItem)e.AddedItems[0];
                if (ti == tbi_videocapture)
                {
                    ListDevices();
                }
            }
        }

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

        //public string OCRTiff(string path)
        //{
        //    String PageText; String Result = String.Empty;
        //    try
        //    {
        //        using (TesseractEngine TE = new TesseractEngine(@"D:\C#\advise-ihm\tessdata", "eng", EngineMode.Default))
        //        {
        //            using (Pix Image = Pix.LoadFromFile(TiffPath))
        //            {
        //                PageText = TE.Process(Image).GetText();
        //            }
        //        }
        //        String TextFilePath = string.Format("{0}{1}", TiffPath.Substring(0, TiffPath.Length - 4), ".txt");

        //        File.WriteAllText(TextFilePath, PageText);

        //        Result = "Sucess!";

        //    }
        //    catch (Exception Exception)
        //    {
        //        Result = String.Format("Error: {0}", Exception.Message);
        //    }

        //    return Result;
        //}

    }
}