using System.Reflection;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.OSC;
using VRCFaceTracking.PaperTracker.Tracking.Eye;
using VRCFaceTracking.PaperTracker.Tracking.Face;

namespace VRCFaceTracking.PaperTracker;

public class UnifiedTrackingModule : ExtTrackingModule
{
    private UnifiedConfigManager? _configManager;
    private UnifiedOSCManager? _oscManager;
    private EyeTrackingManager? _eyeTrackingManager;
    private FaceTrackingManager? _faceTrackingManager;
    
    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);
    
    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        // 设置模块信息
        ModuleInformation.Name = "PaperTracker Module";
        
        // 加载模块图标
        try
        {
            var logoStream = GetType().Assembly.GetManifestResourceStream("VRCFaceTracking.PaperTracker.PaperTrackerLogo.png");
            if (logoStream != null)
            {
                ModuleInformation.StaticImages = new List<Stream> { logoStream };
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load module logo");
        }
        
        // 获取当前路径并初始化配置管理器
        var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _configManager = new UnifiedConfigManager(currentPath, Logger);
        
        // 尝试迁移旧配置
        _configManager.MigrateFromLegacyConfigs();
        _configManager.LoadConfig();
        
        var config = _configManager.Config;
        
        bool eyeSuccess = false;
        bool expressionSuccess = false;
        
        // 初始化眼部追踪
        if (config.EnableEyeTracking && eyeAvailable)
        {
            eyeSuccess = InitializeEyeTracking(config.EyeTracking);
        }
        
        // 初始化面部表情追踪
        if (config.EnableFaceTracking && expressionAvailable)
        {
            expressionSuccess = InitializeFaceTracking(config.FaceTracking);
        }
        
        // 初始化统一OSC管理器
        if (eyeSuccess || expressionSuccess)
        {
            _oscManager = new UnifiedOSCManager(Logger, _eyeTrackingManager, _faceTrackingManager);
            _oscManager.RegisterConfigManager(_configManager);
            _oscManager.Start();
        }
        
        Logger.LogInformation($"Unified module initialized - Eye: {eyeSuccess}, Expression: {expressionSuccess}");
        return (eyeSuccess, expressionSuccess);
    }
    
    private bool InitializeEyeTracking(EyeTrackingConfig config)
    {
        try
        {
            _eyeTrackingManager = new EyeTrackingManager(Logger, config);
            Logger.LogInformation("Eye tracking initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize eye tracking");
            return false;
        }
    }
    
    private bool InitializeFaceTracking(FaceTrackingConfig config)
    {
        try
        {
            _faceTrackingManager = new FaceTrackingManager(Logger, config);
            Logger.LogInformation("Face tracking initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize face tracking");
            return false;
        }
    }
    
    public override void Update()
    {
        try
        {
            // 更新眼部追踪数据
            _eyeTrackingManager?.UpdateVRCFTState();
            
            // 更新面部表情数据
            _faceTrackingManager?.UpdateExpressionData();
            
            // 控制更新频率
            Thread.Sleep(8);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during update cycle");
        }
    }
    
    public override void Teardown()
    {
        try
        {
            _oscManager?.TearDown();
            _eyeTrackingManager?.Dispose();
            _faceTrackingManager?.Dispose();
            
            Logger.LogInformation("Unified module teardown completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during teardown");
        }
    }
}