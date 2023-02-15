using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.Management;

public class Constant
{
    public static string PlatformAlias
    {
        get
        {
            if (RuntimePlatform.WindowsPlayer == Platform ||
                RuntimePlatform.WindowsEditor == Platform)
                return "windows";
            if (RuntimePlatform.LinuxPlayer == Platform ||
                RuntimePlatform.LinuxEditor == Platform)
                return "linux";
            if (RuntimePlatform.OSXPlayer == Platform ||
                RuntimePlatform.OSXPlayer == Platform)
                return "osx";
            if (RuntimePlatform.Android == Platform)
                return "android";
            if (RuntimePlatform.IPhonePlayer == Platform)
                return "ios";
            if (RuntimePlatform.Android == Platform)
                return "android";
            if (RuntimePlatform.WebGLPlayer == Platform)
                return "webgl";
            return "";
        }
    }

    public static RuntimePlatform Platform
    {
        get
        {
            return Application.platform;
        }
    }

    public static string DeviceCode
    {
        get
        {
            if (RuntimePlatform.WindowsEditor == Application.platform)
                return SystemInfo.deviceUniqueIdentifier;

            if (string.IsNullOrEmpty(devicecode_))
            {
                StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(Application.productName)
                    .Append(Application.platform.ToString())
                    .Append(SystemInfo.deviceModel + "\n")
                    .Append(SystemInfo.deviceName + "\n")
                    .Append(SystemInfo.deviceType + "\n")
                    .Append(SystemInfo.graphicsDeviceID + "\n")
                    .Append(SystemInfo.graphicsDeviceName + "\n")
                    .Append(SystemInfo.graphicsDeviceType + "\n")
                    .Append(SystemInfo.graphicsDeviceVendor + "\n")
                    .Append(SystemInfo.graphicsDeviceVendorID + "\n")
                    .Append(SystemInfo.graphicsDeviceVersion + "\n")
                    .Append(SystemInfo.processorCount + "\n")
                    .Append(SystemInfo.processorType + "\n");

                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    sb.Append(mo.Properties["ProcessorId"].Value.ToString());
                }
                moc = null;
                mc = null;
                devicecode_ = sb.ToString();
                UnityLogger.Singleton.Info("********* device info  ************");
                UnityLogger.Singleton.Info(sb.ToString());
                UnityLogger.Singleton.Info("**********************************");


                UnityLogger.Singleton.Info(sb.ToString());
                MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
                byte[] bytes = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                StringBuilder tmp = new StringBuilder();
                foreach (byte i in bytes)
                {
                    tmp.Append(i.ToString("x2"));
                }
                devicecode_ = tmp.ToString().ToUpper();

                // !! 不采用的方法
                // 直接调用WMI, 部分Win10和Win11升级更新后，找不到WMI

                // !! 已弃用的方法
                // 系统更新或激活网卡变化后，设备码会变化
                // devicecode_ = SystemInfo.deviceUniqueIdentifier;
            }
            return devicecode_;
        }
    }

    private static string devicecode_ = "";
}
