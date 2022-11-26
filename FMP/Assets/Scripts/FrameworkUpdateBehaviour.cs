using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FrameworkUpdateBehaviour : MonoBehaviour
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
        public string update_parse_failure;
        public string dependencies_update_tip;
        public string dependencies_error;
        public string downloading_tip;
    }

    public Ui ui;
    public TextAsset updateTip;

    public enum Panel
    {
        NONE,
        TIP,
        UPDATING,
        ERROR,
        FAILURE,
        SUCCESS,
    }

    private FrameworkUpdate frameworkUpdate_ = new FrameworkUpdate();
    private UiTip uiTip_;
    private string updateStrategy_;

    private void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter FrameworkUpdate Scene");
        uiTip_ = JsonUtility.FromJson<UiTip>(updateTip.text);

        ui.root.gameObject.SetActive(true);
        ui.updateErrorPanel.btnSkip.onClick.AddListener(() =>
        {
            switchPanel(Panel.FAILURE);
            enterAssetSyndication(3);
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
            enterAssetSyndication(0);
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
        // WASM 不支持程序集运行时加载，
        if (RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            enterAssetSyndication(0);
            yield break;
        }

        updateStrategy_ = VendorManager.Singleton.active.updateConfig.schema.body.frameworkUpdate.strategy;
        UnityLogger.Singleton.Info("Strategy of FrameworkUpdate is {0}", updateStrategy_);

        if (updateStrategy_.Equals("skip"))
        {
            UnityLogger.Singleton.Warning("skip frameworkupdate");
            enterAssetSyndication(0);
            yield break;
        }

        // 更新流程参见设计文档中的更新流程说明
        // !!! 更新操作会将文件下载到缓存目录中，如果所有文件下载成功，才会将缓存目录中的文件拷贝到虚拟环境中，
        // 如果任何一个文件下载失败，虚拟环境中的文件不会发生变化
        frameworkUpdate_.ParseSchema();
        yield return updateDependencies();
    }

    private void Update()
    {
        ui.updatingPanel.textHash.text = frameworkUpdate_.updateEntryHash;
        ui.updatingPanel.textFinishSize.text = formatSize(frameworkUpdate_.updateFinishedSize);
        ui.updatingPanel.textTotalSize.text = formatSize(frameworkUpdate_.updateTotalSize);
        ui.updatingPanel.sliderTotal.value = frameworkUpdate_.updateTotalSize > 0 ? (frameworkUpdate_.updateFinishedSize * 100 / frameworkUpdate_.updateTotalSize) / 100f : 0;
        ui.updatingPanel.sliderSingle.value = frameworkUpdate_.updateEntryProgress;
    }

    private void enterAssetSyndication(float _delay)
    {
        StartCoroutine(delayEnterAssetSyndication(_delay));
    }

    private IEnumerator delayEnterAssetSyndication(float _delay)
    {
        yield return new WaitForSeconds(_delay);
        SceneManager.LoadScene("AssetSyndication");
    }

    private IEnumerator updateDependencies()
    {
        UnityLogger.Singleton.Info("check dependencies ......");
        yield return frameworkUpdate_.CheckDependencies();
        if (FrameworkUpdate.ErrorCode.OK != frameworkUpdate_.errorCode)
        {
            //检查阶段有错误
            UnityLogger.Singleton.Error("check dependencies has error: {0}", frameworkUpdate_.errorCode.ToString());
            if (updateStrategy_.Equals("manual"))
            {
                // 手动模式弹出错误提示
                switchPanel(Panel.ERROR);
                ui.updateErrorPanel.tip.text = string.Format(uiTip_.dependencies_error, frameworkUpdate_.errorCode.ToString());
            }
            else
            {
                switchPanel(Panel.FAILURE);
                // 非手动模式进入startup场景
                enterAssetSyndication(3);
            }
            yield break;
        }

        // 检查阶段没有错误
        if (updateStrategy_.Equals("manual"))
        {
            // 没有数据需要更新
            UnityLogger.Singleton.Info("ready to download dependencies, totalSize is {0}, finishedSize is {1}", frameworkUpdate_.updateTotalSize, frameworkUpdate_.updateFinishedSize);
            if (frameworkUpdate_.updateFinishedSize >= frameworkUpdate_.updateTotalSize)
            {
                enterAssetSyndication(0);
                yield break;
            }
            // 手动模式弹出更新提示
            switchPanel(Panel.TIP);
            ui.updateTipPanel.tip.text = string.Format(uiTip_.dependencies_update_tip, formatSize(frameworkUpdate_.updateTotalSize - frameworkUpdate_.updateFinishedSize));
            yield break;
        }

        // 自动模式开始下载
        yield return downloadDependencies();
    }

    private IEnumerator downloadDependencies()
    {
        UnityLogger.Singleton.Info("ready to download dependencies, totalSize is {0}, finishedSize is {1}", frameworkUpdate_.updateTotalSize, frameworkUpdate_.updateFinishedSize);
        // 没有数据需要更新
        if (frameworkUpdate_.updateFinishedSize >= frameworkUpdate_.updateTotalSize)
        {
            enterAssetSyndication(0);
            yield break;
        }

        switchPanel(Panel.UPDATING);
        yield return frameworkUpdate_.DownloadDependencies();
        // 有错误
        if (FrameworkUpdate.ErrorCode.OK != frameworkUpdate_.errorCode)
        {
            UnityLogger.Singleton.Error("download has error: {0}", frameworkUpdate_.errorCode.ToString());
            if (updateStrategy_.Equals("manual"))
            {
                // 手动模式弹出错误提示
                switchPanel(Panel.ERROR);
                ui.updateErrorPanel.tip.text = string.Format(uiTip_.dependencies_error, frameworkUpdate_.errorCode.ToString());
            }
            else
            {
                switchPanel(Panel.FAILURE);
                // 非手动模式进入startup场景
                enterAssetSyndication(3);
            }
            yield break;
        }

        UnityLogger.Singleton.Info("all dependencies download success");
        switchPanel(Panel.SUCCESS);
        yield return new WaitForEndOfFrame();
        yield return frameworkUpdate_.OverwriteDependencies();
        // 有错误
        if (FrameworkUpdate.ErrorCode.OK != frameworkUpdate_.errorCode)
        {

            switchPanel(Panel.FAILURE);
            yield break;
        }
        UnityLogger.Singleton.Info("all dependencies overwrite success");
        enterAssetSyndication(1);
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
