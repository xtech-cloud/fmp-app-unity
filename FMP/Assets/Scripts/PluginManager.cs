using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public class PluginManager
{
    public class Plugin
    {
        private string name = "";
        private object instance = null;
        private MethodInfo miDo = null;
        private MethodInfo miCall = null;
        private object[] paramDo = null;
        private object[] paramCall = null;

        public Plugin(string _name, object _instance, MethodInfo _miDo, MethodInfo _miCall)
        {
            name = _name;
            instance = _instance;
            miDo = _miDo;
            miCall = _miCall;
            paramDo = new object[2];
            paramCall = new object[3];
        }

        public void Do(string _method, object[] _param)
        {
            paramDo[0] = _method;
            paramDo[1] = _param;
            try
            {
                miDo.Invoke(instance, paramDo);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("{0}:Do:{1}", name, _method));
                Debug.LogException(ex);
            }
        }

        public void Call(string _method, object[] _param, System.Action _callback)
        {
            paramCall[0] = _method;
            paramCall[1] = _param;
            paramCall[2] = _callback;
            try
            {
                miCall.Invoke(instance, paramCall);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("{0}:Call:{1}", name, _method));
                Debug.LogException(ex);
            }
        }
    }
    public static Dictionary<string, Plugin> plugins = new Dictionary<string, Plugin>();

    /// <summary>
    /// 注册插件
    /// </summary>
    /// <param name="_file">动态链接库文件路径</param>
    /// <param name="_name">插件名</param>
    public static void Register(string _file, string _name)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(_file);
            Assembly assembly = Assembly.Load(bytes);
            Type t = assembly.GetType(_name);
            MethodInfo miDo = t.GetMethod("Do");
            MethodInfo miCall = t.GetMethod("Call");
            object obj =  assembly.CreateInstance(_name);
            plugins[_name] = new Plugin(_name, obj, miDo, miCall);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// 注销插件
    /// </summary>
    /// <param name="_name">插件名</param>
    public static void Cancel(string _name)
    {
        if (!plugins.ContainsKey(_name))
            return;
        plugins.Remove(_name);
    }

    /// <summary>
    /// 查找插件
    /// </summary>
    /// <param name="_name"></param>
    /// <returns></returns>
    public static Plugin Find(string _name)
    {
        Plugin plugin = null;
        plugins.TryGetValue(_name, out plugin);
        return plugin;
    }

    /// <summary>
    /// 创建参数
    /// </summary>
    /// <param name="_count"></param>
    /// <returns></returns>
    public static object[] CreateParamAry(int _count)
    {
        return new object[_count];
    }
}
