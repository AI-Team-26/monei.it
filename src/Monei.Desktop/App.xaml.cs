using System.IO;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Monei.Core.Configuration;
using Monei.Core.Data;
using Monei.Core.Repositories;

namespace Monei.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = CreateHostBuilder().Build();
        ConfigureServices(_host.Services);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(basePath);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context.Configuration);
            });
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration != null)
        {
            services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));

            var mongoSettings = configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>()
                ?? new MongoDbSettings();

            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.DatabaseName));

            var sqlServerSettings = configuration.GetSection(SqlServerSettings.SectionName).Get<SqlServerSettings>()
                ?? new SqlServerSettings();

            // The connection string is empty until the "SqlServer" section is added to appsettings.json.
            services.AddSingleton(_ => new MoneiDbContext(sqlServerSettings.ConnectionString));
        }

        services.AddTransient<RegistryEntryRepository>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();
    }

    private static void ConfigureServices(IServiceProvider serviceProvider)
    {
        Ioc.Default.ConfigureServices(serviceProvider);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}