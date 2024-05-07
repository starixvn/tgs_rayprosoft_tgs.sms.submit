using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Sms.Submit;

namespace Submit.Core.UnitTests;

public class SubmitApiTests
{
    [Fact]
    public async Task 傳送簡訊()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var hasher = new SubmitSmsHasher(NullLogger<SubmitSmsHasher>.Instance);

        var sut = new SubmitApi(httpClient, hasher);

        var orgCode = "orgCode";
        var md5Key = "md5Key";
        var phone = "+85291200539";
        var message = "Verification code is 123456, it will expire after 10 minutes.";

        mockHttp.Expect("http://localhost/*")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 200,
                    "data": {
                        "sendCode": "20240507154149PIF45"
                    },
                    "message": "Submitted successfully"
                }
                """));

        // Act
        await sut.SendMessageAsync(orgCode, md5Key, phone, message);
    }
}