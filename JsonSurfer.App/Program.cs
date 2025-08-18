using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JsonSurfer.App.DependencyInjection;

namespace JsonSurfer.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Build the host
        var host = CreateHostBuilder(args).Build();
        
        // Create and configure the WPF app
        var app = new App();
        app.ConfigureServices(host.Services);
        
        // Run the app
        app.Run();
        
        // Clean up
        host.Dispose();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<MainWindow>();
                services.AddJsonSurferServices();
            });
}