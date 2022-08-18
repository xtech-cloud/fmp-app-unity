# 使用说明
- git打tag
- 关闭FMP的unity工程，构建过程会静默调用unity
- cmd中运行以下脚本
  ```
  python deploy.py
  ```

# 添加新应用方法
1、复制branch文件夹为.branch
2、在.branch中添加新的应用文件名，修改图标文件和license的key和secret。
3、在.branch/deploy.json中添加新的应用。
