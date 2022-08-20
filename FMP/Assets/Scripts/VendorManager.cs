using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VendorManager
{
    public static VendorManager Singleton
    {
        get
        {
            if (null == singleton_)
                singleton_ = new VendorManager();
            return singleton_;
        }
    }

    public string active { get; set; } = "";

    private static VendorManager singleton_;
}
