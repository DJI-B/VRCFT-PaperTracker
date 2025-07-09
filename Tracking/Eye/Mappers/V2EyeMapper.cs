using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.Models;
using VRCFaceTracking.PaperTracker.Utils;

namespace VRCFaceTracking.PaperTracker.Tracking.Eye.Mappers;

// V2眼部映射器 (从原始V2Mapper改写)
public class V2EyeMapper : BaseEyeMapper
{
    private readonly Dictionary<string, float> _parameterValues = new()
    {
        { "EyeX", 0f },
        { "EyeY", 0f },
        { "EyeLid", 1f },
        { "EyeLeftX", 0f },
        { "EyeLeftY", 0f },
        { "EyeRightX", 0f },
        { "EyeRightY", 0f },
        { "EyeLidLeft", 1f },
        { "EyeLidRight", 1f },
        { "PupilDilation", 0f },
    };
    
    private readonly string[] _singleEyeParams = { "EyeX", "EyeY", "EyeLid" };
    private bool _isSingleEyeMode = false;
    
    public V2EyeMapper(ILogger logger, EyeTrackingConfig config) : base(logger, config) { }
    
    public override void ProcessMessage(OSCMessage message)
    {
        var paramName = GetParameterName(message.address);
        if (!_parameterValues.ContainsKey(paramName)) return;
        
        if (message.value is OSCFloat oscFloat)
        {
            _parameterValues[paramName] = oscFloat.value;
            _isSingleEyeMode = _singleEyeParams.Contains(paramName);
        }
    }
    
    public override void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        // 处理眼部注视
        HandleEyeGaze(ref eyeData);
        
        // 处理瞳孔扩张
        HandleEyeDilation(ref eyeData);
        
        // 处理眼睑开合
        HandleEyeOpenness(ref eyeData);
        
        // 模拟眉毛
        if (_config.ShouldEmulateEyebrows)
        {
            HandleEyebrowEmulation(ref eyeShapes);
        }
    }
    
    private void HandleEyeGaze(ref UnifiedEyeData eyeData)
    {
        if (_isSingleEyeMode)
        {
            var combinedGaze = new Vector2(_parameterValues["EyeX"], _parameterValues["EyeY"]);
            eyeData.Left.Gaze = combinedGaze;
            eyeData.Right.Gaze = combinedGaze;
        }
        else
        {
            eyeData.Left.Gaze = new Vector2(_parameterValues["EyeLeftX"], _parameterValues["EyeLeftY"]);
            eyeData.Right.Gaze = new Vector2(_parameterValues["EyeRightX"], _parameterValues["EyeRightY"]);
        }
    }
    
    private void HandleEyeDilation(ref UnifiedEyeData eyeData)
    {
        eyeData.Left.PupilDiameter_MM = _parameterValues["PupilDilation"];
        eyeData.Right.PupilDiameter_MM = _parameterValues["PupilDilation"];
    }
    
    private void HandleEyeOpenness(ref UnifiedEyeData eyeData)
    {
        if (_isSingleEyeMode)
        {
            var eyeOpenness = (float)_leftOneEuroFilter.Filter(_parameterValues["EyeLid"], 1);
            ProcessSingleEyeOpenness(ref eyeData.Left, eyeOpenness);
            ProcessSingleEyeOpenness(ref eyeData.Right, eyeOpenness);
        }
        else
        {
            var leftOpenness = (float)_leftOneEuroFilter.Filter(_parameterValues["EyeLidLeft"], 1);
            var rightOpenness = (float)_rightOneEuroFilter.Filter(_parameterValues["EyeLidRight"], 1);
            ProcessSingleEyeOpenness(ref eyeData.Left, leftOpenness);
            ProcessSingleEyeOpenness(ref eyeData.Right, rightOpenness);
        }
    }
    
    private void ProcessSingleEyeOpenness(ref UnifiedSingleEyeData eyeData, float baseOpenness)
    {
        eyeData.Openness = baseOpenness;
        
        if (_config.ShouldEmulateEyeWiden && baseOpenness >= _config.WidenThresholdV2[0])
        {
            var widenValue = MathUtils.SmoothStep(
                _config.WidenThresholdV2[0], _config.WidenThresholdV2[1], baseOpenness) * _config.OutputMultiplier;
            eyeData.Openness = baseOpenness + widenValue;
        }
        
        if (_config.ShouldEmulateEyeSquint && baseOpenness <= _config.SqueezeThresholdV2[0])
        {
            var squintValue = MathUtils.SmoothStep(
                _config.SqueezeThresholdV2[0], _config.SqueezeThresholdV2[1], baseOpenness) * _config.OutputMultiplier;
            eyeData.Openness = baseOpenness - squintValue;
        }
    }
    
    private void HandleEyebrowEmulation(ref UnifiedExpressionShape[] eyeShapes)
    {
        if (_isSingleEyeMode)
        {
            var eyeOpenness = _parameterValues["EyeLid"];
            EmulateEyebrow(ref eyeShapes, UnifiedExpressions.BrowLowererLeft, UnifiedExpressions.BrowOuterUpLeft,
                ref _leftOneEuroFilter, eyeOpenness, _config.EyebrowThresholdRising, _config.EyebrowThresholdLowering);
            EmulateEyebrow(ref eyeShapes, UnifiedExpressions.BrowLowererRight, UnifiedExpressions.BrowOuterUpRight,
                ref _rightOneEuroFilter, eyeOpenness, _config.EyebrowThresholdRising, _config.EyebrowThresholdLowering);
        }
        else
        {
            var leftOpenness = _parameterValues["EyeLidLeft"];
            var rightOpenness = _parameterValues["EyeLidRight"];
            EmulateEyebrow(ref eyeShapes, UnifiedExpressions.BrowLowererLeft, UnifiedExpressions.BrowOuterUpLeft,
                ref _leftOneEuroFilter, leftOpenness, _config.EyebrowThresholdRising, _config.EyebrowThresholdLowering);
            EmulateEyebrow(ref eyeShapes, UnifiedExpressions.BrowLowererRight, UnifiedExpressions.BrowOuterUpRight,
                ref _rightOneEuroFilter, rightOpenness, _config.EyebrowThresholdRising, _config.EyebrowThresholdLowering);
        }
    }
}