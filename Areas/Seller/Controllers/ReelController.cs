using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using iameewh.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Linq;

namespace iameewh.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Admin,Seller")]
    public class ReelController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ReelController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            var objReelList = _db.Reels.Include(r => r.Product).ToList();
            return View(objReelList);
        }

        public IActionResult Upsert(int? id)
        {
            ViewBag.ProductList = new SelectList(_db.Products, "Id", "Name");

            if (id == null || id == 0)
            {
                return View(new Reel());
            }
            else
            {
                var reelFromDb = _db.Reels.Find(id);
                if (reelFromDb == null) return NotFound();
                return View(reelFromDb);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Reel obj, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string reelPath = Path.Combine(wwwRootPath, @"videos\reels");

                    if (!Directory.Exists(reelPath))
                    {
                        Directory.CreateDirectory(reelPath);
                    }

                    if (!string.IsNullOrEmpty(obj.VideoUrl))
                    {
                        var oldVideoPath = Path.Combine(wwwRootPath, obj.VideoUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldVideoPath))
                        {
                            System.IO.File.Delete(oldVideoPath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(reelPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.VideoUrl = @"\videos\reels\" + fileName;
                }

                if (obj.Id == 0)
                {
                    if (string.IsNullOrEmpty(obj.VideoUrl))
                    {
                        TempData["error"] = "Nẫu phải chọn file video chớ!";
                        ViewBag.ProductList = new SelectList(_db.Products, "Id", "Name");
                        return View(obj);
                    }
                    _db.Reels.Add(obj);
                    TempData["success"] = "Đăng video thành công rồi nhen!";
                }
                else
                {
                    _db.Reels.Update(obj);
                    TempData["success"] = "Cập nhật video êm ru!";
                }

                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ProductList = new SelectList(_db.Products, "Id", "Name");
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            var obj = _db.Reels.Find(id);
            if (obj == null) return NotFound();

            if (!string.IsNullOrEmpty(obj.VideoUrl))
            {
                var oldVideoPath = Path.Combine(_hostEnvironment.WebRootPath, obj.VideoUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldVideoPath))
                {
                    System.IO.File.Delete(oldVideoPath);
                }
            }

            _db.Reels.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Xóa clip thành công!";
            return RedirectToAction("Index");
        }
    }
}