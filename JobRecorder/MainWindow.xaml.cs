using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.ComponentModel;

namespace JobRecorder
{


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ///USB消息提醒常量
        public const int WM_DEVICECHANGE = 0x219;//设备改变
        public const int DBT_DEVICEARRIVAL = 0x8000;//设备接入
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;//设备拔出

        /// <summary>
        /// 带指定文件夹目录的可移动磁盘信息集合
        /// </summary>
        private List<USBDevice> DriveList { get; set; }

        /// <summary>
        /// 待处理磁盘信息队列
        /// </summary>
        private Queue<USBDevice> CopyQueue { get; set; }

        //队列是否正在处理数据
        private int isProcessing;
        //有线程正在处理数据
        private const int Processing = 1;
        //没有线程处理数据
        private const int UnProcessing = 0;

        /// <summary>
        /// 默认设备存储视频目录
        /// </summary>
        public const string devicePath = "FILE\\100CHINA";
        /// <summary>
        /// 默认用于存储视频的电脑目录
        /// </summary>
        public const string storePath = "D:\\调车视频";

        /// <summary>
        /// 背景进程，用于异步执行复制，防止卡顿（调用时注意把UI更新放在另一个进程中）
        /// </summary>
        BackgroundWorker backgroundWorker = new BackgroundWorker() { WorkerReportsProgress = true };

        public MainWindow()
        {
            InitializeComponent();
            DriveList = new List<USBDevice>();
            CopyQueue = new Queue<USBDevice>();

            backgroundWorker.DoWork += new DoWorkEventHandler(DoTask);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Task_Completed);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            AddUSBDevices();
            if (isProcessing == UnProcessing)
            {
                Start();
            }
        }

        /// <summary>
        /// 开始执行队列复制
        /// </summary>
        public void Start()
        {
            if (CopyQueue.Count != 0)
            {
                if (Directory.Exists(CopyQueue.Peek().Name + devicePath))
                {
                    backgroundWorker.RunWorkerAsync();
                }
                else
                {
                    CopyQueue.Dequeue();
                }

            }
        }

        /// <summary>
        /// 执行任务事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoTask(object sender, DoWorkEventArgs e)
        {
            isProcessing = Processing;

            USBDevice drive = CopyQueue.Peek();
            try
            {
                string[] files = Directory.GetFiles(drive.Name + MainWindow.devicePath, "YC?????_*.MP4", SearchOption.TopDirectoryOnly);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    drive.Button.IsEnabled = false;
                    if (files.Length == 0)
                    {
                        drive.ComState.ProgressMax();
                        drive.ComState.StateNothing();
                    }
                    else
                    {
                        drive.ComState.StateCoping();
                        drive.ComState.SetMaxProgress(files.Length);
                        drive.ComState.ProgressReSet();
                    }
                }));
                if (files.Length > 0)
                {
                    DoCopy(files);
                }
            }
            catch (Exception ex)
            {
                //主要是U盘意外插拔错误
                MessageBox.Show(ex.Message);
                return;
            }

        }

        /// <summary>
        /// 更新进度事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                CopyQueue.Peek().ComState.ProgressInc();
            }));
        }

        /// <summary>
        /// 任务完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Task_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                USBDevice device = CopyQueue.Dequeue();
                device.Button.IsEnabled = true;
                if (device.ComState.State != "Nothing")
                {
                    device.ComState.StateCompete();
                }
                else
                {
                    device.ComState.StateNothing();
                }
            }));
            if (CopyQueue.Count != 0)
            {
                Start();
            }
            else
            {
                isProcessing = UnProcessing;
            }

        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="files">文件路径数组</param>
        public void DoCopy(string[] files)
        {
            //复制操作
            for (int i = 0; i < files.Length; i++)
            {
                string fileStorePath = GetFileStorePath(files[i]);
                if (!File.Exists(fileStorePath))
                {
                    Directory.CreateDirectory(fileStorePath);
                }

                string filename = System.IO.Path.GetFileName(files[i]);
                if (!File.Exists(fileStorePath + "\\" + filename))
                {
                    try
                    {
                        File.Copy(files[i], fileStorePath + "\\" + filename, false);
                        backgroundWorker.ReportProgress(i);
                        //System.Threading.Thread.Sleep(5000);
                        File.Delete(files[i]);
                    }
                    catch (Exception ex)
                    {
                        //主要是U盘意外插拔错误
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }
                else
                {
                    File.Delete(files[i]);
                }
            }
        }

        /// <summary>
        /// 获取某文件在电脑硬盘上的存储目录
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFileStorePath(string fileName)
        {
            string path = "";
            string dataTimeString = System.IO.Path.GetFileName(fileName).Substring(14, 10);
            DateTime date = new DateTime(Convert.ToInt32(dataTimeString.Substring(0, 4)), Convert.ToInt32(dataTimeString.Substring(4, 2)),
                Convert.ToInt32(dataTimeString.Substring(6, 2)), Convert.ToInt32(dataTimeString.Substring(8, 2)), 0, 0);
            if (date.Hour < 8)
            {
                date = date - new TimeSpan(1, 0, 0, 0, 0);
            }
            string num = "";
            switch (System.IO.Path.GetFileName(fileName).Substring(6, 1))
            {
                case "1":
                    num = "1.干部";
                    break;
                case "2":
                    num = "2.调车长";
                    break;
                case "4":
                    num = "4.连结员";
                    break;
                default:
                    num = "编号错误";
                    break;
            }
            path = string.Format("{0}\\{1}年\\{2}月\\{3}\\{4}日", storePath, date.Year, date.Month, num, date.Day);

            return path;
        }

        /// <summary>
        /// 增加设备，用于在设备插入后将其添加到列表和处理队列中
        /// </summary>
        private void AddUSBDevices()
        {
            var drives = DriveInfo.GetDrives();
            string deviceNum = "";
            foreach (var drive in drives)
            {
                if (drive.IsReady && drive.DriveType == DriveType.Removable
                    && !DriveList.Exists(x => x.Name == drive.Name) && Directory.Exists(drive.Name + MainWindow.devicePath))
                {
                    try
                    {
                        deviceNum = drive.Name;
                        if (File.Exists(drive.Name + "Config.ini"))
                        {
                            string[] deviceInfo = File.ReadAllLines(drive.Name + "Config.ini",Encoding.Default);
                            if (deviceInfo.Length > 1 && deviceInfo[0] == "调车")
                            {
                                deviceNum = deviceInfo[1] + "号";
                            }
                        }
                        string[] files = Directory.GetFiles(drive.Name + MainWindow.devicePath, "YC?????_*.MP4", SearchOption.TopDirectoryOnly);
                        if (files.Length != 0)
                        {
                            deviceNum = System.IO.Path.GetFileName(files[files.GetUpperBound(0)]).Substring(6, 1) + "号";
                        }
                    }
                    catch
                    {
                        MessageBox.Show("部分U盘设备未连接好，请重新连接");
                    }

                    AddControl(deviceNum, out ComState comState, out Button button);
                    USBDevice usbDevice = new USBDevice(drive.Name, comState, button, drive.DriveType);
                    DriveList.Add(usbDevice);
                    CopyQueue.Enqueue(usbDevice);
                }
            }
            if (DriveList.Count == 0)
            {
                NoDriveState.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 在主界面上添加控件
        /// </summary>
        /// <param name="num"></param>
        /// <param name="comState"></param>
        /// <param name="button"></param>
        private void AddControl(string num, out ComState comState, out Button button)
        {
            button = new Button()
            {
                Margin = new Thickness(10),
                Height = 30,
                Content = string.Format("{0}已连接(点击移除)", num)
            };

            button.Click += DoUSBout; ;
            leftPanel.Children.Add(button);

            comState = new ComState()
            {
                Margin = new Thickness(3)
            };
            comState.StateWating();
            comState.SetNum(num);
            rightPanel.Children.Add(comState);
        }

        /// <summary>
        /// 绑定消息接收事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null) { source.AddHook(WndProc); }
        }

        /// <summary>
        /// 接收消息事件处理，用于处理收到信息，找出U盘插拔事件
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_DEVICECHANGE)
                {
                    switch (wParam.ToInt32())
                    {
                        case DBT_DEVICEARRIVAL://U盘插入
                            //写入队列和设备列表；
                            AddUSBDevices();
                            if (isProcessing == UnProcessing)
                            {
                                Start();
                            }
                            NoDriveState.Visibility = Visibility.Collapsed;
                            break;
                        case DBT_DEVICEREMOVECOMPLETE: //U盘拔出
                            //删除队列和列表，删除控件；
                            ClearDriveList();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 弹出U盘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoUSBout(object sender, RoutedEventArgs e)
        {
            var sourceButton = sender as Button;
            string deviceName = DriveList.Find(x => x.Button.Equals(sourceButton)).Name;
            uint removeState = USBDevice.RemoveDevice(deviceName);
            if (removeState == 0)
            {
                //MessageBox.Show(string.Format("可移动设备{0}已移除", deviceName));
                ClearDriveList();
            }
            else
            {
                MessageBox.Show(string.Format("可移动设备{0}移除失败代码：{1}", deviceName, removeState.ToString()));
            }
        }

        /// <summary>
        /// 在U盘移除后清理可移动磁盘列表和控件
        /// </summary>
        private void ClearDriveList()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            //清除在DriveList中但已被移除的设备，通常是直接拔出U盘
            foreach (USBDevice usbDrive in DriveList)
            {
                if (!Array.Exists(drives, x => x.Name == usbDrive.Name))
                {
                    leftPanel.Children.Remove(usbDrive.Button);
                    rightPanel.Children.Remove(usbDrive.ComState);
                    DriveList.Remove(usbDrive);
                    break;
                }
            }
            //清除已移除但仍在DriveList中并未清除控件的设备,通常是使用界面按钮移除的设备
            foreach (DriveInfo drive in drives)
            {
                if (!drive.IsReady)
                {
                    USBDevice usbDrive = DriveList.Find(x => x.Name == drive.Name);
                    if (usbDrive != null)
                    {
                        leftPanel.Children.Remove(usbDrive.Button);
                        rightPanel.Children.Remove(usbDrive.ComState);
                        DriveList.Remove(usbDrive);
                    }
                }
            }
            if (DriveList.Count == 0)
            {
                NoDriveState.Visibility = Visibility.Visible;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing == UnProcessing)
            {
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show("正在复制文件，请稍后！", "文件复制中…");
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Show();
        }
    }
}
