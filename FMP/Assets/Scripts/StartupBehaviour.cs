using System.IO;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MVCS = XTC.FMP.LIB.MVCS;

public class StartupBehaviour : MonoBehaviour
{
    public Transform mainCanvas;
    public GameObject bootloader;
    public Text textBootloaderTip;
    public Text textBootloaderUpgress;
    public Font mainFont;

    private MVCS.Framework framework;
    private MVCS.Logger logger;
    private MVCS.Config config;
    private ModuleManager moduleManager;
    private AppConfig.Vendor activeVendor_;

    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("########### Enter Startup Scene");
        bootloader.SetActive(true);
        textBootloaderTip.text = "";
        textBootloaderUpgress.text = "";

        foreach (var vendor in AppConfig.Singleton.body.vendorSelector.vendors)
        {
            if (vendor.directory.Equals(AppConfig.Singleton.body.vendorSelector.active))
            {
                activeVendor_ = vendor;
                break;
            }
        }

        string datapath = Application.persistentDataPath;

        var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(
            activeVendor_.graphics.referenceResolution.width,
            activeVendor_.graphics.referenceResolution.height
        );
        canvasScaler.matchWidthOrHeight = activeVendor_.graphics.referenceResolution.match;


        // 初始化MVCS框架
        FileConfig fileConfig = new FileConfig();
        fileConfig.Load(datapath, activeVendor_.directory);
        UnityLogger uniLogger = new UnityLogger();
        logger = uniLogger;
        logger.setLevel((MVCS.LogLevel)AppConfig.Singleton.body.logger.level);
        config = fileConfig;
        framework = new MVCS.Framework();
        framework.setLogger(logger);
        framework.setConfig(config);
        framework.Initialize();

        // 加载模块
        Dictionary<string, MVCS.Any> settings = new Dictionary<string, MVCS.Any>();
        moduleManager = new ModuleManager();
        moduleManager.OnTipChanged = (_tip) => textBootloaderTip.text = _tip;
        moduleManager.OnUpgressChanged = (_percentage) => textBootloaderUpgress.text = _percentage.ToString();
        moduleManager.OnBootFinish = () => bootloader.SetActive(false);
        settings["vendor"] = MVCS.Any.FromString(activeVendor_.directory);
        settings["datapath"] = MVCS.Any.FromString(datapath);
        settings["devicecode"] = MVCS.Any.FromString(Constant.DeviceCode);
        settings["platform"] = MVCS.Any.FromString(Constant.Platform);
        settings["main.canvas"] = MVCS.Any.FromObject(mainCanvas);
        settings["main.font"] = MVCS.Any.FromObject(mainFont);
        moduleManager.Load(activeVendor_.directory, datapath);
        // 注册模块中的MVCS
        moduleManager.Inject(this, framework, logger, config, settings);
        moduleManager.Register();

        // 装载已注册的部件
        framework.Setup();
    }

    void OnEnable()
    {
        Debug.Log("---------------  OnEnable ------------------------");
    }

    IEnumerator Start()
    {
        Debug.Log("---------------  Start ------------------------");
        yield return new WaitForEndOfFrame();
        moduleManager.Preload();
    }

    void Update()
    {
    }

    void OnDisable()
    {
        Debug.Log("---------------  OnDisable ------------------------");
    }

    void OnDestroy()
    {
        Debug.Log("---------------  OnDestroy ------------------------");

        framework.Dismantle();
        // 拆卸模块中的MVCS
        moduleManager.Cancel();
        framework.Release();
        framework = null;

        moduleManager.Unload();
    }
}
