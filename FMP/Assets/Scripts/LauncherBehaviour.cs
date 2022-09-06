using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bu
{
    public string AppKey { get; set; }
    public string AppSecret { get; set; }
}

public class LauncherBehaviour : MonoBehaviour
{
    public TextAsset businessBranch;

    IEnumerator Start()
    {
        UnityLogger.Singleton.Info("########### Enter Launcher Scene");

        if (null != businessBranch)
        {
            string text = businessBranch.text;
            var schema = JsonConvert.DeserializeObject<BusinessBranch.Schema>(text);
            BusinessBranch.Security.RewriteAppKey(schema.AppKey);
            BusinessBranch.Security.RewriteAppSecret(schema.AppSecret);
        }


        if (RuntimePlatform.WebGLPlayer == Application.platform)
        {
            Storage.mode = Storage.Mode.Browser;
        }

        // 加载配置文件
        yield return AppConfig.Singleton.Load();

        // 解析参数
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        foreach (string arg in commandLineArgs)
        {
            if (arg.StartsWith("-vendor="))
            {
                VendorManager.Singleton.active = arg.Replace("-vendor=", "").Trim();
            }
        }

        // 参数没有指定vendor时，使用配置文件中激活的虚拟环境
        if (string.IsNullOrWhiteSpace(VendorManager.Singleton.active))
        {
            VendorManager.Singleton.active = AppConfig.Singleton.body.vendorSelector.active;
        }

        SceneManager.LoadScene("selector");
    }
}
