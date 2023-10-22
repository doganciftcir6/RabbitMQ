using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using UdemyRabbitMQ.ExcelCreateApp.Web.Hubs;
using UdemyRabbitMQ.ExcelCreateApp.Web.Models;

namespace UdemyRabbitMQ.ExcelCreateApp.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly IHubContext<MyHub> _hubContext;
        public FilesController(AppDbContext appDbContext, IHubContext<MyHub> hubContext)
        {
            _appDbContext = appDbContext;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int fileId)
        {
            // is not ile file içindeki propertyler yazdığımız kontroldeki gibi değilse diyoruz
            if (file is not { Length: > 0 }) return BadRequest();
            // İlgili UserFile'a erişelim
            var userFile = await _appDbContext.UserFiles.FirstAsync(x => x.Id == fileId);
            var filePath = userFile.FileName + Path.GetExtension(file.FileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files", filePath);
            using FileStream stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            // Dosya kaydoldu
            userFile.CreatedDate = DateTime.Now;
            userFile.FilePath = filePath;
            userFile.FileStatus = FileStatus.Completed;
            await _appDbContext.SaveChangesAsync();
            // İleride SignalR ile bildirim oluşturacağız burada
            // Upload işlemi bittiğinde Hangi kullanıcı oluşturmuşsa o Kullanıcıya realtime olarak bilgi gönderecek
            // Dinleme işlemi Layoutta olacak çünkü bildirimin tüm sayfalarda gözüksün istiyoruz sadece Productta değil
            await _hubContext.Clients.User(userFile.UserId).SendAsync("ComplatedFile");
            return Ok();
        }
    }
}
