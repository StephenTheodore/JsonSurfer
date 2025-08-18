using Microsoft.Extensions.DependencyInjection;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Services;
using JsonSurfer.App.ViewModels;

namespace JsonSurfer.App.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonSurferServices(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IJsonParserService, JsonParserService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IFileService, FileService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<CompareViewModel>();

        return services;
    }
}