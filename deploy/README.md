# 使用说明
- git打tag
- 关闭FMP的unity工程，构建过程会静默调用unity
- cmd中运行以下脚本
  ```
  python deploy.py
  ```

# 添加新应用方法
1、在.branch中添加新的应用文件名，修改图标文件和license的key和secret。
2、添加新的repo-XXX.json文件，修改其内容。
3、在repo-cdn.txt中添加新的地址。
4、在.branch/deploy.json中添加新的应用选择。
