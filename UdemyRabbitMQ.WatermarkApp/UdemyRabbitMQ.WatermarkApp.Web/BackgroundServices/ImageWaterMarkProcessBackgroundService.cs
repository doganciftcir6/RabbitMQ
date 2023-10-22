using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UdemyRabbitMQ.WatermarkApp.Web.Services;

namespace UdemyRabbitMQ.WatermarkApp.Web.BackgroundServices
{
    public class ImageWaterMarkProcessBackgroundService : BackgroundService
    {
        // RabbitMQ Kanalı almam lazım
        private readonly RabbitMqClientService _rabbitMqClientService;
        private readonly ILogger<ImageWaterMarkProcessBackgroundService> _logger;
        // Kanalı oluştur, bunu ctorda değil farklı bir yerde set edicem o yüzden readonly demiyorum
        private IModel _channel;
        public ImageWaterMarkProcessBackgroundService(RabbitMqClientService rabbitMqClientService, ILogger<ImageWaterMarkProcessBackgroundService> logger)
        {
            _rabbitMqClientService = rabbitMqClientService;
            _logger = logger;
        }

        // İmplement zorunlu değil virtual metot
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // RabbitMQ'ya bağlan ve Kanalı al
            _channel = _rabbitMqClientService.Connect();
            // Messageleri kaçar kaçar alacağımızı belirtelim, 1'er
            _channel.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }
        // İmplement zorunlu yani Abstract metot
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Cunsomer oluştur
            var consumer = new AsyncEventingBasicConsumer(_channel);
            // Kanalı oku, resmi başarılı bir şekilde işlersem ilgili messageyi silmesi için rabbitmqya haber vereceğim autoAck
            _channel.BasicConsume(queue: RabbitMqClientService.QueueName, autoAck: false, consumer: consumer);
            // Artık eventi dinleyebilirim
            consumer.Received += Consumer_Received;
            // İşlemimiz tamamlandı diyelim
            return Task.CompletedTask;
        }

        private Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                // Resime yazı ekleme olayını burada gerçekleştireceğiz
                // Eventi al Byte[] gelen datayı stringe dömüştür sonra onuda classa
                var productImageCreatedEvent = JsonSerializer.Deserialize<ProductImageCreatedEvent>(Encoding.UTF8.GetString(@event.Body.ToArray()));
                // Resmi çek
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", productImageCreatedEvent.ImageName);
                var siteName = "www.mysite.com";
                // Watermark kullanmakla ilgili hazır kütüphanelerde var bu kadar detaylı olmayan onları kullanmak daha faydalı olabilir bu noktada
                using var img = Image.FromFile(path);
                // Resim geldi yazı yazabilmem için graphic oluştur
                using var graphic = Graphics.FromImage(img);
                var font = new Font(FontFamily.GenericMonospace, 40, FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = graphic.MeasureString(siteName, font);
                var color = Color.FromArgb(128, 255, 255, 255);
                var brush = new SolidBrush(color);
                var position = new Point(img.Width - ((int)textSize.Width + 30), img.Height - ((int)textSize.Height + 30));
                graphic.DrawString(siteName, font, brush, position);
                img.Save("wwwroot/images/watermarks/" + productImageCreatedEvent.ImageName);
                // Bellekten silelim, dispose ile
                img.Dispose();
                graphic.Dispose();
                // İşlemenin başırılı olduğu bilgisini RabbiMQ'ya ver o da messageyi kuyruktan silsin
                _channel.BasicAck(@event.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                // Messageyi işleme başarılı değil RabbitMq'ya haber gitmeyecek ve message kuyruktan silinmeyecek
                _logger.LogError(ex.Message);
            }
            // İşlemin bittiğini haber edelim
            return Task.CompletedTask;
        }

        // İmplement zorunlu değil virtual metot
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
