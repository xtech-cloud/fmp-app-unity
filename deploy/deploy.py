#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os
import sys
import shutil
import subprocess
import json
from colorama import init

init(autoreset=True)


def printRed(_msg):
    print("\033[0;31;40m{}\033[0m".format(_msg))

def printGreen(_msg):
    print("\033[0;32;40m{}\033[0m".format(_msg))

def printYellow(_msg):
    print("\033[0;33;40m{}\033[0m".format(_msg))

def printBlue(_msg):
    print("\033[0;34;40m{}\033[0m".format(_msg))

def printPurple(_msg):
    print("\033[0;35;40m{}\033[0m".format(_msg))

def printCyan(_msg):
    print("\033[0;36;40m{}\033[0m".format(_msg))


unity_home = ""
tmp_dir = "_tmp"
package_dir = "_package"
keystore_path = os.path.abspath("../unitysln/keystore").replace("\\", "/")
current_dir = os.path.abspath(os.path.curdir)

ex = subprocess.Popen("git tag --contains", stdout=subprocess.PIPE, shell=True)
out, err = ex.communicate()
status = ex.wait()
version = out.decode().strip().replace("v", "")

if "" == version:
    printRed("ERROR: tag is required!!")
    exit(1)

branch_dir = "branch"
if os.path.exists(".branch"):
    branch_dir = ".branch"

deploy_json = "deploy.json"
if os.path.exists(branch_dir + "/.deploy.json"):
    deploy_json = ".deploy.json"

"""
检测unity是否存在
"""
if not os.path.exists(".UNITY_HOME.env"):
    printRed("ERROR: file .UNITY_HOME.env is required!")
    sys.exit(1)

with open(".UNITY_HOME.env") as f:
    unity_home = f.read().strip()
    f.close()

if not os.path.exists(unity_home):
    printRed("ERROR: {} not found!".format(unity_home))
    sys.exit(1)

if not os.path.exists(branch_dir):
    printRed("ERROR: directory {} not found!".format(branch_dir))
    sys.exit(1)

if not os.path.exists(branch_dir + "/" + deploy_json):
    print("ERROR: {} not found!".format(branch_dir + "/" + deploy_json))
    exit(1)


def backup():
    try:
        shutil.copy("../FMP/ProjectSettings/ProjectSettings.asset", "./_tmp")
        shutil.copy("../FMP/Assets/AppData/icon.png", "./_tmp")
        shutil.copy("../FMP/Assets/Scripts/BusinessBranch.cs", "./_tmp")
        shutil.copy("../FMP/Packages/manifest.json", "./_tmp")
        shutil.copy("../FMP/Packages/packages-lock.json", "./_tmp")
    except Exception as e:
        printRed(e)
        clean()
        sys.exit(1)


def restore():
    try:
        shutil.copy(
            "./_tmp/ProjectSettings.asset",
            "../FMP/ProjectSettings/ProjectSettings.asset",
        )
        shutil.copy("./_tmp/icon.png", "../FMP/Assets/AppData/icon.png")
        shutil.copy(
            "./_tmp/BusinessBranch.cs", "../FMP/Assets/Scripts/BusinessBranch.cs"
        )
        shutil.copy("./_tmp/manifest.json", "../FMP/Packages/manifest.json")
        shutil.copy("./_tmp/packages-lock.json", "../FMP/Packages/packages-lock.json")
    except Exception as e:
        print(e)
        clean()
        exit(1)


def clean():
    printYellow("clean ... ")
    if os.path.exists(tmp_dir):
        shutil.rmtree(tmp_dir)
    if os.path.exists(package_dir):
        shutil.rmtree(package_dir)


def build(_company, _product, _buildParameter, _bits, _output):
    printYellow("build {} ...".format(_product))
    printCyan("-----------------------------------------------------------------------")
    printCyan("company: {}".format(_company))
    printCyan("product: {}".format(_product))
    printCyan("builder: {}".format(_buildParameter))
    printCyan("version: {}".format(version))
    printCyan("keystore: {}".format(keystore_path))
    printCyan("curdir: {}".format(current_dir))
    printCyan("unity: {}".format(unity_home))
    printCyan("-----------------------------------------------------------------------")
    # 删除中间文件夹
    clean()
    printGreen("clean SUCCESS")
    # 创建中间文件夹
    os.mkdir(tmp_dir)
    os.mkdir(package_dir)
    # 删除Unity/Plugins文件夹
    if os.path.exists("../FMP/Assets/Plugins"):
        shutil.rmtree("../FMP/Assets/Plugins")
    # 备份原始数据
    backup()
    printGreen("backup SUCCESS")

    # 改写分支的项目设置文件
    projectSettings = ""
    with open(branch_dir + "/ProjectSettings.asset") as f:
        projectSettings = f.read()
        f.close()
    projectSettings = projectSettings.replace("{{company}}", _company)
    projectSettings = projectSettings.replace("{{product}}", _product)
    projectSettings = projectSettings.replace("{{version}}", version)
    projectSettings = projectSettings.replace("{{keystore_path}}", keystore_path)

    # 使用分支文件替换项目文件
    with open("../FMP/ProjectSettings/ProjectSettings.asset", "w") as f:
        f.write(projectSettings)
        f.close()
    shutil.copy(
        "{}/{}/icon.png".format(branch_dir, _product), "../FMP/Assets/AppData/icon.png"
    )
    shutil.copy(
        "{}/{}/BusinessBranch.cs".format(branch_dir, _product),
        "../FMP/Assets/Scripts/BusinessBranch.cs",
    )
    if os.path.exists("{}/{}/Packages".format(branch_dir, _product)):
        shutil.copy(
            "{}/{}/Packages/packages-lock.json".format(branch_dir, _product),
            "../FMP/Packages/packages-lock.json",
        )
        shutil.copy(
            "{}/{}/Packages/manifest.json".format(branch_dir, _product),
            "../FMP/Packages/manifest.json",
        )
    if os.path.exists("{}/{}/Plugins".format(branch_dir, _product)):
        shutil.copytree(
            "{}/{}/Plugins".format(branch_dir, _product),
            "../FMP/Assets/Plugins",
        )
    printGreen("overwrite SUCCESS")
    printYellow("build ... ")
    # 构建
    os.system(
        "{}/Editor/Unity.exe -quit -batchmode -projectPath ../FMP -{} {}/_package/{}/application/{}.exe".format(
            unity_home, _buildParameter, current_dir, _product, _product
        )
    )
    printGreen("build SUCCESS")

    # 还原原始数据
    restore()
    if os.path.exists("../FMP/Assets/Plugins"):
        shutil.rmtree("../FMP/Assets/Plugins")
    printGreen("restore SUCCESS")

    # 打包
    try:
        pluginsExt = os.path.abspath("{}/{}/PluginsExt".format(branch_dir, _product))
        if os.path.exists(pluginsExt):
            for subdir in os.listdir(pluginsExt):
                shutil.copytree(
                    "{}/{}/PluginsExt/{}".format(branch_dir, _product, subdir),
                    "./_package/{}/application/{}_Data/Plugins/{}".format(
                        _product, _product, subdir
                    ),
                )
    except Exception as e:
        printRed(e)
        clean()
        sys.exit(1)
    os.system(
        "{}/7z/7z.exe a {}\_output\{}_v{}.zip .\_package\*".format(
            current_dir, current_dir, _output, version
        )
    )
    printGreen("archive SUCCESS")
    # 删除中间文件夹
    clean()
    printGreen("clean SUCCESS")


if os.path.exists("_output"):
    shutil.rmtree("_output")

with open(branch_dir + "/" + deploy_json) as f:
    targets = json.loads(f.read())
    for target in targets:
        product = target["product"]
        company = target["company"]
        output = target["output"]
        if "win64" == target["platform"].lower():
            buildParameter = "buildWindows64Player"
            bits = "64"
        elif "win32" == target["platform"].lower():
            buildParameter = "buildWindowsPlayer"
            bits = "86"
        else:
            sys.exit(1)
        build(company, product, buildParameter, bits, output)
