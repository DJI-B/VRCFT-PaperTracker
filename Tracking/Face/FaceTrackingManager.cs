using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.Models;
using VRCFaceTracking.PaperTracker.Utils;

namespace VRCFaceTracking.PaperTracker.Tracking.Face;

// 面部追踪管理器
public class FaceTrackingManager : IDisposable
{
    private readonly ILogger _logger;
    private FaceTrackingConfig _config;
    
    // 使用现有的PaperTracker表情映射
    private readonly TwoKeyDictionary<UnifiedExpressions, string, float> _expressionMap;
    private bool _disposed = false;
    
    public FaceTrackingManager(ILogger logger, FaceTrackingConfig config)
    {
        _logger = logger;
        _config = config;
        
        // 初始化表情映射 (使用现有的PaperTrackerExpressions)
        _expressionMap = PaperTrackerExpressions.PaperTrackerExpressionMap;
    }
    
    public void UpdateConfig(FaceTrackingConfig config)
    {
        if (_disposed) return;
        _config = config;
    }
    
    public void ProcessMessage(OSCMessage message)
    {
        if (!message.success || _disposed) return;
        
        try
        {
            if (message.value is OSCFloat oscFloat)
            {
                ProcessExpressionMessage(message.address, oscFloat.value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OSC message in face tracking manager");
        }
    }
    
    private void ProcessExpressionMessage(string address, float value)
    {
        if (_disposed) return;
        
        // 处理特殊的表情映射
        switch (address)
        {
            case "/mouthFunnel":
            case "/mouthPucker":
                _expressionMap.SetByKey2(address, value * 4f);
                break;
            case "/mouthLeft":
            case "/mouthRight":
                _expressionMap.SetByKey2(address, value * 2f);
                break;
            default:
                if (_expressionMap.ContainsKey2(address))
                {
                    _expressionMap.SetByKey2(address, value);
                }
                break;
        }
    }
    
    public void UpdateExpressionData()
    {
        if (_disposed) return;
        
        try
        {
            // 更新VRCFT表情数据
            foreach (UnifiedExpressions expression in _expressionMap)
            {
                UnifiedTracking.Data.Shapes[(int)expression].Weight = _expressionMap.GetByKey1(expression);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expression data");
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        try
        {
            // 清理资源
            _expressionMap?.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing face tracking manager");
        }
    }
}