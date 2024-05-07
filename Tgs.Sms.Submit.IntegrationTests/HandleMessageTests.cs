using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.Protos;
using Tgs.Sms.Submit.MessageQueue;
using Valhalla.MessageQueue;
using Xunit.Abstractions;

namespace Tgs.Sms.Submit.IntegrationTests;

public class HandleMessageTests(ITestOutputHelper testOutputHelper)
{
    [Fact(Timeout = 3000)]
    public async Task 處理傳送簡訊要求()
    {
        // Arrange
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var mockHttp = new MockHttpMessageHandler();

        await using var web = SubmitTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeBrandServiceClient)
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddHandler(ProcessorTypes.SubmitReceiveHandlerType, "tgs.sms.submit"))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")
                    .Services
                    .AddHttpClient<SubmitApi>()
                    .ConfigurePrimaryHttpMessageHandler(_ => mockHttp)));

        var brandId = Guid.NewGuid();
        var request = new ShortMessage
        {
            BrandId = brandId,
            Content = "test123",
            Destination = "+15100000044",
            AreaCode = "+151",
            Phone = "00000044"
        };

        var messageSender = web.Services.GetRequiredService<IMessageSender>();

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
            .Returns(new BrandInfo
            {
                Id = brandId,
                SmsEnabled = true
            });

        var vendorInfo = new SmsVendorInfo<SmsSettings>(
            "Submit",
            new SmsSettings("orgCode", "md5Key"));
        _ = fakeBrandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(
            Arg.Is(brandId),
            Arg.Any<JsonSerializerOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(vendorInfo);

        var tcs = new TaskCompletionSource();
        mockHttp.Expect("http://localhost/*")
            .Respond(
            HttpStatusCode.OK,
                _ =>
                {
                    tcs.SetResult();

                    return JsonContent.Create(new SubmitResponse(
                        200,
                        "Submitted successfully",
                        JsonDocument.Parse("""
                            {
                                "sendCode": "20240507154149PIF45"
                            }
                            """)));
                });

        // Act
        await messageSender.PublishAsync<ReadOnlyMemory<byte>>("tgs.sms.submit", request.ToByteArray());
        await tcs.Task;
    }
}