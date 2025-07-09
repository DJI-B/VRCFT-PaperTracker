using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using VRCFaceTracking.PaperTracker.Utils;

namespace VRCFaceTracking.PaperTracker.Configuration;

// 统一配置结构
public struct UnifiedConfig
{
    // 眼部追踪配置
    [JsonInclude] public EyeTrackingConfig EyeTracking { get; set; }
    
    // 面部追踪配置
    [JsonInclude] public FaceTrackingConfig FaceTracking { get; set; }
    
    // 通用配置
    [JsonInclude] public bool EnableEyeTracking { get; set; }
    [JsonInclude] public bool EnableFaceTracking { get; set; }
    
    public static UnifiedConfig Default => new()
    {
        EyeTracking = EyeTrackingConfig.Default,
        FaceTracking = FaceTrackingConfig.Default,
        EnableEyeTracking = true,
        EnableFaceTracking = true
    };
}

// 眼部追踪配置 (从ETVR移植)
public struct EyeTrackingConfig
{
    [JsonConverter(typeof(IPAddressJsonConverter))]
    [JsonInclude] public IPAddress ListeningAddress { get; set; }
    [JsonInclude] public ushort PortNumber { get; set; }
    [JsonInclude] public bool ShouldEmulateEyeWiden { get; set; }
    [JsonInclude] public bool ShouldEmulateEyeSquint { get; set; }
    [JsonInclude] public bool ShouldEmulateEyebrows { get; set; }
    
    [JsonIgnore] private float[] _squeezeThresholdV1;
    [JsonIgnore] private float[] _widenThresholdV1;
    [JsonIgnore] private float[] _squeezeThresholdV2;
    [JsonIgnore] private float[] _widenThresholdV2;
    [JsonIgnore] private float _outputMultiplier;
    
    [JsonInclude]
    public float[] SqueezeThresholdV1
    {
        get => _squeezeThresholdV1;
        set => _squeezeThresholdV1 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
    }
    
    [JsonInclude]
    public float[] WidenThresholdV1
    {
        get => _widenThresholdV1;
        set => _widenThresholdV1 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
    }
    
    [JsonInclude]
    public float[] SqueezeThresholdV2
    {
        get => _squeezeThresholdV2;
        set => _squeezeThresholdV2 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], -2f, 0f) };
    }
    
    [JsonInclude]
    public float[] WidenThresholdV2
    {
        get => _widenThresholdV2;
        set => _widenThresholdV2 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
    }
    
    [JsonInclude]
    public float OutputMultiplier
    {
        get => _outputMultiplier;
        set => _outputMultiplier = Math.Clamp(value, 0f, 2f);
    }
    
    [JsonInclude] public float EyebrowThresholdRising { get; set; }
    [JsonInclude] public float EyebrowThresholdLowering { get; set; }
    
    public static EyeTrackingConfig Default => new()
    {
        ListeningAddress = IPAddress.Any,
        PortNumber = 8889,
        ShouldEmulateEyeWiden = false,
        ShouldEmulateEyeSquint = false,
        ShouldEmulateEyebrows = false,
        WidenThresholdV1 = new[] { 0.60f, 1f },
        WidenThresholdV2 = new[] { 0.60f, 1.05f },
        SqueezeThresholdV1 = new[] { 0.07f, 0.5f },
        SqueezeThresholdV2 = new[] { 0.07f, -1f },
        EyebrowThresholdRising = 0.8f,
        EyebrowThresholdLowering = 0.15f,
        OutputMultiplier = 1f
    };
}

// 面部追踪配置 (从PaperTracker移植)
public struct FaceTrackingConfig
{
    [JsonInclude] public string FaceHost { get; set; }
    [JsonInclude] public int FacePort { get; set; }
    
    public static FaceTrackingConfig Default => new()
    {
        FaceHost = "127.0.0.1",
        FacePort = 8888
    };
}