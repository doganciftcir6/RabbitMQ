using System;
using System.Collections.Generic;
using System.Text;

namespace UdemyRabbitMQExcelCreateApp.Shared.Messages
{
    // RabbitMQ'ya gidecek olan messagenin içeriğinde ne olacak
    // Eğer tabloda çok fazla kayıt olacaksa message içinde tabloyu göndermek mantıklı değil WorkesService veri tabanına bağlanıp tabloyu kendisi çeksin ama küçük verili tabloları excele dönüştüreceksek direkt olarak messagenin içerisinde tabloyu Byte[] olarak gönderebilirdik ve burada onu List<Product> olarak belirtebilridik
    public class CreateExcelMessage
    {
        public int FileId { get; set; }
    }
}
