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
        }

        [Serializable]
        public class PanelUpdating
        {
            public Transform root;
            public Slider sliderTotal;
            public Slider sliderSingle;
            public Text textHash;
            public Text textTotalSize;
            public Text textFinishSize;
        }

        [Serializable]
        public class PanelUpdateError
        {
            public Transform root;
            public Text tip;
            public Button btnRetry;
            public Button btnSkip;
        }

        [Serializable]
        public class PanelUpdateSuccess
        {
            public Transform root;
        }

        [Serializable]
        public class PanelUpdateFailure
        {
            public Transform root;
        }

        public Transform root;

        public PanelUpdateTip updateTipPanel;
        public PanelUpdating updatingPanel;
        public PanelUpdateError updateErrorPanel;
        public PanelUpdateSuccess updateSuccessPanel;
        public PanelUpdateFailure updateFailurePanel;
    }

    public class UiTip
    {
        public string upgrade_parse_failure;
        public string dependencies_update_tip;
        public string dependencies_error;
        public string downloading_tip;
    }

    public Ui ui;
    public TextAsset upgradeTip;

    public enum Panel
    {
        NONE,
        TIP,
        UPDATING,
        ERROR,
        FAILURE,
        SUCCESS,
    }

    private Upgrade.Schema schema_;

    private Upgrade upgrade_ = new Upgrade();
    private UiTip uiTip_;

    private void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter Upgrade Scene");
        uiTip_ = JsonUtility.FromJson<UiTip>(upgradeTip.text);

        ui.root.gameObject.SetActive(true);
        ui.updateErrorPanel.btnSkip.onClick.AddListener(() =>
        {
            switchPanel(Panel.FAILURE);
            enterStartup(3);
        });
        ui.updateErrorPanel.btnRetry.onClick.AddListener(() =>
        {
            StartCoroutine(updateDependencies());
        });
        ui.updateTipPanel.btnYes.onClick.AddListener(() =>
        {
            ui.updateTipPanel.root.gameObject.SetActive(false);
            StartCoroutine(downloadDependencies());
        });
        ui.updateTipPanel.btnNo.onClick.AddListener(() =>
        {
            enterStartup(0);
        });
        ui.updatingPanel.sliderTotal.value = 0;
        ui.updatingPanel.sliderSingle.value = 0;
        ui.updatingPanel.textHash.text = "";
        ui.updatingPanel.textTotalSize.text = "";
        ui.updatingPanel.textFinishSize.text = "";

        switchPanel(Panel.NONE);
    }


    private IEnumerator Start()
    {
        // Browser 不更新，在模块加载时下载
        if (RuntimePlatform.WebGLPlayer == Application.platform)
        {
            enterStartup(0);
            yield break;
        }

        var storage = new XmlStorage<Upgrade.Schema>();
        yield return storage.Load(VendorManager.Singleton.active, "Upgrade.xml");
        schema_ = storage.xml as Upgrade.Schema;
        UnityLogger.Singleton.Info("Strategy of Update is {0}", schema_.body.update.strategy);

        if (schema_.body.update.strategy.Equals("skip"))
        {
            UnityLogger.Singleton.Warning("skip upgrade");
            enterStartup(0);
            yield break;
        }

        // 升级流程参见设计文档中的升级流程说明
        // !!! 升级操作会将文件下载到缓存目录中，如果所有文件下载成功，才会将缓存目录中的文件拷贝到虚拟环境中，
        // 如果任何一个文件下载失败，虚拟环境中的文件不会发生变化
        upgrade_.ParseSchema(schema_);
        yield return updateDependencies();
    }

    private void Update()
    {
        ui.updatingPanel.textHash.text = upgrade_.updateEntryHash;
        ui.updatingPanel.textFinishSize.text = formatSize(upgrade_.updateFinishedSize);
        ui.updatingPanel.textTotalSize.text = formatSize(upgrade_.updateTotalSize);
        ui.updatingPanel.sliderTotal.value = upgrade_.updateTotalSize > 0 ? (upgrade_.updateFinishedSize * 100 / upgrade_.updateTotalSize) / 100f : 0;
        ui.updatingPanel.sliderTotal.value = upgrade_.updateEntryProgress;
    }

    private void enterStartup(float _delay)
    {
        StartCoroutine(delayEnterStartup(_delay));
    }

    private IEnumerator delayEnterStartup(float _delay)
    {
        yield return new WaitForSeconds(_delay);
        SceneManager.LoadScene("startup");
    }

    private IEnumerator updateDependencies()
    {
        UnityLogger.Singleton.Info("check dependencies ......");
        yield return upgrade_.CheckDependencies(schema_);
        if (Upgrade.ErrorCode.OK != upgrade_.errorCode)
        {
            //检查阶段有错误
            UnityLogger.Singleton.Error("check dependencies has error: {0}", upgrade_.errorCode.ToString());
            if (schema_.body.update.strategy.Equals("manual"))
            {
                // 手动模式弹出错误提示
                switchPanel(Panel.ERROR);
                ui.updateErrorPanel.tip.text = string.Format(uiTip_.dependencies_error, upgrade_.errorCode.ToString());
            }
            else
            {
                switchPanel(Panel.FAILURE);
                // 非手动模式进入startup场景
                enterStartup(3);
            }
            yield break;
        }

        // 检查阶段没有错误
        if (schema_.body.update.strategy.Equals("manual"))
        {
            // 没有数据需要更新
            UnityLogger.Singleton.Info("ready to download dependencies, totalSize is {0}, finishedSize is {1}", upgrade_.updateTotalSize, upgrade_.updateFinishedSize);
            if (upgrade_.updateFinishedSize >= upgrade_.updateTotalSize)
            {
                enterStartup(0);
                yield break;
            }
            // 手动模式弹出升级提示
            switchPanel(Panel.TIP);
            ui.updateTipPanel.tip.text = string.Format(uiTip_.dependencies_update_tip, formatSize(upgrade_.updateTotalSize - upgrade_.updateFinishedSize));
            yield break;
        }

        // 自动模式开始下载
        yield return downloadDependencies();
    }

    private IEnumerator downloadDependencies()
    {
        UnityLogger.Singleton.Info("ready to download dependencies, totalSize is {0}, finishedSize is {1}", upgrade_.updateTotalSize, upgrade_.updateFinishedSize);
        // 没有数据需要更新
        if (upgrade_.updateFinishedSize >= upgrade_.updateTotalSize)
        {
            enterStartup(0);
            yield break;
        }

        switchPanel(Panel.UPDATING);
        yield return upgrade_.DownloadDependencies(schema_);
        // 有错误
        if (Upgrade.ErrorCode.OK != upgrade_.errorCode)
        {
            UnityLogger.Singleton.Error("download has error: {0}", upgrade_.errorCode.ToString());
            if (schema_.body.update.strategy.Equals("manual"))
            {
                // 手动模式弹出错误提示
                switchPanel(Panel.ERROR);
                ui.updateErrorPanel.tip.text = string.Format(uiTip_.dependencies_error, upgrade_.errorCode.ToString());
            }
            else
            {
                switchPanel(Panel.FAILURE);
                // 非手动模式进入startup场景
                enterStartup(3);
            }
            yield break;
        }

        UnityLogger.Singleton.Info("all dependencies download success");
        switchPanel(Panel.SUCCESS);
        yield return new WaitForEndOfFrame();
        yield return upgrade_.OverwriteDependencies(schema_);
        // 有错误
        if (Upgrade.ErrorCode.OK != upgrade_.errorCode)
        {

            switchPanel(Panel.FAILURE);
            yield break;
        }
        UnityLogger.Singleton.Info("all dependencies overwrite success");
        enterStartup(1);
    }

    private string formatSize(ulong _size)
    {
        if (_size < 1024)
            return string.Format("{0}B", _size);
        if (_size < 1024 * 1024)
            return string.Format("{0}K", _size / 1024);
        if (_size < 1024 * 1024 * 1024)
            return string.Format("{0}M", _size / 1024 / 1024);
        return string.Format("{0}G", _size / 1024 / 1024 / 1024);
    }

    private void switchPanel(Panel _panel)
    {
        ui.updateTipPanel.root.gameObject.SetActive(Panel.TIP == _panel);
        ui.updatingPanel.root.gameObject.SetActive(Panel.UPDATING == _panel);
        ui.updateSuccessPanel.root.gameObject.SetActive(Panel.SUCCESS == _panel);
        ui.updateFailurePanel.root.gameObject.SetActive(Panel.FAILURE == _panel);
        ui.updateErrorPanel.root.gameObject.SetActive(Panel.ERROR == _panel);
    }
}
