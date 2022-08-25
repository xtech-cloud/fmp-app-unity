﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using MVCS = XTC.FMP.LIB.MVCS;

public class ModuleManager
{
    public class BootStep
    {
        [XmlAttribute("length")]
        public int length = 1;
        [XmlAttribute("tip")]
        public string tip = "";
        [XmlAttribute("org")]
        public string org = "";
        [XmlAttribute("module")]
        public string module = "";
    }

    public class Bootloader
    {
        [XmlArray("Steps"), XmlArrayItem("Step")]
        public BootStep[] steps { get; set; } = new BootStep[0];
    }

    public class Module
    {
        public Assembly assembly { get; private set; }
        public object entryInstance { get; private set; }
        public Type entryClass { get; private set; }
        public string namespacePrefix { get; private set; }
        public Module(Assembly _assembly, object _entryInstance, Type _entryClass, string _namespacePrefix)
        {
            assembly = _assembly;
            entryInstance = _entryInstance;
            entryClass = _entryClass;
            namespacePrefix = _namespacePrefix;
        }
    }

    public Action<int> OnUpgressChanged { get; set; }
    public Action<string> OnTipChanged { get; set; }
    public Action OnBootFinish { get; set; }

    public Dictionary<string, string> configs { get; private set; } = new Dictionary<string, string>();
    public bool success { get; private set; } = true;

    private Dictionary<string, Module> modules_ = new Dictionary<string, Module>();
    private Dictionary<string, Assembly> assemblies_ = new Dictionary<string, Assembly>();
    private Bootloader bootloader_ = new Bootloader();
    private int currentBootStep_ = 0;
    // 引导的总长度
    private int totalBootLength_ = 0;
    // 完成的引导步的长度
    private int finishedBootLength_ = 0;
    private Action<int> onBootStepProgress_;
    private Action<string> onBootStepFinish_;


    public ModuleManager()
    {
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(assemblyResolve);
        onBootStepProgress_ = this.handleBootStepProgress;
        onBootStepFinish_ = this.handleBootStepFinish;
    }

    public IEnumerator Load()
    {
        // 加载所有模块的配置文件
        // 配置文件在这里统一加载，而不由模块自己加载，是为了在模块的Unity工程中，能将配置文件放置于编译外的代码中灵活处理。
        yield return loadConfigs();
        if (!success)
            yield break;
        yield return loadDependencies();
        if (!success)
            yield break;

        // 加载bootloader
        var storage = new XmlStorage<Bootloader>();
        yield return storage.Load(VendorManager.Singleton.active, "Bootloader.xml");
        bootloader_ = storage.xml as Bootloader;
        success = true;
    }

    public void Unload()
    {
        //TODO 加载的assembly无法卸载，需要重启Unity编辑器
        assemblies_.Clear();
        modules_.Clear();
        GC.Collect();
    }

    public void Inject(MonoBehaviour _mono, MVCS.Framework _framework, MVCS.Logger _logger, MVCS.Config _config, Dictionary<string, MVCS.Any> _settings)
    {
        foreach (var module in modules_.Values)
        {
            MethodInfo miNewOptions = module.entryClass.GetMethod("NewOptions");
            var options = miNewOptions.Invoke(module.entryInstance, new object[] { });
            MethodInfo miInject = module.entryClass.GetMethod("Inject");
            miInject.Invoke(module.entryInstance, new object[] { _framework, options });
            MethodInfo miUniInject = module.entryClass.GetMethod("UniInject");
            miUniInject.Invoke(module.entryInstance, new object[] { _mono, options, _logger, _config, _settings });
            MVCS.UserData entry = module.entryInstance as MVCS.UserData;
            _framework.setUserData(module.namespacePrefix + ".LIB.MVCS.Entry", entry);
        }
    }

    public void Register()
    {
        foreach (var module in modules_.Values)
        {
            MethodInfo miRegister = module.entryClass.GetMethod("RegisterDummy");
            miRegister.Invoke(module.entryInstance, null);
        }
    }

    public void Preload()
    {
        UnityLogger.Singleton.Info("read to boot {0} steps", bootloader_.steps.Length);
        totalBootLength_ = 0;
        foreach (var step in bootloader_.steps)
        {
            totalBootLength_ += step.length;
        }

        executeNextBootStep();
    }

    public void Cancel()
    {
        foreach (var module in modules_.Values)
        {
            MethodInfo miCancel = module.entryClass.GetMethod("CancelDummy");
            miCancel.Invoke(module.entryInstance, null);
        }
    }

    private Assembly assemblyResolve(object sender, ResolveEventArgs args)
    {
        //Debug.Log(args.Name);
        return assemblies_[args.Name];
    }

    private IEnumerator loadConfigs()
    {
        var storage = new ModuleStorage();
        foreach (var reference in DependencyConfig.Singleton.body.references)
        {
            UnityLogger.Singleton.Info("load config of {0}_{1}", reference.org, reference.module);
            yield return storage.LoadConfig(VendorManager.Singleton.active, reference.org, reference.module, reference.version);
            if (200 != storage.statusCode)
            {
                UnityLogger.Singleton.Error(storage.error);
                success = false;
                yield break;
            }
            UnityLogger.Singleton.Trace("load config of {0}_{1} success", reference.org, reference.module);
            configs[string.Format("{0}_{1}", reference.org, reference.module)] = storage.config;
        }
    }

    private IEnumerator loadDependencies()
    {
        // 先加载plugin
        foreach (var plugin in DependencyConfig.Singleton.body.plugins)
        {
            yield return loadPlugin(plugin.name, plugin.version);
            if (!success)
                yield break;
        }

        // 再加载reference
        foreach (var reference in DependencyConfig.Singleton.body.references)
        {
            yield return loadReference(reference.org, reference.module, reference.version);
            if (!success)
                yield break;
        }

        UnityLogger.Singleton.Info("finally load {0} modules", modules_.Count);
    }

    private IEnumerator loadPlugin(string _file, string _version)
    {
        string file = _file + ".dll";
        var storage = new ModuleStorage();
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadAssembly(VendorManager.Singleton.active, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }

        string filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;
    }

    private IEnumerator loadReference(string _org, string _module, string _version)
    {
        var storage = new ModuleStorage();

        // proto.dll
        string file = string.Format("fmp-{0}-{1}-lib-proto.dll", _org.ToLower(), _module.ToLower());
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadAssembly(VendorManager.Singleton.active, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        string filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        // bridge.dll
        file = string.Format("fmp-{0}-{1}-lib-bridge.dll", _org.ToLower(), _module.ToLower());
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadAssembly(VendorManager.Singleton.active, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        // mvcs.dll
        file = string.Format("fmp-{0}-{1}-lib-mvcs.dll", _org.ToLower(), _module.ToLower());
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadAssembly(VendorManager.Singleton.active, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        // Unity.dll
        file = string.Format("{0}.FMP.MOD.{1}.LIB.Unity.dll", _org, _module);
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadAssembly(VendorManager.Singleton.active, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        Assembly entryAssembly = storage.assembly;

        string namespacePrefix = string.Format("{0}.FMP.MOD.{1}", _org, _module);
        string entryClassName = string.Format("{0}.LIB.Unity.MyEntry", namespacePrefix);
        UnityLogger.Singleton.Info("Create Instance of {0}", entryClassName);
        try
        {
            object instanceEntry = entryAssembly.CreateInstance(entryClassName);
            Type entryClass = entryAssembly.GetType(entryClassName);
            Module module = new Module(entryAssembly, instanceEntry, entryClass, namespacePrefix);
            modules_[filename] = module;
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
            success = false;
        }
    }

    private void executeNextBootStep()
    {
        if (currentBootStep_ >= bootloader_.steps.Length)
        {
            UnityLogger.Singleton.Info("All steps are finished");
            OnBootFinish();
            return;
        }

        BootStep step = bootloader_.steps[currentBootStep_];
        string moduleFile = string.Format("{0}.FMP.MOD.{1}.LIB.Unity.dll", step.org, step.module);
        UnityLogger.Singleton.Info("Boot the step, module is {0}, tip is {1}", moduleFile, step.tip);
        OnTipChanged(step.tip);

        Module module;
        if (!modules_.TryGetValue(moduleFile, out module))
        {
            UnityLogger.Singleton.Error("Module {0} not found", moduleFile);
            return;
        }

        MethodInfo miPreload = module.entryClass.GetMethod("Preload");
        try
        {
            miPreload.Invoke(module.entryInstance, new object[] { onBootStepProgress_, onBootStepFinish_ });
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
        }
    }


    private void handleBootStepProgress(int _percentage)
    {
        //Debug.Log(_percentage);
    }

    private void handleBootStepFinish(string _module)
    {
        UnityLogger.Singleton.Info("Boot {0} finished", _module);
        BootStep step = bootloader_.steps[currentBootStep_];
        finishedBootLength_ += step.length;

        OnUpgressChanged(finishedBootLength_ * 100 / totalBootLength_);

        currentBootStep_ += 1;
        executeNextBootStep();
    }
}
