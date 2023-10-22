using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;

namespace UdemyRabbitMQ.WatermarkApp.Web.Services
{
    public class RabbitMqClientService : IDisposable
    {
        // Rabbitmqya bağlanmak için
        // readonly ctorda bir kere set edilsin bir daha set edilmesin
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        // Kanal için
        private IModel _channel;
        // DirectExchange kullancağız
        public static string ExchangeName = "ImageDirectExchange";
        public static string RoutingWatermark = "watermark-route-image";
        public static string QueueName = "queue-watermark-image";
        // Loglama yapalım
        private readonly ILogger<RabbitMqClientService> _logger;

        public RabbitMqClientService(ConnectionFactory connectionFactory, ILogger<RabbitMqClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }
        // Bağlantı kurma işlemini yapan metot
        public IModel Connect()
        {
            // Alttaki işlemler Kuyruk,Exchange Declare etmek vs Publisher tarafından yapılıyor bu senaryoda
            // Bir bağlantı aç
            _connection = _connectionFactory.CreateConnection();
            // Kanal daha önce var mı yok mu, is keywordu ile _channel'ın propertylerine girip bir if sorgusu yazabiliyoruz
            // Alttaki örneğe göre çok daha efektif kullanması
            // if(_channel.IsOpen == true)
            if(_channel is { IsOpen: true })
            {
                // Kanal daha önce var
                return _channel;
            }
            // Channel daha önce yok, Bağlantı üzerinden channel oluştur
            _channel = _connection.CreateModel();
            // Channel üzerinden Exchange oluştur
            _channel.ExchangeDeclare(ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
            // Kuyruk oluştur
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, null);
            // Exchange hazır kuyruk hazır şimdi bu kuyrduğu Exchange'e Bind etmeliyiz
            _channel.QueueBind(exchange: ExchangeName, queue: QueueName, routingKey: RoutingWatermark);
            // Log atalım
            _logger.LogInformation("RabbiMQ ile bağlantı kuruldu...");
            // Geriye kanalı dön ben bu kanal üzerinden rabbitmq'ya messageleri göndereceğim
            return _channel;
        }

        // Dispose olduğunda RabbitMq ile ilgili bağlantıları kapatalım
        public void Dispose()
        {
            // Channel var ise null değil ise kaptalım
            _channel?.Close();
            // Channel var ise null değil ise Dispose edelim
            _channel?.Dispose();
            // Channeli null'a set et
            // default keywordu sol taraftaki değişkenin default değerini verir yani bu değiken bool olsaydı o değişkene false atayacaktı
            // _channel = default;
            // Connection varsa close et
            _connection?.Close();
            // Connection varsa Dispose et
            _connection?.Dispose();
            // Log yazdır
            _logger.LogInformation("RabbitMQ ile bağlantı koptu...");
        }
    }
}
