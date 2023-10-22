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
            // Uygulama ayaða kalkarken otomatik migration yapsýn ve seed data eklesin istiyorum
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                // scope oluþturma sebebi iþlem bittikten sonra eldeki serviceler memoryden düþsün gereksiz yere durmasýn diye
                // scope üzerinden startup tarafýnda eklemiþ olduðum servicelere eriþeceðim
                // GetRequiredService ve GetService farký Required eðer bu service'i bulamazsa hata fýrlatýr o yüzden bu servisin mutlaka var olduðunu bildiðimizde kullanýrýz, GetService ise eðer service'i bulamazsa geriye null döner
                // update-database yapýlmadýysa oto kendisi yapmasý için appDbContext
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // User tablosuna seed data ekleyeceðiöiz için UserManager'a ihtiyaç var
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                // Elde bir migration varsa bunu uygulasýn
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
