using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;
//using Timers = System.Timers.Timer;

namespace DetectBootDisk
{
    public partial class DetectBootDisk : ServiceBase
    {
        public DetectBootDisk()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 序列号
        /// </summary>
        private string SerialNumber = String.Empty;
        /// <summary>
        /// 注册表子文件夹名称
        /// </summary>
        private string folder = "USBSerialNumber";
        /// <summary>
        /// 注册表项名称
        /// </summary>
        private string name = String.Empty;

        public enum TimeType
        {
            /// <summary>
            /// 秒
            /// </summary>
            Second = 1000,
            /// <summary>
            /// 分
            /// </summary>
            Minute = Second * 60,
            /// <summary>
            /// 小时
            /// </summary>
            Hour = Minute * 60,
            /// <summary>
            /// 天
            /// </summary>
            Day = Hour * 24
        };

        #region 获取可移动磁盘的SerialNumber
        /// <summary>
        /// 获取移动磁盘的SerialNumber
        /// </summary>
        private string GetUSBSerialNumber()
        {
            /// <summary>
            /// 产品名称
            /// </summary>
            string Caption = String.Empty;
            /// <summary>
            /// 总容量
            /// </summary>
            string Size = String.Empty;
            /// <summary>
            /// 序列号
            /// </summary>
            string PNPDeviceID = String.Empty;
            /// <summary>
            /// 版本号
            /// </summary>
            string REV = String.Empty;
            /// <summary>
            /// 制造商ID
            /// </summary>
            string VID = String.Empty;

            using (ManagementClass cimobject = new ManagementClass("Win32_DiskDrive"))
            {
                ManagementObjectCollection moc = cimobject.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if (mo.Properties["InterfaceType"].Value.ToString() == "USB")
                    {
                        //产品名称
                        Caption = mo.Properties["Caption"].Value.ToString();
                        //Console.WriteLine("产品名称：" + Caption);
                        //总容量
                        Size = mo.Properties["Size"].Value.ToString();
                        long size = Convert.ToInt64(Size);
                        //Console.WriteLine("总容量：" + size / 1024 / 1024 / 1024 + "GB");

                        string[] info = mo.Properties["PNPDeviceID"].Value.ToString().Split('&');
                        string[] xx = info[3].Split('\\');
                        //序列号
                        PNPDeviceID = xx[1];
                        //Console.WriteLine("序列号：" + PNPDeviceID);

                        xx = xx[0].Split('_');
                        //版本号
                        REV = xx[1];
                        //Console.WriteLine("版本号：" + REV);
                        //制造商ID
                        xx = info[1].Split('_');
                        VID = xx[1];
                        //Console.WriteLine("制造商ID：" + VID + "\n");
                        File.WriteAllText("D:\\" + Caption, PNPDeviceID);
                        return PNPDeviceID;
                    }
                }
            }
            return String.Empty;
        }
        #endregion

        #region 写入注册表
        /// <summary>
        /// 写入注册表
        /// </summary>
        /// <param name="tovalue">值</param>
        private void SetRegistData(string tovalue)
        {
            RegistryKey hKey = Registry.LocalMachine;
            RegistryKey software = hKey.OpenSubKey("SOFTWARE", true);
            RegistryKey aimdir = software.CreateSubKey(folder);
            aimdir.SetValue(name, tovalue);
        }
        #endregion

        #region 读取注册表
        /// <summary>
        /// 读取注册表的某个值
        /// </summary>
        /// <returns></returns>
        private string GetRegistData()
        {
            RegistryKey hKey = Registry.LocalMachine;
            RegistryKey software = hKey.OpenSubKey("SOFTWARE", true);
            RegistryKey aimdir = software.OpenSubKey(folder, true);
            if (aimdir != null)
            {
                return aimdir.GetValue(name).ToString();
            }
            return String.Empty;
        }
        #endregion
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WTSGetActiveConsoleSessionId();
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSSendMessage(IntPtr hServer,
            int SessionId,
            String pTitle, int TitleLength,
            String pMessage, int MessageLength,
            int Style, int Timeout, out int pResponse, bool bWait = false);

        public static void ShowMessageBox(string title, string message)
        {
            int resp = 0;
            WTSSendMessage(IntPtr.Zero,
                WTSGetActiveConsoleSessionId(),
                title, title.Length,
                message, message.Length,
                0, 0, out resp, false);
        }
        private void MTimedEvent(object source, ElapsedEventArgs e)
        {
            SerialNumber = GetUSBSerialNumber();
            string RegistData = GetRegistData();
            //File.WriteAllText("D:\\" + RegistData, "RegistData:"+ RegistData);
            if (String.IsNullOrEmpty(SerialNumber) && String.IsNullOrEmpty(RegistData))
            {
                ShowMessageBox("提示", "没有检测到U盘，请插入U盘！");
                return;
            }
            if (String.IsNullOrEmpty(RegistData))
            {
                SetRegistData(SerialNumber);
                RegistData = SerialNumber;
            }
            if (!SerialNumber.Equals(RegistData))
            {
                Process.Start("shutdown", "-f -p");
                //File.AppendAllText("D:\\关机.txt", "SerialNumber:" + SerialNumber + "\nRegistData:" + RegistData + "\n");
            }
        }
        protected override void OnStart(string[] args)
        {
            MTimedEvent(null, null);
            //定时器间隔时间30秒
            Timer tm = new Timer((int)TimeType.Second * 30);
            tm.Elapsed += new ElapsedEventHandler(MTimedEvent);
            tm.Start();
            //判断序列号是否为空
            if (!String.IsNullOrEmpty(SerialNumber))
            {
                //不为空，停止定时器
                tm.Stop();
            }
        }

        protected override void OnStop()
        {
        }
    }
}
