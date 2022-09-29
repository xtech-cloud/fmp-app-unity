using System.IO;

namespace BusinessBranch
{
    public static class Security
    {
        public static string AppKey { get; private set; } = "";
        public static string AppSecret { get; private set; } = "";

        public static string StorageAddress { get; private set; } = "http://localhost:9000";
        public static string StorageVendorRootDir { get; private set; } = "fmp.vendor/unity";

        public static void RewriteAppKey(string _appKey)
        {
            AppKey = _appKey;
        }

        public static void RewriteAppSecret(string _appSecret)
        {
            AppSecret = _appSecret;
        }

        public static void RewriteStorageAddress(string _storageAddress)
        {
            StorageAddress = _storageAddress;
        }

        public static void RewriteVendorRootDir(string _dir)
        {
            StorageVendorRootDir = _dir;
        }
    }

    public class Schema
    {
        public string AppKey { get; set; } = "";
        public string AppSecret { get; set; } = "";
    }
}
