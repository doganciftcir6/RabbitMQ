using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UdemyRabbitMQ.ExcelCreateApp.Web.Models
{
    // Bu sefer contexte üyelik için Identity kullanacağız
    public class AppDbContext : IdentityDbContext
    {
        // OnConfiguring metodunda options'u doldurmak yerine startup tarafında
        // daha merkezi bir yerde bu options'u doldurmak istiyorum o yüzden ctor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<UserFile> UserFiles { get; set; }
    }
}
