namespace UdemyRabbitMQ.WatermarkApp.Web.Services
{
    public class ProductImageCreatedEvent
    {
        // Publisherın Rabbitmq'ya göndereceği eventte ne olacak
        public string ImageName { get; set; }
    }
}
