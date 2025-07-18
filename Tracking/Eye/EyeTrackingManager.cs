using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.Models;
using VRCFaceTracking.PaperTracker.Tracking.Eye.Mappers;

namespace VRCFaceTracking.PaperTracker.Tracking.Eye;

// 眼部追踪管理器
public class EyeTrackingManager : IDisposable
{
    private readonly ILogger _logger;
    private EyeTrackingConfig _config;
    
    private V1EyeMapper _v1Mapper;
    private V2EyeMapper _v2Mapper;
    private BaseEyeMapper _currentMapper;
    
    public EyeTrackingManager(ILogger logger, EyeTrackingConfig config)
    {
        _logger = logger;
        _config = config;
        
        _v1Mapper = new V1EyeMapper(_logger, config);
        _v2Mapper = new V2EyeMapper(_logger, config);
        _currentMapper = _v1Mapper;
    }
    
    public void UpdateConfig(EyeTrackingConfig config)
    {
        _config = config;
        _v1Mapper.UpdateConfig(config);
        _v2Mapper.UpdateConfig(config);
    }
    
    public void ProcessMessage(OSCMessage message)
    {
        if (!message.success) return;
        
        if (IsV2Parameter(message))
        {
            _currentMapper = _v2Mapper;
            _v2Mapper.ProcessMessage(message);
        }
        else
        {
            _currentMapper = _v1Mapper;
            _v1Mapper.ProcessMessage(message);
        }
    }
    
    private bool IsV2Parameter(OSCMessage message)
    {
        return message.address.Contains("/v2/");
    }
    
    public void UpdateVRCFTState()
    {
        _currentMapper.UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
    }
    
    public void Dispose()
    {
        _v1Mapper?.Dispose();
        _v2Mapper?.Dispose();
    }
}