using System.Net;

namespace VRCFaceTracking.PaperTracker.Utils;

public static class Validators
{
    public static bool CheckIfIPAddress(string value)
    {
        IPAddress? result;
        return !String.IsNullOrEmpty(value) && IPAddress.TryParse(value, out result);
    }
}