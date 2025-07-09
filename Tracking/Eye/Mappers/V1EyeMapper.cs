using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.Models;
using VRCFaceTracking.PaperTracker.Utils;

namespace VRCFaceTracking.PaperTracker.Tracking.Eye.Mappers;

// V1眼部映射器 (从原始V1Mapper改写)
public class V1EyeMapper : BaseEyeMapper
{
    private readonly Dictionary<string, float> _parameterValues = new()
    {
        { "RightEyeLidExpandedSqueeze", 1f },
        { "LeftEyeLidExpandedSqueeze", 1f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesDilation", 0f },
        { "EyesY", 0f },
    };
    
    public V1EyeMapper(ILogger logger, EyeTrackingConfig config) : base(logger, config) { }
    
    public override void ProcessMessage(OSCMessage message)
    {
        var paramName = GetParameterName(message.address);
        if (!_parameterValues.ContainsKey(paramName)) return;
        
        if (message.value is OSCFloat oscFloat)
        {
            _parameterValues[paramName] = oscFloat.value;
        }
    }
    
    public override void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        // 处理眼部注视
        HandleEyeGaze(ref eyeData);
        
        // 处理瞳孔扩张
        HandleEyeDilation(ref eyeData);
        
        // 处理眼睑开合
        HandleEyeOpenness(ref eyeData, ref eyeShapes);
        
        // 模拟眉毛
        if (_config.ShouldEmulateEyebrows)
        {
            HandleEyebrowEmulation(ref eyeShapes);
        }
    }
    
    private void HandleEyeGaze(ref UnifiedEyeData eyeData)
    {
        eyeData.Right.Gaze = new Vector2(_parameterValues["RightEyeX"], _parameterValues["EyesY"]);
        eyeData.Left.Gaze = new Vector2(_parameterValues["LeftEyeX"], _parameterValues["EyesY"]);
    }
    
    private void HandleEyeDilation(ref UnifiedEyeData eyeData)
    {
        eyeData.Left.PupilDiameter_MM = _parameterValues["EyesDilation"];
        eyeData.Right.PupilDiameter_MM = _parameterValues["EyesDilation"];
    }
    
    private void HandleEyeOpenness(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        var leftOpenness = (float)_leftOneEuroFilter.Filter(_parameterValues["LeftEyeLidExpandedSqueeze"], 1);
        var rightOpenness = (float)_rightOneEuroFilter.Filter(_parameterValues["RightEyeLidExpandedSqueeze"], 1);
        
        ProcessSingleEyeOpenness(ref eyeData.Left, ref eyeShapes, 
            UnifiedExpressions.EyeWideLeft, UnifiedExpressions.EyeSquintLeft, leftOpenness);
        ProcessSingleEyeOpenness(ref eyeData.Right, ref eyeShapes, 
            UnifiedExpressions.EyeWideRight, UnifiedExpressions.EyeSquintRight, rightOpenness);
    }
    
    private void ProcessSingleEyeOpenness(ref UnifiedSingleEyeData eye, ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions widenParam, UnifiedExpressions squintParam, float baseOpenness)
    {
        eye.Openness = baseOpenness;
        
        if (_config.ShouldEmulateEyeWiden && baseOpenness >= _config.WidenThresholdV1[0])
        {
            eye.Openness = 0.8f;
            var widenValue = MathUtils.SmoothStep(
                _config.WidenThresholdV1[0], _config.WidenThresholdV1[1], baseOpenness) * _config.OutputMultiplier;
            eyeShapes[(int)widenParam].Weight = widenValue;
        }
        else
        {
            eyeShapes[(int)widenParam].Weight = 0;
        }
        
        if (_config.ShouldEmulateEyeSquint && baseOpenness <= _config.SqueezeThresholdV1[0])
        {
            var squintValue = MathUtils.SmoothStep(
                _config.SqueezeThresholdV1[1], _config.SqueezeThresholdV1[0], baseOpenness) * _config.OutputMultiplier;
            eyeShapes[(int)squintParam].Weight = squintValue;
        }
        else
        {
            eyeShapes[(int)squintParam].Weight = 0;
        }
    }
    
    private void HandleEyebrowEmulation(ref UnifiedExpressionShape[] eyeShapes)
    {
        var leftOpenness = _parameterValues["LeftEyeLidExpandedSqueeze"];
        var rightOpenness = _parameterValues["RightEyeLidExpandedSqueeze"];
        
        EmulateEyebrow(ref eyeShapes, UnifiedExpressions.BrowLowererLeft, UnifiedExpressions.BrowOuterUpLeft,
            ref _leftOneEuroFilter, leftOpenness, _config.EyebrowThresholdRising, _config.EyebrowThresholdLowering);
        EmulateEyebrow(ref eyeShapes, UnifiedExpressions.BrowLowererRight, UnifiedExpressions.BrowOuterUpRight,
            ref _rightOneEuroFilter, rightOpenness, _config.EyebrowThresholdRising, _config.EyebrowThresholdLowering);
    }
}