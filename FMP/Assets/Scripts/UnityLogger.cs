using XTC.FMP.LIB.MVCS;

public class UnityLogger : Logger
{
    public static UnityLogger Singleton { get; private set; } = new UnityLogger();


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
