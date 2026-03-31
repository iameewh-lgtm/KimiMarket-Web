using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using iameewh.Models;

namespace iameewh.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            var productList = _db.Products.Include(u => u.Category).ToList();
            return View(productList);
        }

        [Authorize(Roles = "Admin,Seller")]
        public IActionResult Upsert(int? id)
        {
            ViewBag.CategoryList = new SelectList(_db.Categories, "Id", "Name");

            if (id == null || id == 0)
            {
                return View(new Product());
            }
            else
            {
                var product = _db.Products.Find(id);
                if (product == null) return NotFound();
                return View(product);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Product obj, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    if (!string.IsNullOrEmpty(obj.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.ImageUrl = @"\images\products\" + fileName;
                }

                if (obj.Id == 0)
                {
                    _db.Products.Add(obj);
                    TempData["success"] = "Thêm sản phẩm thành công!";
                }
                else
                {
                    _db.Products.Update(obj);
                    TempData["success"] = "Cập nhật sản phẩm thành công!";
                }

                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryList = new SelectList(_db.Categories, "Id", "Name");
            return View(obj);
        }

        // ĐÃ SỬA TẠI ĐÂY: Thêm quyền Seller được phép xóa
        [Authorize(Roles = "Admin,Seller")]
        public IActionResult Delete(int? id)
        {
            var obj = _db.Products.Find(id);
            if (obj == null) return NotFound();

            if (!string.IsNullOrEmpty(obj.ImageUrl))
            {
                var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _db.Products.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }
    }
}