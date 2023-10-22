using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UdemyRabbitMQ.WatermarkApp.Web.Models;
using UdemyRabbitMQ.WatermarkApp.Web.Services;

namespace UdemyRabbitMQWeb.Watermark.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        // Publisher olarak RabbitMQ'ya event gönderebilmek için
        private readonly RabbitMQPublisher _rabbitMQPublisher;
        public ProductsController(AppDbContext context, RabbitMQPublisher rabbitMQPublisher)
        {
            _context = context;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Stock,ImageName")] Product product, IFormFile ImageFile)
        {
            // Bind attribute'u şu işe yarıyor cshtml'den isterse Product'ın tüm propertyleri gönderilsin ama ben burada Product nesnesinin sadece şu propertyleri almak istiyorum diyorum
            // cshtml'de name olarak gönderdiğim ImageFile'ı burada aynı isimle parametrede yakalıyorum
            if (!ModelState.IsValid) return View(product);

            // is keywordu ile ImageFile'ın propertylerine direkt olarak kontrol edebiliyorum
            if (ImageFile is { Length: > 0 })
            {
                var randomImageName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName).ToString();
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images",randomImageName);
                await using FileStream stream = new FileStream(path, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                // Image benzersiz ismi ile beraber wwwroot'a kaydedildi
                // Kaydedilen imagenin ismini Publisher olarak RabbitMQ'ya gönder
                _rabbitMQPublisher.Publish(new ProductImageCreatedEvent() { ImageName = randomImageName });
                // Oluşan dosya ismini entity'e atalım
                product.ImageName = randomImageName;
            }
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
    }

    // GET: Products/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    // POST: Products/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Stock,PictureUrl")] Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // GET: Products/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // POST: Products/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}
}