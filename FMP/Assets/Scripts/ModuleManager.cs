using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using MVCS = XTC.FMP.LIB.MVCS;

public class ModuleManager
{
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

    private Dictionary<string, Module> modules_ = new Dictionary<string, Module>();
    private Dictionary<string, Assembly> assemblies_ = new Dictionary<string, Assembly>();

    public ModuleManager()
    {
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(assemblyResolve);
    }

    public void Load(string _vendor, string _datapath)
    {
        string modulesDir = Path.Combine(_datapath, string.Format("{0}/modules", _vendor));
        Debug.LogFormat("ready to load modules from {0}", modulesDir);
        if (!Directory.Exists(modulesDir))
        {
            Debug.LogWarningFormat("{0} not found", modulesDir);
            return;
        }

        Func<string, Assembly> loadAssembly = (_entry) =>
        {
            string file = Path.Combine(modulesDir, _entry);
            Assembly assembly = Assembly.LoadFile(file);
            if (null == assembly)
            {
                Debug.LogErrorFormat("load assembly from {0} failed!", file);
                return null;
            }

            string filename = Path.GetFileName(_entry);
            assemblies_[filename] = assembly;
            return assembly;
        };

        Action<string, Assembly> instantiateAssembly = (_entry, _assembly) =>
        {
            string filename = Path.GetFileName(_entry);
            string namespacePrefix = filename.Substring(0, filename.Length - ".LIB.Unity.dll".Length);
            string entryClassName = namespacePrefix + ".LIB.Unity.MyEntry";
            object instanceEntry = _assembly.CreateInstance(entryClassName);
            Type entryClass = _assembly.GetType(entryClassName);
            Module module = new Module(_assembly, instanceEntry, entryClass, namespacePrefix);
            modules_[filename] = module;
            Debug.LogFormat("load assembly {0} success", _entry);
        };

        Dictionary<string, Assembly> moduleFiles = new Dictionary<string, Assembly>();
        foreach (var file in Directory.GetFiles(modulesDir))
        {
            if (!file.EndsWith(".dll"))
                continue;
            Debug.LogFormat("found {0}", file);
            var assembly = loadAssembly(file);
            if (file.EndsWith(".LIB.Unity.dll") && file.Contains("FMP.MOD"))
            {
                moduleFiles[file] = assembly;
            }
        }

        // module需要在其他非模块程序集加载完后再加载
        foreach (var pair in moduleFiles)
        {
            instantiateAssembly(pair.Key, pair.Value);
        }

        Debug.LogFormat("finally load {0} modules", modules_.Count);
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
        foreach (Module module in modules_.Values)
        {
            MethodInfo miNewOptions = module.entryClass.GetMethod("NewOptions");
            var options = miNewOptions.Invoke(module.entryInstance, new object[] {});
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
        foreach (Module module in modules_.Values)
        {
            MethodInfo miRegister = module.entryClass.GetMethod("RegisterDummy");
            miRegister.Invoke(module.entryInstance, null);
        }
    }

    public void Preload()
    {
        foreach (Module module in modules_.Values)
        {
            MethodInfo miPreload = module.entryClass.GetMethod("Preload");
            miPreload.Invoke(module.entryInstance, null);
        }
    }

    public void Cancel()
    {
        foreach (Module module in modules_.Values)
        {
            MethodInfo miCancel = module.entryClass.GetMethod("CancelDummy");
            miCancel.Invoke(module.entryInstance, null);
        }
    }

    private Assembly assemblyResolve(object sender, ResolveEventArgs args)
    {
        Debug.Log(args.Name);
        return assemblies_[args.Name];
    }
}
