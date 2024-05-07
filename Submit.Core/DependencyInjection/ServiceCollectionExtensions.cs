using Tgs.Sms.Submit;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSubmitSms(this IServiceCollection services, Action<IHttpClientBuilder> configureHttpClient)
	{
		configureHttpClient?.Invoke(services.AddHttpClient<SubmitApi>());

		return services.AddSingleton<SubmitSmsHasher>();
	}
}