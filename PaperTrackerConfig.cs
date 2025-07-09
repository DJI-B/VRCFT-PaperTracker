using Newtonsoft.Json;
using System.Reflection;

namespace VRCFaceTracking.PaperTracker;

public static class PaperTrackerConfig
{
	private const string PaperTrackerConfigFile = "PaperTrackerConfig.json";

	public static Config GetPaperTrackerConfig()
	{
        string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
		string path = Path.Combine(directoryName, PaperTrackerConfigFile);
		string value = File.ReadAllText(path);
		return JsonConvert.DeserializeObject<Config>(value)!;
	}
}
