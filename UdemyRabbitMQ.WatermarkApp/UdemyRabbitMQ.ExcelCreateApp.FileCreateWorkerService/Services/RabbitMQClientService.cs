using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdemyRabbitMQ.ExcelCreateApp.FileCreateWorkerService.Services
{
    // Burası Subscriber için olacak yani subscriber rabbitmq'ya bağlanırken bu classı kullanacak artık burada kuyruk, exchange declare edip bindlamaya gerek yok zaten publisher tarafında yaptık
    public class RabbitMQClientService : IDisposable
    {
        // Rabbitmqya bağlanmak için
        // readonly ctorda bir kere set edilsin bir daha set edilmesin
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        // Kanal için
        private IModel _channel;
        public static string QueueName = "queue-excel-file";
        // Loglama yapalım
        private readonly ILogger<RabbitMQClientService> _logger;

        public RabbitMQClientService(ConnectionFactory connectionFactory, ILogger<RabbitMQClientService> logger)
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
            if (_channel is { IsOpen: true })
            {
                // Kanal daha önce var
                return _channel;
            }
            // Channel daha önce yok, Bağlantı üzerinden channel oluştur
            _channel = _connection.CreateModel();
     
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
