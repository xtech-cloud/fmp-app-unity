using Newtonsoft.Json;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LauncherBehaviour : MonoBehaviour
{
    public TextAsset businessBranch;

    [DllImport("__Internal")]
    private static extern string QueryVendor();


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


        if (RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            yield return launcherWebGL();
        }
        else
        {
            yield return launcherStandard();
        }
    }

    IEnumerator launcherWebGL()
    {
        Storage.mode = Storage.Mode.Browser;
        string vendorUuid = QueryVendor();
        UnityLogger.Singleton.Info("vendor is {0}", vendorUuid);
        if(string.IsNullOrEmpty(vendorUuid))
        {
            yield return launcherStandard();
            yield break;
        }

        Storage.mode = Storage.Mode.Browser;
        BusinessBranch.Security.RewriteStorageAddress("http://minio.xtech.cloud");
        yield return VendorManager.Singleton.Activate(vendorUuid);
        SceneManager.LoadScene("selector");
    }

    IEnumerator launcherStandard()
    {
        // 加载配置文件
        yield return AppConfig.Singleton.Load();

        // 解析参数
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        foreach (string arg in commandLineArgs)
        {
            if (arg.StartsWith("-vendor="))
            {
                VendorManager.Singleton.Activate(arg.Replace("-vendor=", "").Trim());
            }
        }

        // 参数没有指定vendor时，使用配置文件中激活的虚拟环境
        if (string.IsNullOrWhiteSpace(VendorManager.Singleton.activeUuid))
        {
            yield return VendorManager.Singleton.Activate(AppConfig.Singleton.body.vendorSelector.active);
        }

        SceneManager.LoadScene("selector");
    }
}
