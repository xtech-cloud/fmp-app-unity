using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LauncherBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter Launcher Scene");

        // 加载配置文件
        AppConfig.Singleton.Load();

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
    }

    void Start()
    {
        SceneManager.LoadScene("selector");
    }
}
