using System.Text.Json;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.PaperTracker.Core.Models;

namespace VRCFaceTracking.PaperTracker.Configuration;

// 统一配置管理器
public class UnifiedConfigManager
{
    private readonly string _configurationFileName = "UnifiedTrackerConfig.json";
    private readonly string _configFilePath;
    private UnifiedConfig _config = UnifiedConfig.Default;
    
    public UnifiedConfig Config => _config;
    
    private List<Action<UnifiedConfig>> _listeners = new();
    private readonly ILogger _logger;
    
    // OSC配置更新映射
    private readonly Dictionary<string, string> _etvrToConfigMap = new()
    {
        {"gui_VRCFTModulePort", "PortNumber" },
        {"gui_ShouldEmulateEyeWiden", "ShouldEmulateEyeWiden"},
        {"gui_ShouldEmulateEyeSquint", "ShouldEmulateEyeSquint"},
        {"gui_ShouldEmulateEyebrows", "ShouldEmulateEyebrows"},
        {"gui_WidenThresholdV1_min", "WidenThresholdV1"},
        {"gui_WidenThresholdV1_max", "WidenThresholdV1"},
        {"gui_WidenThresholdV2_min", "WidenThresholdV2"},
        {"gui_WidenThresholdV2_max", "WidenThresholdV2"},
        {"gui_SqueezeThresholdV1_min", "SqueezeThresholdV1"},
        {"gui_SqueezeThresholdV1_max", "SqueezeThresholdV1"},
        {"gui_SqueezeThresholdV2_min", "SqueezeThresholdV2"},
        {"gui_SqueezeThresholdV2_max", "SqueezeThresholdV2"},
        {"gui_EyebrowThresholdRising", "EyebrowThresholdRising"},
        {"gui_EyebrowThresholdLowering", "EyebrowThresholdLowering"},
        {"gui_OutputMultiplier", "OutputMultiplier"},
    };

    private readonly List<string> _etvrHalfValues = new()
    {
        "gui_WidenThresholdV1_min",
        "gui_WidenThresholdV1_max",
        "gui_WidenThresholdV2_min",
        "gui_WidenThresholdV2_max",
        "gui_SqueezeThresholdV1_min",
        "gui_SqueezeThresholdV1_max",
        "gui_SqueezeThresholdV2_min",
        "gui_SqueezeThresholdV2_max",
    };
    
    public UnifiedConfigManager(string configFilePath, ILogger logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(configFilePath, _configurationFileName);
    }
    
    public void LoadConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation($"Config file did not exist, creating one at {_configFilePath}");
            SaveConfig();
            return;
        }
        
        _logger.LogInformation($"Loading config from {_configFilePath}");
        var jsonData = File.ReadAllText(_configFilePath);
        try
        {
            _config = JsonSerializer.Deserialize<UnifiedConfig>(jsonData);
            NotifyListeners();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Something went wrong during config decoding. Overwriting it with defaults");
            _config = UnifiedConfig.Default;
            SaveConfig();
        }
    }
    
    public void SaveConfig()
    {
        _logger.LogInformation($"Saving config at {_configFilePath}");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var jsonData = JsonSerializer.Serialize(_config, options);
        File.WriteAllText(_configFilePath, jsonData);
    }
    
    public void UpdateConfig(UnifiedConfig newConfig)
    {
        _config = newConfig;
        SaveConfig();
        NotifyListeners();
    }
    
    // OSC配置更新处理 (从ETVR移植)
    public void UpdateConfigFromOSC(string oscFieldName, OSCValue value)
    {
        if (!_etvrToConfigMap.TryGetValue(oscFieldName, out var fieldName))
            return;

        if (!OSCValueUtils.OSCTypeMap.TryGetValue(value.Type, out var mappers))
            return;

        var oscValueInstance = Convert.ChangeType(value, mappers.Item2);
        var field = oscValueInstance.GetType().GetField("value");
        var oscValue = field!.GetValue(oscValueInstance);
        
        if (_etvrHalfValues.Contains(oscFieldName))
            HandleHalfFields(oscFieldName, fieldName, oscValue);
        else
            HandleSingleField(fieldName, oscValue);

        SaveConfig();
        NotifyListeners();
    }
    
    private void HandleSingleField<T>(string fieldName, T value)
    {
        var eyeConfig = _config.EyeTracking;
        var field = eyeConfig.GetType().GetField(fieldName);
        if (field is null) return;

        object boxedConfig = eyeConfig;
        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        var safeValue = Convert.ChangeType(value, type);

        _logger.LogInformation($"[UPDATE] updating field {fieldName} to {safeValue}");
        field.SetValue(boxedConfig, safeValue);
        
        _config.EyeTracking = (EyeTrackingConfig)boxedConfig;
    }

    private void HandleHalfFields<T>(string oscFieldName, string fieldName, T value)
    {
        var eyeConfig = _config.EyeTracking;
        var propertyInfo = eyeConfig.GetType().GetProperty(fieldName);
        if (propertyInfo is null) return;
        
        var oldValueInfo = propertyInfo.GetValue(eyeConfig);
        if (oldValueInfo is null) return;

        object boxedConfig = eyeConfig;
        var minMaxIndex = oscFieldName.Split("_").Last() == "min" ? 0 : 1;
        var valueToPreserve = ((Array)oldValueInfo).GetValue(1 - minMaxIndex)!;

        var safeValue = Convert.ChangeType(value, typeof(float))!;
        float[] updatedValue; 
        
        if(minMaxIndex == 0)
            updatedValue = new float[] { (float)safeValue, (float)valueToPreserve };
        else
            updatedValue = new float[] { (float)valueToPreserve, (float)safeValue };

        _logger.LogInformation($"[UPDATE] updating field {fieldName} to [{updatedValue[0]}, {updatedValue[1]}]");
        propertyInfo.SetValue(boxedConfig, updatedValue);
        
        _config.EyeTracking = (EyeTrackingConfig)boxedConfig;
    }
    
    public void RegisterListener(Action<UnifiedConfig> listener)
    {
        _listeners.Add(listener);
    }
    
    private void NotifyListeners()
    {
        foreach (var listener in _listeners)
        {
            listener(_config);
        }
    }
    
    // 迁移旧配置文件的方法
    public void MigrateFromLegacyConfigs()
    {
        var currentPath = Path.GetDirectoryName(_configFilePath)!;
        bool migrated = false;
        
        // 尝试迁移PaperTracker配置
        var paperConfigPath = Path.Combine(currentPath, "PaperTrackerConfig.json");
        if (File.Exists(paperConfigPath))
        {
            try
            {
                var paperConfigJson = File.ReadAllText(paperConfigPath);
                var paperConfig = JsonSerializer.Deserialize<FaceTrackingConfig>(paperConfigJson);
                _config.FaceTracking = paperConfig;
                _logger.LogInformation("Migrated PaperTracker configuration");
                migrated = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to migrate PaperTracker configuration");
            }
        }
        
        // 尝试迁移ETVR配置
        var etvrConfigPath = Path.Combine(currentPath, "ETVRModuleConfig.json");
        if (File.Exists(etvrConfigPath))
        {
            try
            {
                var etvrConfigJson = File.ReadAllText(etvrConfigPath);
                var etvrConfig = JsonSerializer.Deserialize<EyeTrackingConfig>(etvrConfigJson);
                _config.EyeTracking = etvrConfig;
                _logger.LogInformation("Migrated ETVR configuration");
                migrated = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to migrate ETVR configuration");
            }
        }
        
        // 如果有任何迁移，保存新配置
        if (migrated)
        {
            SaveConfig();
        }
    }
}