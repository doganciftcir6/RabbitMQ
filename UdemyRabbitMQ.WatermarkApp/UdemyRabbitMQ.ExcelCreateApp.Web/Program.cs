using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UdemyRabbitMQ.ExcelCreateApp.Web.Models;

namespace UdemyRabbitMQ.ExcelCreateApp.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Uygulama aya�a kalkarken otomatik migration yaps�n ve seed data eklesin istiyorum
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                // scope olu�turma sebebi i�lem bittikten sonra eldeki serviceler memoryden d��s�n gereksiz yere durmas�n diye
                // scope �zerinden startup taraf�nda eklemi� oldu�um servicelere eri�ece�im
                // GetRequiredService ve GetService fark� Required e�er bu service'i bulamazsa hata f�rlat�r o y�zden bu servisin mutlaka var oldu�unu bildi�imizde kullan�r�z, GetService ise e�er service'i bulamazsa geriye null d�ner
                // update-database yap�lmad�ysa oto kendisi yapmas� i�in appDbContext
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // User tablosuna seed data ekleyece�i�iz i�in UserManager'a ihtiya� var
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                // Elde bir migration varsa bunu uygulas�n
                appDbContext.Database.Migrate();
                // Seed data
                if (!appDbContext.Users.Any())
                {
                    userManager.CreateAsync(new IdentityUser() { UserName = "deneme", Email = "deneme@outlook.com" }, "Password12*").Wait();
                    userManager.CreateAsync(new IdentityUser() { UserName = "deneme2", Email = "deneme2@outlook.com" }, "Password12*").Wait();
                }
            }
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
