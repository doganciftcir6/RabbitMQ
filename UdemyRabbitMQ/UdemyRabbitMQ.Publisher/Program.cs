using RabbitMQ.Client;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace UdemyRabbitMQ.Publisher
{
    // DİRECT / TOPİC EXCHANGE
    public enum LogNames
    {
        Critical = 1,
        Error = 2,
        Warning = 3,
        Info = 4
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // RabbitMQ'ya bağlanmak için
            var factory = new ConnectionFactory();
            // Gerçek senaryoda Uri bilgisi Appsettings.json içerisinden alınmalı
            factory.Uri = new Uri("amqps://kasgqbbi:Wp526uB4_j4R---ulZ1bRvLoj4AqtU_E@toad.rmq.cloudamqp.com/kasgqbbi");
            // Bağlantı aç, Using() içerisinde oluşturacağımız connection süslü parantezler içindeki işlemler bittiği zaman Memory'den otomatik olarak düşecektir
            using (var connection = factory.CreateConnection())
            {
                // Bir bağlantı var bu bağlantı üzerinden RabbitMQ'ya bir kanal üzerinden bağlanacağız o yüzden kanal oluştur
                var channel = connection.CreateModel();
                // Bu kanal üzerinden artık RabbitMQ ile haberleşebilirim, İlk olarak ben RabbitMQ'ya bir message gönderdiğimde bir kuyruğun olması lazım yoksa messagelar boşa gider, Kuyruk oluştur
                // Önce kuyruğun ismini ver, İkincisi durable propu eğer ben bunu false yaparsam rabbitmqda oluşan kuyruklar memoryde tutulur rabbitmq restart yerse memory gideceğinden dolayı tüm kuyruk gider true yaparsak kuyruklar fiziksel olarak kaydedilir rabbitmq restart yese bile kuyruklar gitmez gerçek dünyada true olması uygun. Üçüncü olarak Exclusive isminde prop var eğer bu true olursa buradaki kuyruğa sadece burada oluşturmuş olduğum kanal üzerinden bağlanabilirim ama ben istiyorum ki buradaki kuyruğa Subscriber tarafında yani farklı bir processte farklı bir kanal oluşturmam lazım rabbitmqya erişip kuyruğu dinleyebilmem için işte bu farklı kanal üzerinden bağlanıcam o yüzden false yapmalıyız zaten gerçek hayatta çoğunlukla false yapılır. Dördüncü ise AutoDelete ismindeki property bu ise eğer bu kuyruğa bağlı olan son Subscriberda bağlantısını kopartırsa kuyruğu otomatik bir şekilde siler bu istediğim bir durum değil subsriber'ım yanlışlıkla down olursa kuyruk silinir ama kuyruk her zaman ayakta durmalı
                // Kuyruk yoksa bu kod kuyruğu sıfırdan oluşturur kuyruk daha önce varsa herhangi bir işlem yapmaz
                //channel.QueueDeclare("hello-queue", true, false, false);

                // FANOUT / DİRECT / TOPİC /Header EXCHANGE
                // Artık bir Exchange Declare edeceğiz, önce isim ver, ikinci durable ver true dersek fiziksel olarak keydedilsin uygulama restart yediğinde bu exchange kaybolmasın, üçüncü olarak type belirtiyoruz, 
                // channel.ExchangeDeclare("logs-fanout", durable: true, type: ExchangeType.Fanout);
                // channel.ExchangeDeclare("logs-direct", durable: true, type: ExchangeType.Direct);
                // channel.ExchangeDeclare("logs-topic", durable: true, type: ExchangeType.Topic);
                channel.ExchangeDeclare("header-exchange", durable: true, type: ExchangeType.Headers);

                //DİRECT EXCHANGE 
                // Şimdi en her bir log için kuyruk oluşturup Exchange'e bind edeceğiz
                //Enum.GetNames(typeof(LogNames)).ToList().ForEach(x =>
                //{
                //    Enuma göre kuyruk isimlerini vereceğiz 4 kuyruk oluşacak
                //     Enuma göre oluşan 4 kuyruğa route tanımlayacağız
                //    var routeKey = $"route-{x}";
                //    var queueName = $"direct-queue = {x}";
                //    channel.QueueDeclare(queueName, true, false, false);
                //    channel.QueueBind(queueName, "logs-direct", routeKey, null);
                //});

                // TOPİC EXCHANGE
                // 3 tane random log ismi al
                //Random rnd = new Random();

                //HEADER EXCHANGE
                Dictionary<string, object> headers = new Dictionary<string, object>();
                headers.Add("format", "pdf");
                headers.Add("shape", "a4");
                var properties = channel.CreateBasicProperties();
                // Messageleri kalıcı hale getirmek, true dersek kalıcı olur fiziksel bir yerde saklanır ramde değil, bu diğer tüm echangeler içinde geçerli bir tane propertioes oluşturup Persistancesini true yaparız
                properties.Persistent = true;
                properties.Headers = headers;
                // Complex typeı message olarak göndermek
                var product = new Product() { Id = 1, Name = "Test", Price = 100, Stock = 10  };
                var productJsonString = JsonSerializer.Serialize(product);
                channel.BasicPublish("header-exchange", string.Empty, properties, Encoding.UTF8.GetBytes(productJsonString));

                // Artık tek seferde kuyruğa 50 message gitsin istiyorum
                //Enumerable.Range(1, 50).ToList().ForEach(x =>
                //{
                //    //DİRECT EXCHANGE
                //    //Enuma göre rastgele isimler vereceğiz messagelere
                //    // LogNames log = (LogNames)new Random().Next(1, 5);
                //    // Kuyruk oluştu messageyi oluşturalım
                //    // string message = $"Log-type: {log}";

                //    // TOPİC EXCHANGE
                //    // 3 tane random log ismi al
                //    LogNames log1 = (LogNames)rnd.Next(1, 5);
                //    LogNames log2 = (LogNames)rnd.Next(1, 5);
                //    LogNames log3 = (LogNames)rnd.Next(1, 5);
                //    var routeKey = $"{log1}.{log2}.{log3}";
                //    string message = $"Log-type: {log1}-{log2}-{log3}";

                //    // RabbitMQ'ya messagelerimizi Byte[] dizisi olarak göndeririz. Bu sayede avantaj olarak istediğimiz her şeyi gönderebiliyoruz. PDF, İmage, Büyük bir dosya yani sonuçta Byte dizisine çevir gönder
                //    // Messageyi Byte[]'a çevir
                //    var messageBody = Encoding.UTF8.GetBytes(message);

                //    // Artık messageyi kuyruğa gönder
                //    // Eğer Exchange kullanmıyorsak string.Empty diyoruz ve bu Default Exchange ismini alıyor eğer Default Exchange kullanıyorsak routingKey parametresine mutlaka kuyruğun ismini vermemiz gerekiyor ki route map'E göre gelen messageyi şu kuyruğa gönderebilsin

                //    // FANOUT / DİRECT EXCHANGE
                //    // Artık Exchange kullandığımız için ilk parametrede Exchange'in adını veriyoruz, Artık kuyruk ismini vermiyoruz

                //    //DİRECT EXCHANGE
                //    // Artık messagelerin routelarını belirlemem lazım
                //    // var routeKey = $"route-{log}";
                //    //channel.BasicPublish("logs-direct", routeKey, null, messageBody);

                //    //TOPİC EXCHANGE
                //    // channel.BasicPublish("logs-direct", routeKey, null, messageBody);
                //    channel.BasicPublish("logs-topic", routeKey, null, messageBody);
                //    Console.WriteLine($"Log gönderilmiştir : {message}");
                //});
                Console.WriteLine("Message gönderilmiştir");
                Console.ReadLine();
            }
        }
    }
}
