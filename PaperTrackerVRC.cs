using System.Reflection;
using VRCFaceTracking.Core.Params.Expressions;

namespace VRCFaceTracking.PaperTracker;

public class PaperTrackerVRC : ExtTrackingModule
{
    private PaperTrackerOSC PaperTrackerOSC;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (false, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        Config papertracker_config = PaperTrackerConfig.GetPaperTrackerConfig();
        PaperTrackerOSC = new PaperTrackerOSC(logger: Logger, papertracker_config.Host, papertracker_config.Port);
        List<Stream> list = new List<Stream>();
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("VRCFaceTracking.PaperTracker.PaperTrackerLogo.png")!;
        list.Add(manifestResourceStream);
        ModuleInformation = new ModuleMetadata
        {
            Name = "Project PaperTracker Module v2.1.2",
            StaticImages = list
        };
        return (false, true);
    }

    public override void Teardown()
    {
        PaperTrackerOSC.Teardown();
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
