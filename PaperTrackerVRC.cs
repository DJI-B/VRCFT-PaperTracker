using System.Reflection;
using VRCFaceTracking.Core.Params.Expressions;

namespace VRCFaceTracking.PaperTracker;

public class PaperTrackerVRC : ExtTrackingModule
{
    private PaperFaceTrackerOSC? paper_face_tracker_osc;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (false, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        Config papertracker_config = PaperTrackerConfig.GetPaperTrackerConfig();
        paper_face_tracker_osc = new PaperFaceTrackerOSC(logger: Logger, papertracker_config.FaceHost, papertracker_config.FacePort);
        List<Stream> list = new();
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("VRCFaceTracking.PaperTracker.PaperTrackerLogo.png")!;
        list.Add(manifestResourceStream);
        ModuleInformation = new ModuleMetadata
        {
            Name = "Project PaperTracker Module v0.0.1",
            StaticImages = list
        };
        return (false, true);
    }
    
    public override void Teardown()
    {
        paper_face_tracker_osc?.Teardown();
    }

    public override void Update()
    {
        foreach (UnifiedExpressions expression in PaperTrackerExpressions.PaperTrackerExpressionMap)
        {
            UnifiedTracking.Data.Shapes[(int)expression].Weight = PaperTrackerExpressions.PaperTrackerExpressionMap.GetByKey1(expression);
        }
        Thread.Sleep(10);
    }
}
