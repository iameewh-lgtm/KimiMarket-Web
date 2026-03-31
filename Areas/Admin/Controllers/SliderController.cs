using iameewh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iameewh.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SliderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SliderController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Slider> objSliderList = _db.Sliders.ToList();
            return View(objSliderList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Slider obj, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string sliderPath = Path.Combine(wwwRootPath, @"images\sliders");

                    if (!Directory.Exists(sliderPath)) Directory.CreateDirectory(sliderPath);

                    using (var fileStream = new FileStream(Path.Combine(sliderPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.ImageUrl = @"\images\sliders\" + fileName;
                }

                _db.Sliders.Add(obj);
                _db.SaveChanges();
                TempData["success"] = "Thêm Banner thành công";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            var obj = _db.Sliders.Find(id);
            if (obj == null) return NotFound();

            if (!string.IsNullOrEmpty(obj.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
            }

            _db.Sliders.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Xóa Banner thành công";
            return RedirectToAction("Index");
        }
    }
}