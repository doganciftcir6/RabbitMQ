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
                    // SqlServer baðlantýsýný burada yap
                    services.AddDbContext<AdventureWorks2022Context>(opt =>
                    {
                        opt.UseSqlServer(Configuration.GetConnectionString("SqlServer"));
                    });
                    // Redis için ConnectionFactory bize DI'dan gelsin
                    // Singleton yaptýðýmýzdan uygulama ayaða kalktýðýnda ConnectionFactory'dan bir tane nesne örneði gelecek, Dependency yani ctorlara burada DI'da oluþan nesne örneði gidecek, iþlemlerde async kullandýðýmýz için DispatchConsumersAsync = true
                    services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(Configuration.GetConnectionString("RabbitMQ")), DispatchConsumersAsync = true });
                    // Hazýrladýðýmýz RabbitMqClient classýný service olarak ekle
                    services.AddSingleton<RabbitMQClientService>();

                    services.AddHostedService<Worker>();
                });
    }
}
