using System;
using System.IO;

namespace BusinessBranch
{
    public static class Build
    {
        public static string version { get; private set; }

        public static void RewriteVersion(string _value)
        {
            version = _value;
        }
    }

    public static class Security
    {
        public static string AppKey { get; private set; } = "";
        public static string AppSecret { get; private set; } = "";


        public static string StorageAddress { get; private set; }
        public static string StorageVendorRootDir { get; private set; }
        public static string StorageAssloudRootDir { get; private set; }

        public static void RewriteAppKey(string _appKey)
        {
            AppKey = _appKey;
        }

        public static void RewriteAppSecret(string _appSecret)
        {
            AppSecret = _appSecret;
        }

        public static void RewriteStorageAddress(string _value)
        {
            StorageAddress = _value;
        }

        public static void RewriteVendorRootDir(string _value)
        {
            StorageVendorRootDir = _value;
        }

        public static void RewriteAssloudRootDir(string _value)
        {
            StorageAssloudRootDir = _value;
        }
    }

    /// <summary>
    /// 配置文件结构，主要用于在BusinessBranch被编译成dll的时候，以外部文件形式
    /// </summary>
    public class Schema
    {
        public string AppKey { get; set; } = "";
        public string AppSecret { get; set; } = "";
        public string StorageAddress { get; private set; } = "http://localhost:9000";
        public string StorageVendorRootDir { get; private set; } = "fmp.vendor/unity";
        public string StorageAssloudRootDir { get; private set; } = "fmp.assloud";
        public string BuildVersion { get; private set; } = "b1";
    }
}
