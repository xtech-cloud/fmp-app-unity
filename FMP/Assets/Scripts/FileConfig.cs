﻿using System.IO;
using XTC.FMP.LIB.MVCS;

public class FileConfig : Config
{
    public void Load(string _datapath, string _vendor)
    {
        string dir_path = Path.Combine(_datapath, _vendor);
        dir_path = Path.Combine(dir_path, "configs");
        UnityEngine.Debug.LogFormat("ready to load config from {0}", dir_path);
        if (!Directory.Exists(dir_path))
        {
            UnityEngine.Debug.LogErrorFormat("{0} not found", dir_path);
            return;
        }

        foreach (var file in Directory.GetFiles(dir_path))
        {
            string contents = File.ReadAllText(file);
            string filename = Path.GetFileNameWithoutExtension(file);
            string key = Path.GetFileNameWithoutExtension(filename);
            fields_[key] = Any.FromString(contents);
            UnityEngine.Debug.LogFormat("save config from {0}", key);
        }

    }
}