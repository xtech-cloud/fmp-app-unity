using UnityEngine;
using System.Text;
using System.Security.Cryptography;

public class Constant
{
    public static string Platform
    {
        get
        {
            if (RuntimePlatform.WindowsPlayer == Application.platform ||
                RuntimePlatform.WindowsEditor == Application.platform)
                return "windows";
            if (RuntimePlatform.Android == Application.platform)
                return "android";
            return "";
        }
    }

    public static string DeviceCode
    {
        get
        {
            if (string.IsNullOrEmpty(devicecode_))
            {

                StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(SystemInfo.deviceModel + "\n")
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

                UnityLogger.Singleton.Info(sb.ToString());
                MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
                byte[] bytes = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                StringBuilder tmp = new StringBuilder();
                foreach (byte i in bytes)
                {
                    tmp.Append(i.ToString("x2"));
                }
                devicecode_ = tmp.ToString().ToUpper();
                //return SystemInfo.deviceUniqueIdentifier;
            }
            return devicecode_;
        }
    }

    private static string devicecode_ = "";
}
