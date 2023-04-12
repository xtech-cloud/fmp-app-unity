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

    public class Schema
    {
        public string AppKey { get; set; } = "";
        public string AppSecret { get; set; } = "";
        public string StorageAddress { get; set; } = "http://localhost:9000";
        public string StorageVendorRootDir { get; set; } = "fmp.vendor/unity";
        public string StorageAssloudRootDir { get; set; } = "fmp.assloud";
        public string BuildVersion { get; set; } = "b0";
    }
}
