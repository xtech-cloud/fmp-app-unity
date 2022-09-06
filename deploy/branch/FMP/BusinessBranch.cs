namespace BusinessBranch
{
    public static class Security
    {
        public static string AppKey { get; private set; } = "";
        public static string AppSecret { get; private set; } = "";

        public static void RewriteAppKey(string _appKey)
        {
            AppKey = _appKey;
        }

        public static void RewriteAppSecret(string _appSecret)
        {
            AppSecret = _appSecret;
        }
    }

    public class Schema
    {
        public string AppKey { get; set; } = "";
        public string AppSecret { get; set; } = "";
    }
}
