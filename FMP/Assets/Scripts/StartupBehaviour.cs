using System.IO;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MVCS = XTC.FMP.LIB.MVCS;

public class StartupBehaviour : MonoBehaviour
{
    public class MVCSConfig : MVCS.Config
    {
        public MVCSConfig(Dictionary<string, string> _configs)
        {
            foreach (var pair in _configs)
            {

                fields_[pair.Key] = MVCS.Any.FromString(pair.Value);
            }
        }
    }


    public Transform mainCanvas;
    public GameObject bootloader;
    public Text textBootloaderTip;
    public Text textBootloaderUpgress;
    public Font mainFont;

    private MVCS.Framework framework;
    private ModuleManager moduleManager;
    private AppConfig.Vendor activeVendor_;

    IEnumerator Start()
    {
        UnityLogger.Singleton.Info("########### Enter Startup Scene");

        UnityLogger.Singleton.Info("---------------  Start ------------------------");

        bootloader.SetActive(true);
        textBootloaderTip.text = "";
        textBootloaderUpgress.text = "";

        foreach (var vendor in AppConfig.Singleton.body.vendorSelector.vendors)
        {
            if (vendor.scope.Equals(VendorManager.Singleton.active))
            {
                activeVendor_ = vendor;
                break;
            }
        }

        var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(
            activeVendor_.graphics.referenceResolution.width,
            activeVendor_.graphics.referenceResolution.height
        );
        canvasScaler.matchWidthOrHeight = activeVendor_.graphics.referenceResolution.match;

        // 加载模块
        moduleManager = new ModuleManager();
        yield return moduleManager.Load();
        if(!moduleManager.success)
            yield break;

        // 初始化MVCS框架
        MVCS.Config config = new MVCSConfig(moduleManager.configs);
        UnityLogger uniLogger = new UnityLogger();
        MVCS.Logger logger = uniLogger;
        logger.setLevel((MVCS.LogLevel)AppConfig.Singleton.body.logger.level);
        framework = new MVCS.Framework();
        framework.setLogger(logger);
        framework.setConfig(config);
        framework.Initialize();

        // 加载模块
        Dictionary<string, MVCS.Any> settings = new Dictionary<string, MVCS.Any>();
        moduleManager.OnTipChanged = (_tip) => textBootloaderTip.text = _tip;
        moduleManager.OnUpgressChanged = (_percentage) => textBootloaderUpgress.text = _percentage.ToString();
        moduleManager.OnBootFinish = () => bootloader.SetActive(false);
        settings["vendor"] = MVCS.Any.FromString(activeVendor_.scope);
        settings["datapath"] = MVCS.Any.FromString(Storage.ScopePath);
        settings["devicecode"] = MVCS.Any.FromString(Constant.DeviceCode);
        settings["platform"] = MVCS.Any.FromString(Constant.Platform);
        settings["main.canvas"] = MVCS.Any.FromObject(mainCanvas);
        settings["main.font"] = MVCS.Any.FromObject(mainFont);
        // 注册模块中的MVCS
        moduleManager.Inject(this, framework, logger, config, settings);
        moduleManager.Register();

        // 装载已注册的部件
        framework.Setup();

        yield return new WaitForEndOfFrame();
        UnityLogger.Singleton.Info("++++++++++++++++++++++++++++++++++++++++++++++++");
        UnityLogger.Singleton.Info("+ Preload Modules                              +");
        UnityLogger.Singleton.Info("++++++++++++++++++++++++++++++++++++++++++++++++");
        moduleManager.Preload();
    }

    void Update()
    {
    }

    void OnDestroy()
    {
        UnityLogger.Singleton.Info("---------------  OnDestroy ------------------------");

        framework.Dismantle();
        // 拆卸模块中的MVCS
        moduleManager.Cancel();
        framework.Release();
        framework = null;

        moduleManager.Unload();
    }
}
