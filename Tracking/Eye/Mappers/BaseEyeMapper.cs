using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.Filters;
using VRCFaceTracking.PaperTracker.Core.Models;
using VRCFaceTracking.PaperTracker.Utils;

namespace VRCFaceTracking.PaperTracker.Tracking.Eye.Mappers;

// 基础眼部映射器
public abstract class BaseEyeMapper : IDisposable
{
    protected readonly ILogger _logger;
    protected EyeTrackingConfig _config;
    
    protected OneEuroFilter _leftOneEuroFilter;
    protected OneEuroFilter _rightOneEuroFilter;
    
    public BaseEyeMapper(ILogger logger, EyeTrackingConfig config)
    {
        _logger = logger;
        _config = config;
        
        _leftOneEuroFilter = new OneEuroFilter(minCutoff: 0.1f, beta: 15.0f);
        _rightOneEuroFilter = new OneEuroFilter(minCutoff: 0.1f, beta: 15.0f);
    }
    
    public virtual void UpdateConfig(EyeTrackingConfig config)
    {
        _config = config;
    }
    
    public abstract void ProcessMessage(OSCMessage message);
    
    public abstract void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes);
    
    protected static string GetParameterName(string oscAddress)
    {
        var parts = oscAddress.Split('/');
        return parts[^1];
    }
    
    protected void EmulateEyebrow(
        ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions eyebrowLowerer,
        UnifiedExpressions eyebrowUpper,
        ref OneEuroFilter filter,
        float baseEyeOpenness,
        float riseThreshold,
        float lowerThreshold)
    {
        if (!_config.ShouldEmulateEyebrows) return;
        
        var filteredOpenness = (float)filter.Filter(baseEyeOpenness, 1);
        
        if (filteredOpenness >= riseThreshold)
        {
            eyeShapes[(int)eyebrowUpper].Weight = MathUtils.SmoothStep(
                riseThreshold, 1, filteredOpenness);
        }
        
        if (filteredOpenness <= lowerThreshold)
        {
            eyeShapes[(int)eyebrowLowerer].Weight = MathUtils.SmoothStep(
                lowerThreshold, 1, filteredOpenness);
        }
    }
    
    public virtual void Dispose()
    {
        // 清理资源
    }
}