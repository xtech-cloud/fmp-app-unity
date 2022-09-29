using System.Collections.Generic;
using UnityEngine;

public class ModulesExport : MonoBehaviour
{
    void Awake()
    {
        UnityLogger.Singleton.Info("Export Modules ...");
        ModuleManager.Singleton.ExportModule(typeof(XTC.FMP.MOD.Hotspot2D.LIB.Unity.MyEntry).Assembly, "XTC", "Hotspot2D");
        ModuleManager.Singleton.ExportModule(typeof(XTC.FMP.MOD.ImageAtlas3D.LIB.Unity.MyEntry).Assembly, "XTC", "ImageAtlas3D");
    }
}
