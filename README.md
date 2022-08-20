# 简易说明

## 配置虚拟环境

FMP运行时，需要虚拟环境以加载相应的模块和内容等数据。
在link-vendor.bat的同级目录创建vendor文件夹，创建以下目录结构

```
vendor/
  |- Bootloader.xml
  |- configs/
  |- modules/
  |- themes/
  |- uabs/
```

FMP程序运行时，会按照Bootloader定义的引导步骤（Steps），依次加载对应的组件，并加载组件对应的资源。

Bootloader.xml的内容如下
```xml
<Bootloader>
    <Steps>
        <Step length="1" tip="加载演示模块" module="XTC.FMP.MOD.Startkit.LIB.Unity"/>
    </Steps>
</Bootloader>
```


## 部署

先确保当前的git提交具有有效的tag

进入deploy目录,运行以下命令

```bash
python deploy.py
```

在deploy/_output目录中得到打包的程序

### 创建衍生应用

在FMP的基础上，使用自定义的程序图标，程序名称，密钥等所创建出的应用，统称为衍生应用。

#### 添加衍生应用方法
1、复制branch文件夹为.branch
2、在.branch中添加新的应用文件名，修改图标文件和license的key和secret。
3、在.branch/deploy.json中添加新的衍生应用应用。
4、使用 `python deploy.py` 重新构建

# 详细文件
参见《FMP开发者指南》
