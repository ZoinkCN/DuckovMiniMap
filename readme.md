# 小地图Mod

## 概述

本 Mod 用于添加游戏的小地图，提供可配置的图标映射、动态 POI 管理、最小地图渲染增强与自定义输入支持。

## 主要功能

- 显示小地图，兼容 ModSetting，可在设置界面中修改参数。
- 添加角色POI，在地图中显示单位位置，支持通过 `Resources/config/iconConfig.json` 自定义图标。
- 检测鼠标位置，当鼠标进入小地图范围内，小地图会变成半透明以避免遮挡视线。

## 要求

- 目标框架：`.NET Standard 2.1`
- 推荐 IDE：Visual Studio 或兼容的 .NET 开发环境

## 配置

- `config/iconConfig.json`：编辑图标 ID、路径与映射规则以更改 POI 的显示。请确保 JSON 格式有效且资源路径正确，路径默认以 `textures` 文件夹为根目录，其内文件直接写文件名即可，若有子文件夹，请将子文件夹写在路径中。
- `config/modConfig.json`: 保存的mod设置，首次运行后自动生成。
- `config/template/modConfigTemplate.json`: 生成 ModSetting 设置界面的模板文件，请勿修改，除非你知道你在做什么。

## ⚠️注意
玩家自定义修改的图片资源请及时备份，避免mod后续更新时被覆盖！
