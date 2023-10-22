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
        // RabbitMQ'ya ba�lanmak i�in, DI Container'a bunu Singleton olarak ekledi�im i�in bunu direkt alabilirim
        private readonly RabbitMQClientService _rabbitmqClientService;
        // Dbcontext DI Container'a scoped olarak eklendi BackgroundService'im ise singleton dolays��yla bunun i�erisinde bunu direkt Dependency olarak alamam. ServieProvider �zerinden contexti alaca��z
        private readonly IServiceProvider _serviceProvider;
        // Channeli rabbitmq'ya ba�lanma s�ras�nda dolduraca��z, bu ortak oldu�u i�in bu class i�inde bir �ok yerde kullanaca��m�z i�in burada tan�ml�yoruz
        private IModel _channel;
        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitmqClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitmqClientService = rabbitmqClientService;
            _serviceProvider = serviceProvider;
        }
        // Start metodunda RabbiMQ'ya ba�lanaca��z
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitmqClientService.Connect();
            // Sen bana Subscriber olarak messageleri bir bir g�nder diyoruz
            _channel.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Consumer olu�tur
            var consumer = new AsyncEventingBasicConsumer(_channel);
            // Hangi kuyru�u dinleyece�ini belirt
            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);
            // Consumer �zerinden eventi yakala
            consumer.Received += Consumer_Received;
            return Task.CompletedTask;
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            // Excel'e d�n��t�rme i�lemi �ok h�zl� olacak g�rmek a��s�ndan biraz gecikme yapabiliriz
            await Task.Delay(5000);
            // Kuyruktan messageyi al
            var createdExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));
            // Excel dosyas�n� olu�turaca��m ama bunu MemoryStream'e atmam laz�m ��nk� olu�turdu�um Exceli memoryde bir stream olarak tutaca��m
            using var ms = new MemoryStream();
            // Bir xml �al��ma kitapi olu�tur
            var wb = new XLWorkbook();
            // Bir dataset olu�tural�m bunu veritaban� gibi d���nebiliriz veritaban�na tablomuzu ekleyece�iz
            var ds = new DataSet();
            // Excel olu�turma metotudnan gelen tablomu datasete ekleyeyim ��nk� birden fazla olabilir
            ds.Tables.Add(GetTable("products"));
            // Art�k tabloyu olu�turmas� i�in
            wb.Worksheets.Add(ds);
            // Olu�turmu� oldu�un Excel dosyas�n� nereye kaydet, memory stream'e
            wb.SaveAs(ms);
            // Excel dosyas� �uan bellekte �nce endpointte beklenilen IFormFile nesnesini g�nderece�iz
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid().ToString() + ".xlsx");
            var baseUrl = "https://localhost:44307/api/files";
            using (var httpClient = new HttpClient())
            {
                // Art�k Endpointe iste�i atabiliriz
                var response = await httpClient.PostAsync($"{baseUrl}?fileId={createdExcelMessage.FileId}", multipartFormDataContent);
                if (response.IsSuccessStatusCode)
                {
                    // Ba�ar�l� i�lemse rabbitmq'ya messageyi sil diyelim
                    _logger.LogInformation($"File ( Id = {createdExcelMessage.FileId} was created by successful)");
                    _channel.BasicAck(@event.DeliveryTag, false);
                }
            }
        }

        // Excel olu�turma i�lemini ayr� bir metotta yapmak istiyorum
        // Memoryde DataTable ile bir tablo olu�turup bunu geri d�nece�iz sonra k�t�phane ile bu tablodan excel tablosu olu�turaca��z
        private DataTable GetTable(string tableName)
        {
            // Bu tablo List<Product>'dan olu�acak
            List<Product> products;
            // Db'ye ba�lanal�m
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2022Context>();
                products = context.Products.ToList();
            }
            // Productslar�m doldu ve context bellekten d��t�
            // DataTable yani Memoryde bir tablolu�tur ve ismini ver
            DataTable dataTable = new DataTable() { TableName = tableName };
            // Tablonun s�tun isimlerini ekle
            dataTable.Columns.Add("ProductId", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("ProductNumber", typeof(string));
            dataTable.Columns.Add("Color", typeof(string));
            // �imdi tablonun sat�rlar�na veri ekle
            products.ForEach(p =>
            {
                dataTable.Rows.Add(p.ProductId, p.Name, p.ProductNumber, p.Color);
            });
            // Art�k Memoryde bu tablom var
            return dataTable;
        }
    }
}
