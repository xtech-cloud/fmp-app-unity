using XTC.FMP.LIB.MVCS;

public class WebLogger : Logger
{
    public new void Trace(string _message, params object[] _args)
    {
        UnityEngine.Debug.Log(string.Format("TRACE - {0}", string.Format(_message, _args)));
    }

    public new void Debug(string _message, params object[] _args)
    {
        UnityEngine.Debug.Log(string.Format("DEBUG - {0}", string.Format(_message, _args)));
    }

    public new void Info(string _message, params object[] _args)
    {
        UnityEngine.Debug.Log(string.Format("INFO - {0}", string.Format(_message, _args)));
    }

    public new void Warning(string _message, params object[] _args)
    {
        UnityEngine.Debug.Log(string.Format("WARNING - {0}", string.Format(_message, _args)));
    }

    public new void Error(string _message, params object[] _args)
    {
        UnityEngine.Debug.Log(string.Format("ERROR - {0}", string.Format(_message, _args)));
    }

    public new void Exception(System.Exception _exp)
    {
        UnityEngine.Debug.LogException(_exp);
    }
}

public class UnityLogger : Logger
{
    public static Logger Singleton
    {
        get
        {
            if (null == singleton_)
            {
                /// wasm，调用堆栈获取方法名会抛出异常
                if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WebGLPlayer)
                    singleton_ = new WebLogger();
                else
                    singleton_ = new UnityLogger();
            }
            return singleton_;
        }
    }
    private static Logger singleton_;


    protected override void trace(string _categoray, string _message)
    {
        UnityEngine.Debug.Log(string.Format("<color=#02cbac>TRACE</color> [{0}] - {1}", _categoray, _message));
    }

    protected override void debug(string _categoray, string _message)
    {
        UnityEngine.Debug.Log(string.Format("<color=#346cfd>DEBUG</color> [{0}] - {1}", _categoray, _message));
    }
    protected override void info(string _categoray, string _message)
    {
        UnityEngine.Debug.Log(string.Format("<color=#04fc04>INFO</color> [{0}] - {1}", _categoray, _message));
    }
    protected override void warning(string _categoray, string _message)
    {
        UnityEngine.Debug.LogWarning(string.Format("<color=#fce204>WARNING</color> [{0}] - {1}", _categoray, _message));
    }
    protected override void error(string _categoray, string _message)
    {
        UnityEngine.Debug.LogError(string.Format("<color=#fc0450>ERROR</color> [{0}] - {1}", _categoray, _message));
    }
    protected override void exception(System.Exception _exp)
    {
        UnityEngine.Debug.LogException(_exp);
    }
}
