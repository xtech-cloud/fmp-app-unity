FMP Unity Application（简称UnityApp）作为FMP方案中的视觉呈现终端部分，能灵活的搭配使用FMP方案构建出的标准模块，快速制作数字内容的交互终端。

# DD (Design Document)

## Unity应用程序 （FMP-UnityApp）

### 虚拟环境（Vendor）

为了在开发和实际使用中，能使用一个UnityApp切换不同的内容呈现，特别设计了虚拟环境的功能，vendor是一个文件结构，包含以下层级

```
|+ vendor/
  |+ configs/
  |+ modules/
  |+ uabs/
  |+ themes/
  |- Bootloader.xml
  |- Upgrade.xml
```

在UnityApp的数据持久化路径下，可以存在多个虚拟环境，在应用启动的时候，可以通过配置文件或命令行参数，指定激活一个虚拟环境作为此次启动的数据目录。

### 升级（Upgrade）

FMP构建出的标准模块，在发布后，会存放在仓库中（Repository），在UnityApp运行时，可以通过模块的更新，从仓库中获取指定版本的模块。


### 许可证（License）

### 模块管理（Module Management）

UnityApp本身只是一个基础应用，为了使用FMP方案中构建出的标准模块。需要在UnityApp启动后，将模块载入运行时。
FMP方案中的标准模块，均基于FMP-MVCS架构，具有统一的结构和接口。

### 引导器 （Bootloader）

FMP程序运行时，会按照定义的引导步骤（Steps），依次加载对应的模块，并加载模块对应的资源。

### 衍生应用（Clone Application）

UnityApp是一个母体应用，不推荐在生产环境中使用。
在UnityApp的基础上，使用自定义的程序图标，程序名称，密钥等所创建出的应用，统称为衍生应用。在生产环境中推荐使用衍生应用。

### 业务分支 (Business Branch)

业务分支主要用于在构建衍生应用时，替换敏感数据，分为源文件替换和资源文件替换两种方式。

- 源文件替换
  在构建时，替换业务分支的源文件，这种方式适合直接使用母体应用构建衍生应用的情况。
- 资源文件替换
  在启动时，读取工程中的资源文件，解析后，重写业务分支的敏感数据，这种方式适合将母体应用编译为库的情况。

### 生命周期

UnityApp的生命周期为启动（Launcher）、选择（Selector）、过场（Splash）、升级（Upgrade）、开始（Startup）五个阶段。

- 启动阶段

  此阶段完成以下工作：
  - 加载应用配置文件，如果不存在，创建一个默认的配置文件存放到数据持久化路径下。
  - 解析命令行参数，如果指定了vendor，使用此参数值作为激活的vendor。
  - 如果命令行参数没有指定vendor，则使用配置文件中激活的vendor。
  - 进入选择阶段。

- 选择阶段

  此阶段完成以下工作：
  - 如果有激活的vendor，直接进入过场阶段。
  - 如果没有激活的vendor，显示选择界面后等待用户选择后进入过场阶段。

- 过场阶段

  此阶段完成以下工作：
  - 加载激活的vendor的皮肤。
  - 根据应用配置文件调整画质。
  - 检查许可证。
  - 根据许可证状态决定是否进入升级阶段。
  - 在进入升级阶段前加载依赖项的配置文件。

- 升级阶段

  此阶段完成以下工作：
  - 根据vendor中的更新配置文件中的更新策略进行升级。
  - 升级过程会将所有的更新文件存放到临时文件夹中。
  - 升级成功后，使用临时文件加中的文件覆盖需要升级的文件。
  - 升级失败后，不做任何数据的更改。
  - 进入开始阶段。

- 开始阶段

  此阶段完成以下工作：
  - 使用应用配置文件中的配置，调整界面适配。
  - 初始化FMP-MVCS框架。
  - 载入模块到运行时，完成模块的装载
  - 按加载器的配置，依次执行模块
  - 进入事件循环
  - 在应用程序退出后，完成模块的拆卸
  - 释放FMP-MVCS框架。

# DG (Development Guide)

## Unity应用程序 （FMP-UnityApp）

### 配置虚拟环境

在link-vendor.bat的同级目录创建vendor文件夹，完成后运行link-vendor.bat。
运行一次程序，完成相关默认配置文件的创建。

### 配置升级

按需求修改vendor/Upgrade.xml文件。一个参考的例子如下
```xml
<?xml version="1.0"?>
<Schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <Body>
        <Update strategy="auto">
            <FMP environment="develop" repository="http://localhost:9000/fmp.repository">
                <References>
                    <Reference org="XTC" module="Startkit" version="1.0.0"/>
                </References>
                <Plugins>
                    <Plugin name="EasyTouchPlugin" verison="5.0.17" />
                </Plugins>
            </FMP>
        </Update>
    </Body>
    <Header>
        <Option attribute="Update.strategy" values="升级策略，可选值为：skip, auto, manual" />
    </Header>
</Schema>
```

如果使用本地磁盘的仓库，可将repository的地址修改为本机的文件夹路径。例如 `D:/MyRepository`

### 配置引导

按需求修改vendor/Bootloader.xml文件。一个参考的例子如下
```xml
<Bootloader>
    <Steps>
        <Step length="1" tip="加载演示模块" module="XTC.FMP.MOD.Startkit.LIB.Unity"/>
    </Steps>
</Bootloader>
```

### 添加衍生应用

1. 进入deploy文件夹
2. 复制branch文件夹为.branch
3. 在.branch中添加新的应用文件名，修改图标文件和license的key和secret。
4. 在.branch/deploy.json中添加新的衍生应用应用。



### 构建

先确保当前的git提交具有有效的tag

进入deploy目录,运行以下命令

```bash
python deploy.py
```

在deploy/_output目录中得到打包的程序

# UM (User Manual)

## Unity应用程序 （FMP-UnityApp）

TODO
