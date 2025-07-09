using System.Net;
using Newtonsoft.Json;

namespace VRCFaceTracking.PaperTracker.Utils;

public class IPAddressNewtonsoftConverter : JsonConverter<IPAddress>
{
    public override void WriteJson(JsonWriter writer, IPAddress? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        
        writer.WriteValue(value.ToString());
    }

    public override IPAddress? ReadJson(JsonReader reader, Type objectType, IPAddress? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return IPAddress.Any;
        }
        
        if (reader.TokenType == JsonToken.String)
        {
            var value = reader.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return IPAddress.Any;
            }
            
            if (IPAddress.TryParse(value, out var address))
            {
                return address;
            }
        }
        
        return IPAddress.Any;
    }
}