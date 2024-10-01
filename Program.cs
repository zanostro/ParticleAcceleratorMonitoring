using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Windows.Forms;

namespace ParticleAcceleratorMonitoring
{
    public class Program
    {
        public static IConfiguration? Configuration { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            // Save path for logger
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string SavePath = Path.Combine(documentsPath, "ParticleAcceleratorMonitoring");
            SavePath = Path.Combine(SavePath, "log.txt");

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(SavePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("------------------------------------------------------------------");
            logger.LogInformation("Application starting...");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(host.Services.GetRequiredService<MonitoringService>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while starting the application.");
                throw;
            }
            finally
            {
                logger.LogInformation("Application shutting down...");
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() 
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    Configuration = config.Build();
                })
                .ConfigureServices((context, services) =>
                {
                    // Register application services
                    services.AddSingleton<MonitoringService>();
                    services.AddSingleton<Archiver>();
                    services.AddSingleton<Client>();
                    services.AddSingleton<TemperatureSensor>();
                    services.AddSingleton<RadiationSensor>();
                    services.AddSingleton<PressureSensor>();
                    services.AddSingleton<HumiditySensor>();
                });
    }
}