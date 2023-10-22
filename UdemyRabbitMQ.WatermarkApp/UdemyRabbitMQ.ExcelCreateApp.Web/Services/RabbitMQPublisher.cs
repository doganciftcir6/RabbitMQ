using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using UdemyRabbitMQExcelCreateApp.Shared.Messages;

namespace UdemyRabbitMQ.ExcelCreateApp.Web.Services
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
        public void Publish(CreateExcelMessage createExcelMessage)
        {
            // RabbitMQ'ya bağlan ve kanalı al
            var channel = _rabbitMqClientService.Connect();
            // RabbitMQ'ya göderilecek olan messageyi stringe çevir
            var bodyString = JsonSerializer.Serialize(createExcelMessage);
            // Stringi Byte[] çevir
            var bodyByte = Encoding.UTF8.GetBytes(bodyString);
            // Message rabbitmq'da memoryde durmasın fiziksel olarak keydedilsin
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            // Artık messageyi gönder
            channel.BasicPublish(exchange: RabbitMqClientService.ExchangeName, routingKey: RabbitMqClientService.RoutingExcel, basicProperties: properties, body: bodyByte);
        }
    }
}

