using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace VideoCapture
{
    public partial class CPU_Memory : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public CPU_Memory()
        {
            InitializeComponent();
            DataContext = this;
            _InfoProcess_INIT();
        }

        System.Diagnostics.PerformanceCounter cpuCounterAll;
        System.Diagnostics.PerformanceCounter cpuCounterIHM;
        //PerformanceCounter ramCounter;

        TimeSpan _prevCPUUseTime;
        int cpuUseTime_period = 1000;

        public void _InfoProcess_INIT()
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
    }
}
