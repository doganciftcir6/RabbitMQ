using Microsoft.EntityFrameworkCore;

namespace UdemyRabbitMQ.WatermarkApp.Web.Models
{
    public class AppDbContext : DbContext
    {
        // OnConfiguring metodunda options'u doldurmak yerine startup tarafında
        // daha merkezi bir yerde bu options'u doldurmak istiyorum o yüzden ctor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }
        public DbSet<Product> Products { get; set; }
    }
}
