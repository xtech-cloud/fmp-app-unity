using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UpgradeBehaviour : MonoBehaviour
{
    [Serializable]
    public class Ui
    {
        [Serializable]
        public class PanelUpdateTip
        {
            public Transform root;
            public Text tip;
            public Button btnYes;
            public Button btnNo;

            public string tipFormat;
        }

        [Serializable]
        public class PanelUpdating
        {
            public Transform root;
        }

        [Serializable]
        public class PanelUpdateResult
        {
            public Transform root;
        }

        public Transform root;

        public PanelUpdateTip updateTipPanel;
        public PanelUpdating updatingPanel;
        public PanelUpdateResult updateResultPanel;
    }

    public Ui ui;

    private Upgrade.Schema schema_;

    private Upgrade upgrade_ = new Upgrade();

    private void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter Upgrade Scene");
        ui.root.gameObject.SetActive(true);
        ui.updateTipPanel.root.gameObject.SetActive(false);
        ui.updatingPanel.root.gameObject.SetActive(false);
        ui.updateResultPanel.root.gameObject.SetActive(false);
        ui.updateTipPanel.tipFormat = ui.updateTipPanel.tip.text;
    }


    private IEnumerator Start()
    {

        string vendorDir = Path.Combine(Constant.DataPath, VendorManager.Singleton.active);
        string upgradeConfigFile = Path.Combine(vendorDir, "Upgrade.xml");
        if (!File.Exists(upgradeConfigFile))
        {
            UnityLogger.Singleton.Warning("{0} not found, skip upgrade", upgradeConfigFile);
            SceneManager.LoadScene("startup");
            yield break;
        }

        try
        {
            var xs = new XmlSerializer(typeof(Upgrade.Schema));
            using (FileStream reader = new FileStream(upgradeConfigFile, FileMode.Open))
            {
                schema_ = xs.Deserialize(reader) as Upgrade.Schema;
            }
        }
        catch (System.Exception ex)
        {
            ui.updateResultPanel.root.gameObject.SetActive(true);
            //TODO 显示错误
            UnityLogger.Singleton.Exception(ex);
            yield break;
        }

        upgrade_.ParseSchema(schema_);
        yield return updateDependencies();
    }

    private IEnumerator updateDependencies()
    {
        // 检查依赖
        yield return upgrade_.CheckDependencies(schema_);
        if (Upgrade.ErrorCode.OK != upgrade_.updateDependenciesError)
        {
            ui.updateResultPanel.root.gameObject.SetActive(true);
            yield break;
        }
        ui.updateTipPanel.root.gameObject.SetActive(true);
        ui.updateTipPanel.tip.text = string.Format(ui.updateTipPanel.tipFormat, formatSize(upgrade_.result.totalSize));
        UnityLogger.Singleton.Info(upgrade_.result.totalSize.ToString());
    }

    private string formatSize(long _size)
    {
        if (_size < 1024)
            return string.Format("{0}B", _size);
        if (_size < 1024 * 1024)
            return string.Format("{0}K", _size / 1024);
        if (_size < 1024 * 1024 * 1024)
            return string.Format("{0}M", _size / 1024 / 1024);
        return string.Format("{0}G", _size / 1024 / 1024 / 1024);
    }
}
