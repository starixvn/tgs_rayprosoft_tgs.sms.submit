using System.Net.Http.Json;

namespace Tgs.Sms.Submit;

internal class SubmitApi(
	HttpClient httpClient,
	SubmitSmsHasher hasher)
{
	public async ValueTask SendMessageAsync(
		string orgCode,
		string md5Key,
		string phone,
		string message,
		CancellationToken cancellationToken = default)
	{
		var rand = DateTimeOffset.UtcNow.ToString("HHmmss");
		var sign = hasher.ComputeHash(orgCode, message, rand, md5Key);

		var dict = new Dictionary<string, string?>
		{
			["orgCode"] = orgCode,
			["mobiles"] = phone,
			["content"] = message,
			["rand"] = rand,
			["sign"] = sign,
		};

		var response = await httpClient.PostAsJsonAsync("/v2/api/sendSMS", dict, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<SubmitResponse>(cancellationToken: cancellationToken)
				.ToAsyncEnumerable()
				.Select(res => res!))
			.FirstAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (response.Code != 200)
			throw new Exception("Send message fail.");
	}
}