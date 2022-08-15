using System.IO;
using UnityEngine;

public class AppConfig
{
    private static AppConfig instance_ = null;
    private Schema schema_ = new Schema();

    [System.Serializable]
    public class Resolution
    {
        public int width = 1920;
        public int height = 1080;
        public float match = 1.0f;
    }
    public class Schema
    {
        public int fps = 60;
        public int quality = 3; //High
        public bool profiler = false;
        public int loglevel = 6;
        public Resolution resolution = new Resolution();
    }

    public Schema schema
    {
        get
        {
            return schema_;
        }
    }


    public static AppConfig instance
    {
        get
        {
            if (null == instance_)
                instance_ = new AppConfig();
            return instance_;
        }
    }

    public void MergeJson(string _file)
    {
        if (!File.Exists(_file))
        {
            Debug.LogWarning("config.json not found");
            return;
        }
        try
        {
            string json = File.ReadAllText(_file);
            schema_ = JsonUtility.FromJson<Schema>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
