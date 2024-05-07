using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Sms.Submit;
public record SubmitResponse(
	[property: JsonPropertyName("code")] int Code,
	[property: JsonPropertyName("message")] string Message,
	[property: JsonPropertyName("data")] JsonDocument Data);