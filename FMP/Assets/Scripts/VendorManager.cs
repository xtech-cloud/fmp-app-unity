using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VendorManager
{
    public static VendorManager Singleton
    {
        get
        {
            if (null == singleton_)
                singleton_ = new VendorManager();
            return singleton_;
        }
    }

    public string active { get; set; } = "";

    private static VendorManager singleton_;

    private Upgrade upgrade;

    public IEnumerator RunUpgrade()
    {
        OnlineConfig onlineConfig = new OnlineConfig();
        yield return onlineConfig.Download();

        // 检查更新
        string upgradeFile = Path.Combine(Application.streamingAssetsPath, "upgrade.json");
        if (!File.Exists(upgradeFile))
        {
            SceneManager.LoadScene("startup");
            yield break;
        }

        upgrade = new Upgrade();
        //upgrade.mono = this;
        //upgrade.canvas = canvas;
        //upgrade.vendor = vendor;
        upgrade.onFinish = () =>
        {
            SceneManager.LoadScene("startup");
        };
        string json = File.ReadAllText(upgradeFile);
        upgrade.ParseConfig(json);
        upgrade.Run();
    }
}
