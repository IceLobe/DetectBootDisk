using System;
using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace DetectBootDisk
{
    static class Program
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        static string serviceName = "Detect_Boot_Disk";

        #region 判断服务是否存在
        /// <summary>
        /// 判断服务是否存在
        /// </summary>
        /// <returns></returns>
        private static bool IsServiceExisted()
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController sc in services)
            {
                if (sc.ServiceName.ToLower() == serviceName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 安装Windows服务
        /// <summary>
        /// 安装服务
        /// </summary>
        private static void InstallService(string BinPath)
        {
            using (AssemblyInstaller installer = new AssemblyInstaller())
            {
                installer.UseNewContext = true;
                installer.Path = BinPath;
                IDictionary savedState = new Hashtable();
                installer.Install(savedState);
                installer.Commit(savedState);
            }
        }
        #endregion

        #region 应用程序的主入口点
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        static void Main()
        {
            //要复制的文件
            string sourceFileName = Process.GetCurrentProcess().MainModule.FileName;
            //得到要复制的文件的名称
            string fileName = sourceFileName.Substring(sourceFileName.LastIndexOf("\\"));
            //目标文件的名称
            string destFileName = Environment.GetFolderPath(Environment.SpecialFolder.System) + fileName;
            //判断目标文件是否存在
            if (!File.Exists(destFileName))
            {
                //复制文件
                File.Copy(sourceFileName, destFileName, true);
            }
            //判断服务是否存在
            if (!IsServiceExisted())
            {
                InstallService(destFileName);
            }

            //启动服务
            using (ServiceBase ServicesToRun = new DetectBootDisk())
            {
                ServiceBase.Run(ServicesToRun);
            }
        }
        #endregion

    }
}
