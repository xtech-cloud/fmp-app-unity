using System.IO;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MVCS = XTC.FMP.LIB.MVCS;

public class Startup : MonoBehaviour
{
    public Transform mainCanvas;

    private MVCS.Framework framework;
    private MVCS.Logger logger;
    private MVCS.Config config;
    private ModuleManager moduleManager;

    // Start is called before the first frame update
    void Awake()
    {
        string vendor = "data";
        string datapath = Application.persistentDataPath;
        // 解析参数
        string[] commandLineArgs = System.Environment.GetCommandLineArgs();
        foreach (string arg in commandLineArgs)
        {
            if (arg.StartsWith("-vendor="))
            {
                vendor = arg.Replace("-vendor=", "").Trim();
            }
            else if (arg.StartsWith("-datapath="))
            {
                datapath = arg.Replace("-datapath=", "").Trim();
            }
        }
        Debug.LogFormat("Vendor is {0}", vendor);

        var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(AppConfig.instance.schema.resolution.width, AppConfig.instance.schema.resolution.height);
        canvasScaler.matchWidthOrHeight = AppConfig.instance.schema.resolution.match;

        // 初始化MVCS框架
        FileConfig fileConfig = new FileConfig();
        fileConfig.Load(datapath, vendor);
        UnityLogger uniLogger = new UnityLogger();
        logger = uniLogger;
        logger.setLevel((MVCS.LogLevel)AppConfig.instance.schema.loglevel);
        string errLogFile = Path.Combine(datapath, "err.log");
        uniLogger.errorFile = errLogFile;
        config = fileConfig;
        uniLogger.OpenLogFiles();
        framework = new MVCS.Framework();
        framework.setLogger(logger);
        framework.setConfig(config);
        framework.Initialize();

        // 加载模块
        Dictionary<string, MVCS.Any> settings = new Dictionary<string, MVCS.Any>();
        moduleManager = new ModuleManager();
        settings["vendor"] = MVCS.Any.FromString(vendor);
        settings["datapath"] = MVCS.Any.FromString(datapath);
        settings["devicecode"] = MVCS.Any.FromString(Constant.DeviceCode);
        settings["main.canvas"] = MVCS.Any.FromObject(mainCanvas);
        settings["platform"] = MVCS.Any.FromString(Constant.Platform);
        moduleManager.Load(vendor, datapath);
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
        (logger as UnityLogger).CloseLogFiles();
    }
}
