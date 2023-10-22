using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace UdemyRabbitMQ.Subscriber
{
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

                //TOPİC EXCHANGE

                // Bu kanal üzerinden artık RabbitMQ ile haberleşebilirim, İlk olarak ben RabbitMQ'ya bir message gönderdiğimde bir kuyruğun olması lazım yoksa messagelar boşa gider, Kuyruk oluştur
                // Artık buradaki kodu ya tutabilir ya da silebiliriz eğer silersek Subscriber'ı ayağa kaldırdığımızda böyle bir kuyruk yoksa hata alırız eğer silmezsek Publisher bu kuyruğu oluşturmazsa Subsriber bu kuyruğu oluşturur ve uygulamada hata almayız. Eğer Publisherın gerçekten bu kuyruğu oluşturduğundan eminsek bu kodu silebiliriz. Eğer kuyruk daha önceden oluşmuşsa rabbitmq bunu anlar hehangi bir işlem yapmaz eğer kuyruk yoksa burada bu kuyruğu oluşturur. Ancak hem Publisher hem Subscriber tarafında bu kodu kullanıp kuyruğu oluşturuyorsak parametrelerinin aynı olmasına dikkat etmeliyiz. Yani kuyruğu oluşturma işlemini hem Publisher hem Subscriber tarafında veya ikisinde de yapabiliyoruz
                // channel.QueueDeclare("hello-queue", true, false, false);

                // Fanout EXCHANGE
                // İstersek kuyruk Declare etmek gibi aynı Exchange'i burada da Declare edebiliriz parametreler aynı olmalı yine, Ama ben biliyorum ki Publisher bu exchange'i oluşturdu o yüzden bir daha gerek yok tanımlamaya
                // channel.ExchangeDeclare("logs-fanout", durable: true, type: ExchangeType.Fanout);
                // Şimdi kuyruk oluşturacağız ama kuyruk isimlerini random yapacağız ki her Consumer instancesi kendi ayrı kuyruğuna bağlansınlar, QueueName propu random kuyruk ismi veriyor
                // var randomQueueName = channel.QueueDeclare().QueueName;
                // Bu kuyruğu Fanout Exchangeine bind etmek gerekiyor, Eğer QueueDeclare() kullanırsak Consumerin ilgili instancesi down olsa dahi kuyruk durur silinmez biz silinmesini istiyoruz o yüzden QueueBind kullancağız
                // channel.QueueBind(randomQueueName, "logs-fanout", "", null);

                // Birde Rabbitmqdan messageleri kaçarlı kaçarlı alacağımı belirtebilirim, ilk parametreye 0 dersek bana herhangi bir boyuttaki messageyi gönderebilirsin deriz, ikinci parametre kaç kaç messageler gelsin 1 dersem her bir subscriber'a 1er 1er message gelsin, üçüncü parametre ise bu değer global olsun mu yani true dersek diyelim 6 message alacağız 2 instance var tek seferde 3 message birinci instanceye 3 message ikinci instanceye messageları gönderir yani messageleri instanceler arasında bölüştürür ve toplamda tüm instanceler kullanılarak 6 message gitti anlamına gelir eğer false dersek her bir subscriber için kaçar tane gönderilecek onu belirtir yani bir subscriber'a 6 message gelir, 1 message değeri için bu işlemler geçerli değil algılayamaz true false vermek önemli olmaz 2 den itibaren bu olay başlıyor
                channel.BasicQos(0, 1, false);
                // Artık subscriber'ı oluşturacağız
                var consumer = new EventingBasicConsumer(channel);
                // Bu consumer hangi kuyruğu dinleyecek onu belirt, Önce kuyruk ismi sonra AutoAck isminde bir parametre var eğer ben bunu true verirsem rabbitmq subscriber'a bir message gönderdiğinde bu message doğruda işlense yanlışta işlense kuyruktan siler. False yaparsak rabbitmqya sen bunu kuyruktan silme ben gelen messageyi doğru bir şekilde işlersem o zaman ben sana haberdar edecem kuyruktan silmen için, true diyelim şimdilik yani message Subscriber'a geldikten sonra message ilgili kuyruktan direkt olarak silinsin. Gerçek dünyada false yapılır gelen mesajı bazen doğru işlemeyebiliriz message silinmesin ben ne zaman messageyi doğru işlersem o zaman rabbitmqya haber vereyim o zaman silsin
                // DİRECT EXCHANGE
                //Bu subsrciber Critcal messageler ile ilgilensin
                // var queueName = "direct-queue = Critical";
                // TOPİC EXCHANGE
                var queueName = channel.QueueDeclare().QueueName;
                // var routeKey = "*.Error.*";
                // channel.QueueBind(queueName, "logs-topic", routeKey);
                // HEADER EXCHANGE
                Dictionary<string, object> headers = new Dictionary<string, object>();
                headers.Add("format", "pdf");
                headers.Add("shape", "a4");
                headers.Add("x-match", "any");
                channel.QueueBind(queueName, exchange: "header-exchange", string.Empty, headers);
                // FANOUT Exchange
                // channel.BasicConsume(randomQueueName, false, consumer);
                // Direct EXchange
                channel.BasicConsume(queueName, false, consumer);
                Console.WriteLine("Logları dinleniyor...");
                // Artık event üzerinden dinleyebilirim, bu event rabbitmq buradaki subscriber'a bir message gönderdiğinde buradaki event fırlayacak
                consumer.Received += (object sender, BasicDeliverEventArgs e) =>
                {
                    // Messageyi al, Message Byte[] dizisi olarak gelecek onu stringe çevirmem lazım
                    var message = Encoding.UTF8.GetString(e.Body.ToArray());
                    // Complex type messageleri kuyruktan okumak
                    Product product = JsonSerializer.Deserialize<Product>(message);
                    // Mesaggeleri çok hızlı konsola bastırmasın Threadi 1,5 saniye uyutalım
                    Thread.Sleep(1500);
                    Console.WriteLine($"Gelen Mesaj: {product.Id} - {product.Name} - {product.Price} - {product.Stock}");
                    // Gelen Messageyi txt dosyasına yazdıralım
                    // File.AppendAllText("log-critical.txt", message + "\n");
                    // Messageyi kuyruktan silmesi için rabbitmqya haber verelim, Bana ulaştırılan tagı rabbitmqya gönderiyorum rabbitmq hangi tagla bu mesajı ulaştırmışsa ilgili messageyi bulup kuyruktan siliyor sildekten sonra multiple parametresine true dersek o an memoryde işlenmiş ama rabbitmqya gitmemi şaşka messagelerde varsa onun bilgilerini de rabbitmqya haberdar eder 
                    channel.BasicAck(e.DeliveryTag, false);
                };
                Console.ReadLine();
            }
        }
    }
}
