# 📄 PaperTracker VRCFaceTracking 模块

<p align="center">
  <img src="PaperTrackerLogo.png" alt="PaperTracker Logo" width="200"/>
</p>

<p align="center">
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/releases"><img src="https://img.shields.io/github/v/release/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="Release"></a>
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/blob/main/LICENSE"><img src="https://img.shields.io/github/license/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="License"></a>
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/issues"><img src="https://img.shields.io/github/issues/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="Issues"></a>
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/stargazers"><img src="https://img.shields.io/github/stars/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="Stars"></a>
</p>

## 🚀 概述

PaperTracker VRCFaceTracking 模块是一个专业的面部和眼部追踪解决方案，旨在为 VRChat 用户提供高质量的表情追踪体验。该模块采用统一的追踪架构，作为 PaperTracker 硬件和 VRCFaceTracking 系统之间的桥梁，实现了数据的高效双向传输和处理。

## ✨ 主要特性

### 🎯 核心功能
- **统一追踪架构**：集成眼部和面部追踪于单一模块
- **双向数据传输**：支持眼部和面部追踪数据的接收与发送
- **高精度追踪**：提供精确的面部表情和眼部运动捕捉
- **实时处理**：低延迟的数据处理和传输（8ms更新周期）
- **智能映射器**：支持 V1 和 V2 两种眼部追踪数据格式的自动适配
- **统一配置管理**：支持旧配置自动迁移和统一配置文件

### 👁️ 眼部追踪
- **眼球运动追踪**：X/Y 轴精确定位
- **眼睑动作**：支持眨眼、眯眼、瞪眼检测
- **瞳孔扩张**：实时瞳孔大小变化追踪
- **眉毛动作**：眉毛上扬和下降检测
- **自适应阈值**：可配置的动作触发阈值

### 😊 面部追踪
- **口部动作**：张嘴、微笑、皱眉等表情
- **脸颊动作**：鼓腮、面部肌肉运动
- **舌头检测**：舌头伸出动作识别
- **唇部形状**：嘟嘴、撅嘴等精细动作

## 📋 系统要求

- **操作系统**：Windows 10/11 (64位)
- **运行时**：.NET 7.0 或更高版本
- **VRCFaceTracking**：最新版本
- **硬件**：PaperTracker 设备或兼容的追踪硬件

## 🛠️ 安装指南

### 方法一：通过 VRCFaceTracking 模块注册表安装

1. 打开 VRCFaceTracking 应用程序
2. 导航到 "模块注册表" 页面
3. 搜索 "PaperTracker Module"
4. 点击 "安装" 按钮

### 方法二：手动安装

1. 从 [Releases](https://github.com/DJI-B/VRCFT-PaperTracker/releases) 页面下载最新版本
2. 解压下载的文件
3. 将 `VRCFaceTracking.PaperTracker.dll` 复制到 VRCFaceTracking 的模块目录
4. 重新启动 VRCFaceTracking

## ⚙️ 配置说明

### 统一配置文件 (UnifiedTrackerConfig.json)

```json
{
  "EyeTracking": {
    "ListeningAddress": "127.0.0.1",
    "PortNumber": 8889,
    "ShouldEmulateEyeWiden": true,
    "ShouldEmulateEyeSquint": true,
    "ShouldEmulateEyebrows": true,
    "OutputMultiplier": 1.1,
    "MapperVersion": "V2"
  },
  "FaceTracking": {
    "FaceHost": "127.0.0.1",
    "FacePort": 8888
  },
  "EnableEyeTracking": true,
  "EnableFaceTracking": true
}
```

### 配置参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `ListeningAddress` | 眼部追踪监听地址 | `127.0.0.1` |
| `PortNumber` | 眼部追踪端口号 | `8889` |
| `FaceHost` | 面部追踪主机地址 | `127.0.0.1` |
| `FacePort` | 面部追踪端口号 | `8888` |
| `ShouldEmulateEyeWiden` | 是否模拟眼部瞪大 | `true` |
| `ShouldEmulateEyeSquint` | 是否模拟眼部眯眼 | `true` |
| `ShouldEmulateEyebrows` | 是否模拟眉毛动作 | `true` |
| `OutputMultiplier` | 输出信号倍数 | `1.1` |
| `MapperVersion` | 眼部追踪映射器版本 (V1/V2) | `V2` |
| `EnableEyeTracking` | 启用眼部追踪 | `true` |
| `EnableFaceTracking` | 启用面部追踪 | `true` |

## 🧪 测试工具

项目包含一个 Python 测试脚本 `osc_test.py`，用于测试 OSC 消息的发送和接收：

```bash
# 安装依赖
pip install python-osc

# 测试面部追踪
python osc_test.py --mode face

# 测试眼部追踪
python osc_test.py --mode eye

# 持续测试模式
python osc_test.py --mode continuous
```

## 🔧 开发信息

### 技术架构

- **语言**：C# (.NET 7.0)
- **框架**：VRCFaceTracking SDK
- **通信协议**：OSC (Open Sound Control)
- **数据格式**：JSON 配置，二进制数据传输

### 项目结构

```
PaperTrackerPlugin/
├── Configuration/          # 统一配置管理
│   ├── UnifiedConfig.cs   # 配置数据结构
│   └── UnifiedConfigManager.cs # 配置管理器，支持迁移
├── Core/                  # 核心功能
│   ├── Filters/          # 数据过滤器
│   │   └── OneEuroFilter.cs # 一欧元滤波器
│   ├── Models/           # 数据模型
│   │   └── OSCMessage.cs # OSC 消息模型
│   └── OSC/              # OSC 通信
│       └── UnifiedOSCManager.cs # 统一 OSC 管理器
├── Tracking/              # 追踪功能
│   ├── Eye/              # 眼部追踪
│   │   ├── EyeTrackingManager.cs # 眼部追踪管理器
│   │   ├── Mappers/      # 数据映射器
│   │   │   ├── BaseEyeMapper.cs # 基础映射器
│   │   │   ├── V1EyeMapper.cs # V1 格式映射器
│   │   │   └── V2EyeMapper.cs # V2 格式映射器
│   │   └── Strategies/   # 追踪策略
│   └── Face/             # 面部追踪
│       ├── FaceTrackingManager.cs # 面部追踪管理器
│       └── PaperFaceTrackerExpressions.cs # 表情定义
├── Utils/                 # 工具类
│   ├── IPAddressNewtonsoftConverter.cs # IP地址转换器
│   ├── MathUtils.cs      # 数学工具
│   ├── TwoKeyDictionary.cs # 双键字典
│   └── Validators.cs     # 验证工具
├── UnifiedTrackerConfig.cs # 统一追踪模块主类
└── VRCFaceTracking/       # VRCFT 核心库
```

### 构建说明

```bash
# 克隆项目
git clone https://github.com/DJI-B/VRCFT-PaperTracker.git
cd VRCFT-PaperTracker

# 构建项目
dotnet build

# 运行测试
dotnet test
```

## 📊 性能优化

### 数据过滤
- **OneEuroFilter**：平滑数据输出，减少抖动
- **自适应阈值**：根据用户习惯调整触发灵敏度
- **数据缓存**：优化内存使用和处理速度

### 网络优化
- **UDP 协议**：低延迟数据传输
- **数据压缩**：减少网络带宽占用
- **连接池**：复用网络连接

## 🐛 故障排除

### 常见问题

1. **模块无法加载**
   - 检查 .NET 7.0 运行时是否已安装
   - 确认模块文件完整性

2. **无法接收追踪数据**
   - 检查防火墙设置
   - 确认端口号配置正确
   - 验证 PaperTracker 设备连接

3. **追踪精度不够**
   - 调整配置文件中的阈值参数
   - 检查设备校准状态
   - 优化环境光照条件

## 📚 API 文档

### 主要类和接口

- `UnifiedTrackingModule`：统一追踪模块主类，继承自 ExtTrackingModule
- `UnifiedConfigManager`：统一配置管理器，支持配置迁移和验证
- `EyeTrackingManager`：眼部追踪管理器，支持多种映射器
- `FaceTrackingManager`：面部追踪管理器，处理面部表情数据
- `UnifiedOSCManager`：统一 OSC 通信管理器
- `BaseEyeMapper`、`V1EyeMapper`、`V2EyeMapper`：眼部数据映射器架构
- `OneEuroFilter`：数据平滑滤波器

## 📈 更新日志

### 最新版本特性
- ✅ **统一追踪架构**：重构为单一模块，提升稳定性
- ✅ **智能配置迁移**：自动从旧版配置文件迁移
- ✅ **多版本映射器支持**：V1/V2 眼部追踪格式兼容
- ✅ **优化更新循环**：8ms 更新周期，提升响应速度
- ✅ **改进的 Logo 显示**：嵌入式资源管理
- ✅ **修复 OSC 发送问题**：提升数据传输稳定性
- ✅ **修复 JSON 库兼容性**：解决序列化问题

## 🤝 贡献指南

1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 📞 支持和反馈

- **GitHub Issues**：[报告问题](https://github.com/DJI-B/VRCFT-PaperTracker/issues)
- **讨论区**：[GitHub Discussions](https://github.com/DJI-B/VRCFT-PaperTracker/discussions)
- **邮箱**：kamipaperp@gmail.com

## 🙏 致谢

- VRCFaceTracking 团队提供的优秀框架
- 社区贡献者的宝贵建议和反馈
- 所有使用和测试本模块的用户

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/DJI-B">DJI-B</a>
</p>