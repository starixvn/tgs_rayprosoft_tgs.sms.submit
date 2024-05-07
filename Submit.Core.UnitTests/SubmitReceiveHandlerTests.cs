using System.Net;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.Protos;
using Tgs.Sms.Submit;
using Tgs.Sms.Submit.MessageQueue;

namespace Submit.Core.UnitTests;

public class SubmitReceiveHandlerTests
{
    [Fact]
    public async Task 接收到發送簡訊要求()
    {
        // Arrange
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var hasher = new SubmitSmsHasher(NullLogger<SubmitSmsHasher>.Instance);

        var sut = new SubmitReceiveHandler(
            fakeBrandServiceClient,
            new SubmitApi(httpClient, hasher),
            NullLogger<SubmitReceiveHandler>.Instance);

        var brandId = Guid.NewGuid();
        var request = new ShortMessage
        {
            BrandId = brandId,
            Content = "Verification code is 123456, it will expire after 10 minutes.",
            Destination = "+85291200539",
            AreaCode = "+852",
            Phone = "91200539"
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(new BrandInfo
            {
                Id = brandId,
                SmsEnabled = true
            });

        var vendorInfo = new SmsVendorInfo<SmsSettings>(
            "Test",
            new SmsSettings("orgCode", "md5Key"));
        _ = fakeBrandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(Arg.Is(brandId))
            .Returns(vendorInfo);

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
        await sut.HandleAsync("test", request.ToByteArray(), null);
    }
}