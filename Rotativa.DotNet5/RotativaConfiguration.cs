using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Rotativa.Infrastructure;

namespace Rotativa;

public static class RotativaConfiguration
{
	public static void AddRotative(this IServiceCollection services)
	{
		services.AddSingleton(sp => new RotativaPath(sp.GetRequiredService<IWebHostEnvironment>( )));
		services.AddScoped<IRotativaConverter, RotativaConverter>( );
	}
}
