using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LauncherBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("########### Enter Launcher Scene");

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
        // 如果命令参数和配置文件均没有指定vendor，跳转到vendor选择场景
        if (string.IsNullOrEmpty(VendorManager.Singleton.active))
            SceneManager.LoadScene("selector");
        else
            SceneManager.LoadScene("splash");
    }
}
