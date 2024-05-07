using FluentdForward.OpenTelemetry.MessagePack;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Sms.Submit;
using Tgs.Sms.Submit.MessageQueue;

var builder = WebApplication.CreateBuilder(args);
{
	builder.Logging
		.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			options.ParseStateValues = true;
			_ = options.AddFluentdForwardExporter(fluentOptions =>
			{
				var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
				fluentOptions.Host = nodeIp;
				fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
				fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
					"MachineInfo",
					() => new
					{
						Environment.MachineName
					}));
			});
		});

	builder.Services
		.AddHttpClientLoggingHandler()
		.AddBrandServiceClient((options, _) => options.BrandCertificateBaseUrl =
			builder.Configuration.GetValue<Uri>("Submit:BrandCertificateBaseUrl")!)
		.AddSubmitSms(httpBuilder => httpBuilder
			.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
				builder.Configuration.GetValue<Uri>("Submit:ApiBaseUrl")!)
			.AddLoggingHandler<SmsSettings>());

	builder.Services
		.AddMessageQueue()
		.AddRabbitMessageQueue(config => config
			.ConfigQueueOptions((options, _) =>
			{
				builder.Configuration.GetSection("Submit:RabbitMQ").Bind(options);
				options.RabbitMQUrl = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!);

				options.OnSetupQueueAndExchange = channel =>
				{
					channel.QueueDeclare(
						"tgs.sms.submit",
						true,
						false,
						false,
						null);

					channel.QueueBind(
						"tgs.sms.submit",
						"amq.direct",
						"tgs.sms.submit",
						null);
				};
			})
			.AddHandler(ProcessorTypes.SubmitReceiveHandlerType, "tgs.sms.submit", false))
		.AddRabbitGlobPatternExchange("*", "amq.direct");

	builder.Services
		.AddHealthChecks();

	var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
	builder.Services
		.AddOpenTelemetry()
		.WithTracing(tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddSource("Valhalla.*")
			.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !excludeExtensions.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
			.AddProfileViewExporter()
			.AddOtlpExporter())
		.WithMetrics(meterBuiler => meterBuiler
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
				.AddRuntimeInstrumentation()
				.AddAspNetCoreInstrumentation()
				.AddOtlpExporter())
		.Services
		.AddProfileViewer();
}

var app = builder.Build();
{
	app.MapHealthChecks("/healthz");
	app.MapProfileViewer();
}

app.Run();