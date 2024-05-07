using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Sms.Submit;

internal class SubmitSmsHasher(ILogger<SubmitSmsHasher> logger)
{
	private readonly ILogger<SubmitSmsHasher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public string ComputeHash(string orgCode, string content, string rand, string md5Key)
	{
		var fullText = $"{orgCode}{content}{rand}{md5Key}";

		_logger.LogTrace("FullText: {fullText}", fullText);

		var data = MD5.HashData(Encoding.UTF8.GetBytes(fullText));

		return Convert.ToHexString(data).ToUpper();
	}
}