using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;

namespace JobRecorder
{
    class USBDevice
    {
        public String Name { get; set; }

        public ComState ComState { get; set; }

        public DriveType DriveType { get; set; }

        public Button Button { get; set; }

        public USBDevice(string name, ComState comState, Button button, DriveType driveType)
        {
            Name = name;
            ComState = comState;
            Button = button;
            DriveType = driveType;
        }


        /// <summary>
        /// 移除指定盘符的USB设备，待修改判断是否正在占用
        /// </summary>
        /// <param name="deviceName">盘符名或设备名</param>
        /// <returns></returns>
        public static uint RemoveDevice(string deviceName)
        {
            string filename = @"\\.\" + deviceName.TrimEnd('\\');
            IntPtr handle = CreateFile(filename, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, 0x3, 0, IntPtr.Zero);

            DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out uint byteReturned, IntPtr.Zero);

            return byteReturned;
        }

        /// <summary>
        /// 打开设备函数用于获得handle
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="dwShareMode"></param>
        /// <param name="SecurityAttributes"></param>
        /// <param name="dwCreationDisposition"></param>
        /// <param name="dwFlagsAndAttributes"></param>
        /// <param name="hTemplateFile"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr SecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

        /// <summary>
        /// 向目标设备发送命令，用于弹出U盘
        /// </summary>
        /// <param name="hDevice"></param>
        /// <param name="dwIoControlCode"></param>
        /// <param name="lpInBuffer"></param>
        /// <param name="nInBufferSize"></param>
        /// <param name="lpOutBuffer"></param>
        /// <param name="nOutBufferSize"></param>
        /// <param name="lpBytesReturned"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
           IntPtr hDevice,
           uint dwIoControlCode,
           IntPtr lpInBuffer,
           uint nInBufferSize,
           IntPtr lpOutBuffer,
           uint nOutBufferSize,
           out uint lpBytesReturned,
           IntPtr lpOverlapped);

        private const uint GENERIC_READ = 0x80000000;
        private const int GENERIC_WRITE = 0x40000000;
        private const int FILE_SHARE_READ = 0x1;
        private const int FILE_SHARE_WRITE = 0x2;
        private const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
    }

}

