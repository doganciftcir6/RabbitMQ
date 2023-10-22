using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UdemyRabbitMQ.ExcelCreateApp.Web.Hubs;
using UdemyRabbitMQ.ExcelCreateApp.Web.Models;
using UdemyRabbitMQ.ExcelCreateApp.Web.Services;

namespace UdemyRabbitMQ.ExcelCreateApp.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Redis i�in ConnectionFactory bize DI'dan gelsin
            // Singleton yapt���m�zdan uygulama aya�a kalkt���nda ConnectionFactory'dan bir tane nesne �rne�i gelecek, Dependency yani ctorlara burada DI'da olu�an nesne �rne�i gidecek, i�lemlerde async kulland���m�z i�in DispatchConsumersAsync = true
            services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(Configuration.GetConnectionString("RabbitMQ")), DispatchConsumersAsync = true });
            // Haz�rlad���m�z RabbitMqClient class�n� service olarak ekle
            services.AddSingleton<RabbitMqClientService>();
            // Haz�rlad���m�z RabbitMQPublisher class�n� service olarak ekle, dependency i�in
            services.AddSingleton<RabbitMQPublisher>();

            // Contexti dependency olarak ge�ebilmek i�in service olarak ekle, dependency i�in
            // Contextin options'unu burada doldur
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SqlServer"));
            });
            // Identityi service olarak ekle
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            }).AddEntityFrameworkStores<AppDbContext>();

            services.AddControllersWithViews();

            //SignalR'i service olarak ekle
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // UseAuthentication ekle
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // SignalR i�in Hub'�ma hangi endpoint �zerinden eri�ecek belirtmeliyiz
                endpoints.MapHub<MyHub>("/MyHub");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
