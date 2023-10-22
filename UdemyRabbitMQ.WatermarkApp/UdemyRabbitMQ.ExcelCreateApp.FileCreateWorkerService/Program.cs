using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService.Models;
using UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService.Services;

namespace UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // DI Container burada
                    IConfiguration Configuration = hostContext.Configuration;
                    // SqlServer ba�lant�s�n� burada yap
                    services.AddDbContext<AdventureWorks2022Context>(opt =>
                    {
                        opt.UseSqlServer(Configuration.GetConnectionString("SqlServer"));
                    });
                    // Redis i�in ConnectionFactory bize DI'dan gelsin
                    // Singleton yapt���m�zdan uygulama aya�a kalkt���nda ConnectionFactory'dan bir tane nesne �rne�i gelecek, Dependency yani ctorlara burada DI'da olu�an nesne �rne�i gidecek, i�lemlerde async kulland���m�z i�in DispatchConsumersAsync = true
                    services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(Configuration.GetConnectionString("RabbitMQ")), DispatchConsumersAsync = true });
                    // Haz�rlad���m�z RabbitMqClient class�n� service olarak ekle
                    services.AddSingleton<RabbitMQClientService>();

                    services.AddHostedService<Worker>();
                });
    }
}
