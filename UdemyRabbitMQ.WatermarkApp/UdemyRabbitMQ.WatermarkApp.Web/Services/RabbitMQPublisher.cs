using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace UdemyRabbitMQ.WatermarkApp.Web.Services
{
    public class RabbitMQPublisher
    {
        // Clienta bağlanması lazım
        private readonly RabbitMqClientService _rabbitMqClientService;
        public RabbitMQPublisher(RabbitMqClientService rabbitMqClientService)
        {
            _rabbitMqClientService = rabbitMqClientService;
        }

        // Rabbitmqya Event gönderecek olan metot
        public void Publish(ProductImageCreatedEvent productImageCreatedEvent)
        {
            // RabbitMQ'ya bağlan ve kanalı al
            var channel = _rabbitMqClientService.Connect();
            // RabbitMQ'ya göderilecek olan messageyi stringe çevir
            var bodyString = JsonSerializer.Serialize(productImageCreatedEvent);
            // Stringi Byte[] çevir
            var bodyByte = Encoding.UTF8.GetBytes(bodyString);
            // Message rabbitmq'da memoryde durmasın fiziksel olarak keydedilsin
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            // Artık messageyi gönder
            channel.BasicPublish(exchange: RabbitMqClientService.ExchangeName, routingKey: RabbitMqClientService.RoutingWatermark, basicProperties: properties, body: bodyByte);
        }
    }
}
