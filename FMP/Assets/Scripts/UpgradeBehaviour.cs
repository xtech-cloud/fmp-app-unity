using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

class UpgradeBehaviour : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("########### Enter Upgrade Scene");

        SceneManager.LoadScene("startup");
    }
}
