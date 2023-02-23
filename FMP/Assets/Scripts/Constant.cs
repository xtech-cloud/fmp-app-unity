using UnityEngine;
using System.Text;
using System.Security.Cryptography;

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
            if (0 == AppConfig.Singleton.body.security.dcgen)
                return SystemInfo.deviceUniqueIdentifier;

            if (string.IsNullOrEmpty(devicecode_))
            {
                StringBuilder sb = new StringBuilder();
                
                sb.AppendLine(Application.companyName)
                    .AppendLine(Application.productName)
                    .AppendLine(Application.platform.ToString())
                    .AppendLine(SystemInfo.deviceModel)
                    .AppendLine(SystemInfo.deviceName)
                    .AppendLine(SystemInfo.deviceType.ToString())
                    .AppendLine(SystemInfo.graphicsDeviceID.ToString())
                    .AppendLine(SystemInfo.graphicsDeviceName)
                    .AppendLine(SystemInfo.graphicsDeviceType.ToString())
                    .AppendLine(SystemInfo.graphicsDeviceVendor)
                    .AppendLine(SystemInfo.graphicsDeviceVendorID.ToString())
                    .AppendLine(SystemInfo.graphicsDeviceVersion)
                    .AppendLine(SystemInfo.processorCount.ToString())
                    .AppendLine(SystemInfo.processorType);

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

                // ! 不采用的方法
                // System.Management 只能在Windows Desktop Application中使用;

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
