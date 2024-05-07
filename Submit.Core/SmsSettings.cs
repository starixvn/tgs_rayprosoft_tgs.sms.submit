using System.Text.Json.Serialization;

namespace Tgs.Sms.Submit;
public record SmsSettings(
	[property: JsonPropertyName("orgCode")] string OrgCode,
	[property: JsonPropertyName("md5Key")] string Md5Key);