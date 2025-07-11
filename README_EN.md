# 📄 PaperTracker VRCFaceTracking Module

<p align="center">
  <img src="PaperTrackerLogo.png" alt="PaperTracker Logo" width="200"/>
</p>

<p align="center">
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/releases"><img src="https://img.shields.io/github/v/release/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="Release"></a>
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/blob/main/LICENSE"><img src="https://img.shields.io/github/license/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="License"></a>
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/issues"><img src="https://img.shields.io/github/issues/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="Issues"></a>
  <a href="https://github.com/DJI-B/VRCFT-PaperTracker/stargazers"><img src="https://img.shields.io/github/stars/DJI-B/VRCFT-PaperTracker?style=flat-square" alt="Stars"></a>
</p>

## 🚀 Overview

The PaperTracker VRCFaceTracking Module is a professional facial and eye tracking solution designed to provide high-quality expression tracking experience for VRChat users. This module serves as a bridge between PaperTracker hardware and VRCFaceTracking system, enabling bidirectional data transmission and processing.

## ✨ Key Features

### 🎯 Core Functionality
- **Bidirectional Data Transfer**: Supports receiving and sending both eye and face tracking data
- **High-Precision Tracking**: Provides accurate facial expression and eye movement capture
- **Real-Time Processing**: Low-latency data processing and transmission
- **Smart Mapping**: Supports both V1 and V2 eye tracking data formats

### 👁️ Eye Tracking
- **Eye Movement Tracking**: Precise X/Y axis positioning
- **Eyelid Actions**: Supports blink, squint, and widen detection
- **Pupil Dilation**: Real-time pupil size change tracking
- **Eyebrow Movement**: Eyebrow raising and lowering detection
- **Adaptive Thresholds**: Configurable action trigger thresholds

### 😊 Face Tracking
- **Mouth Actions**: Jaw open, smile, frown, and other expressions
- **Cheek Actions**: Cheek puff and facial muscle movements
- **Tongue Detection**: Tongue protrusion action recognition
- **Lip Shapes**: Funnel, pucker, and other detailed actions

## 📋 System Requirements

- **Operating System**: Windows 10/11 (64-bit)
- **Runtime**: .NET 7.0 or higher
- **VRCFaceTracking**: Latest version
- **Hardware**: PaperTracker device or compatible tracking hardware

## 🛠️ Installation Guide

### Method 1: Install via VRCFaceTracking Module Registry

1. Open VRCFaceTracking application
2. Navigate to "Module Registry" page
3. Search for "PaperTracker Module"
4. Click "Install" button

### Method 2: Manual Installation

1. Download the latest version from [Releases](https://github.com/DJI-B/VRCFT-PaperTracker/releases) page
2. Extract the downloaded file
3. Copy `VRCFaceTracking.PaperTracker.dll` to VRCFaceTracking's module directory
4. Restart VRCFaceTracking

## ⚙️ Configuration

### Main Configuration Options

```json
{
  "EyeTracking": {
    "ListeningAddress": "127.0.0.1",
    "PortNumber": 8889,
    "ShouldEmulateEyeWiden": true,
    "ShouldEmulateEyeSquint": true,
    "ShouldEmulateEyebrows": true,
    "OutputMultiplier": 1.1
  },
  "FaceTracking": {
    "FaceHost": "127.0.0.1",
    "FacePort": 8888
  },
  "EnableEyeTracking": true,
  "EnableFaceTracking": true
}
```

### Configuration Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ListeningAddress` | Eye tracking listening address | `127.0.0.1` |
| `PortNumber` | Eye tracking port number | `8889` |
| `FaceHost` | Face tracking host address | `127.0.0.1` |
| `FacePort` | Face tracking port number | `8888` |
| `ShouldEmulateEyeWiden` | Enable eye widen emulation | `true` |
| `ShouldEmulateEyeSquint` | Enable eye squint emulation | `true` |
| `ShouldEmulateEyebrows` | Enable eyebrow movement emulation | `true` |
| `OutputMultiplier` | Output signal multiplier | `1.1` |

## 🧪 Testing Tools

The project includes a Python test script `osc_test.py` for testing OSC message sending and receiving:

```bash
# Install dependencies
pip install python-osc

# Test face tracking
python osc_test.py --mode face

# Test eye tracking
python osc_test.py --mode eye

# Continuous test mode
python osc_test.py --mode continuous
```

## 🔧 Development Information

### Technical Architecture

- **Language**: C# (.NET 7.0)
- **Framework**: VRCFaceTracking SDK
- **Communication Protocol**: OSC (Open Sound Control)
- **Data Format**: JSON configuration, binary data transmission

### Project Structure

```
PaperTrackerPlugin/
├── Configuration/          # Configuration management
├── Core/                  # Core functionality
│   ├── Filters/          # Data filters
│   ├── Models/           # Data models
│   └── OSC/              # OSC communication
├── Tracking/              # Tracking functionality
│   ├── Eye/              # Eye tracking
│   └── Face/             # Face tracking
├── Utils/                 # Utility classes
└── VRCFaceTracking/       # VRCFT core library
```

### Build Instructions

```bash
# Clone the project
git clone https://github.com/DJI-B/VRCFT-PaperTracker.git
cd VRCFT-PaperTracker

# Build the project
dotnet build

# Run tests
dotnet test
```

## 📊 Performance Optimization

### Data Filtering
- **OneEuroFilter**: Smooths data output, reduces jitter
- **Adaptive Thresholds**: Adjusts trigger sensitivity based on user habits
- **Data Caching**: Optimizes memory usage and processing speed

### Network Optimization
- **UDP Protocol**: Low-latency data transmission
- **Data Compression**: Reduces network bandwidth usage
- **Connection Pooling**: Reuses network connections

## 🐛 Troubleshooting

### Common Issues

1. **Module Cannot Load**
   - Check if .NET 7.0 runtime is installed
   - Verify module file integrity

2. **Cannot Receive Tracking Data**
   - Check firewall settings
   - Confirm port configuration is correct
   - Verify PaperTracker device connection

3. **Tracking Accuracy Issues**
   - Adjust threshold parameters in configuration file
   - Check device calibration status
   - Optimize environmental lighting conditions

## 📚 API Documentation

### Main Classes and Interfaces

- `UnifiedTrackerConfig`: Unified configuration management
- `EyeTrackingManager`: Eye tracking manager
- `FaceTrackingManager`: Face tracking manager
- `UnifiedOSCManager`: OSC communication manager

### Extension Development

```csharp
// Custom eye mapper example
public class CustomEyeMapper : BaseEyeMapper
{
    public override void MapEyeData(EyeData input, out VRCFTEyeData output)
    {
        // Implement custom mapping logic
    }
}
```

## 🤝 Contributing

1. Fork the project
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support and Feedback

- **GitHub Issues**: [Report Issues](https://github.com/DJI-B/VRCFT-PaperTracker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/DJI-B/VRCFT-PaperTracker/discussions)
- **Email**: support@papertracker.com

## 🙏 Acknowledgments

- VRCFaceTracking team for the excellent framework
- Community contributors for valuable suggestions and feedback
- All users who use and test this module

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/DJI-B">DJI-B</a>
</p>