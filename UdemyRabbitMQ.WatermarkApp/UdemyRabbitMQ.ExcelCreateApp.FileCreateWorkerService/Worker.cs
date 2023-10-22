using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService.Models;
using UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService.Services;
using UdemyRabbitMQExcelCreateApp.Shared.Messages;

namespace UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        // RabbitMQ'ya baðlanmak için, DI Container'a bunu Singleton olarak eklediðim için bunu direkt alabilirim
        private readonly RabbitMQClientService _rabbitmqClientService;
        // Dbcontext DI Container'a scoped olarak eklendi BackgroundService'im ise singleton dolaysýýyla bunun içerisinde bunu direkt Dependency olarak alamam. ServieProvider üzerinden contexti alacaðýz
        private readonly IServiceProvider _serviceProvider;
        // Channeli rabbitmq'ya baðlanma sýrasýnda dolduracaðýz, bu ortak olduðu için bu class içinde bir çok yerde kullanacaðýmýz için burada tanýmlýyoruz
        private IModel _channel;
        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitmqClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitmqClientService = rabbitmqClientService;
            _serviceProvider = serviceProvider;
        }
        // Start metodunda RabbiMQ'ya baðlanacaðýz
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitmqClientService.Connect();
            // Sen bana Subscriber olarak messageleri bir bir gönder diyoruz
            _channel.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Consumer oluþtur
            var consumer = new AsyncEventingBasicConsumer(_channel);
            // Hangi kuyruðu dinleyeceðini belirt
            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);
            // Consumer üzerinden eventi yakala
            consumer.Received += Consumer_Received;
            return Task.CompletedTask;
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            // Excel'e dönüþtürme iþlemi çok hýzlý olacak görmek açýsýndan biraz gecikme yapabiliriz
            await Task.Delay(5000);
            // Kuyruktan messageyi al
            var createdExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));
            // Excel dosyasýný oluþturacaðým ama bunu MemoryStream'e atmam lazým çünkü oluþturduðum Exceli memoryde bir stream olarak tutacaðým
            using var ms = new MemoryStream();
            // Bir xml çalýþma kitapi oluþtur
            var wb = new XLWorkbook();
            // Bir dataset oluþturalým bunu veritabaný gibi düþünebiliriz veritabanýna tablomuzu ekleyeceðiz
            var ds = new DataSet();
            // Excel oluþturma metotudnan gelen tablomu datasete ekleyeyim çünkü birden fazla olabilir
            ds.Tables.Add(GetTable("products"));
            // Artýk tabloyu oluþturmasý için
            wb.Worksheets.Add(ds);
            // Oluþturmuþ olduðun Excel dosyasýný nereye kaydet, memory stream'e
            wb.SaveAs(ms);
            // Excel dosyasý þuan bellekte önce endpointte beklenilen IFormFile nesnesini göndereceðiz
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid().ToString() + ".xlsx");
            var baseUrl = "https://localhost:44307/api/files";
            using (var httpClient = new HttpClient())
            {
                // Artýk Endpointe isteði atabiliriz
                var response = await httpClient.PostAsync($"{baseUrl}?fileId={createdExcelMessage.FileId}", multipartFormDataContent);
                if (response.IsSuccessStatusCode)
                {
                    // Baþarýlý iþlemse rabbitmq'ya messageyi sil diyelim
                    _logger.LogInformation($"File ( Id = {createdExcelMessage.FileId} was created by successful)");
                    _channel.BasicAck(@event.DeliveryTag, false);
                }
            }
        }

        // Excel oluþturma iþlemini ayrý bir metotta yapmak istiyorum
        // Memoryde DataTable ile bir tablo oluþturup bunu geri döneceðiz sonra kütüphane ile bu tablodan excel tablosu oluþturacaðýz
        private DataTable GetTable(string tableName)
        {
            // Bu tablo List<Product>'dan oluþacak
            List<Product> products;
            // Db'ye baðlanalým
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2022Context>();
                products = context.Products.ToList();
            }
            // Productslarým doldu ve context bellekten düþtü
            // DataTable yani Memoryde bir tabloluþtur ve ismini ver
            DataTable dataTable = new DataTable() { TableName = tableName };
            // Tablonun sütun isimlerini ekle
            dataTable.Columns.Add("ProductId", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("ProductNumber", typeof(string));
            dataTable.Columns.Add("Color", typeof(string));
            // Þimdi tablonun satýrlarýna veri ekle
            products.ForEach(p =>
            {
                dataTable.Rows.Add(p.ProductId, p.Name, p.ProductNumber, p.Color);
            });
            // Artýk Memoryde bu tablom var
            return dataTable;
        }
    }
}
