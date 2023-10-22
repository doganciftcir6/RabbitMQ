using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using UdemyRabbitMQ.ExcelCreateApp.Web.Models;
using UdemyRabbitMQ.ExcelCreateApp.Web.Services;

namespace UdemyRabbitMQ.ExcelCreateApp.Web.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        // RabbitMQ'ya message göndermek için
        private readonly RabbitMQPublisher _rabbitMQPublisher;
        public ProductsController(AppDbContext context, UserManager<IdentityUser> userManager, RabbitMQPublisher rabbitMQPublisher)
        {
            _context = context;
            _userManager = userManager;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public IActionResult Index()
        {
            return View();
        }
        // Zaten Product tablosunda Excel oluşturma işlemi gerçekleştirceğim için bir parametre almama gerek yok, istersek parametre alıp tabloyu değiştirebiliriz
        public async Task<IActionResult> CreateProductExcel()
        {
            // Önce current useri bul
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            // Dosya ismi oluştur
            var fileName = $"product-excel-{Guid.NewGuid().ToString().Substring(1, 10)}";
            // Nesnemizi dolduralım
            UserFile userFile = new UserFile()
            {
                UserId = user.Id,
                FileName = fileName,
                FileStatus = FileStatus.Creating
            };
            // Dolu nesneyi veri tabanında oluştur
            await _context.UserFiles.AddAsync(userFile);
            await _context.SaveChangesAsync();
            // RabbitMQ'ya mesaj gönderilecek burada
            _rabbitMQPublisher.Publish(new UdemyRabbitMQExcelCreateApp.Shared.Messages.CreateExcelMessage() { FileId = userFile.Id});
            // Files actionuna bilgi taşıyalım, TempData veriyi cookiede tutuyor
            TempData["StartCreatingExcel"] = true;
            return RedirectToAction(nameof(Files));
        }
        public async Task<IActionResult> Files()
        {
            // Current userı al önce
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            // Mevcut user'a ait dataları view'e taşı
            return View(await _context.UserFiles.Where(x => x.UserId == user.Id).OrderByDescending(x => x.Id).ToListAsync());
        }
    }
}
