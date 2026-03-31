using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iameewh.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace iameewh.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly int _pageSize = 8;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Trang chủ: Danh sách sản phẩm, Tìm kiếm, Phân trang
        public IActionResult Index(string searchString, int? categoryId, int productPage = 1)
        {
            ViewBag.CategoryList = _db.Categories.ToList();
            ViewBag.SliderList = _db.Sliders.ToList();

            var products = _db.Products.Include(u => u.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            if (categoryId != null && categoryId > 0)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            int totalItems = products.Count();

            ProductListVM productListVM = new()
            {
                Products = products.Skip((productPage - 1) * _pageSize).Take(_pageSize).ToList(),
                CurrentPage = productPage,
                TotalPages = (int)Math.Ceiling((decimal)totalItems / _pageSize),
                SearchString = searchString,
                CategoryId = categoryId
            };

            return View(productListVM);
        }

        // Chi tiết sản phẩm: Tham số productId phải khớp với View
        public IActionResult Details(int productId)
        {
            var product = _db.Products
                .Include(u => u.Category)
                .FirstOrDefault(u => u.Id == productId);

            if (product == null) return NotFound();

            ShoppingCart cartObj = new()
            {
                Count = 1,
                ProductId = productId,
                Product = product
            };

            var reviews = _db.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.ApplicationUser)
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            ViewBag.ReviewList = reviews;

            return View(cartObj);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;

            ShoppingCart cartFromDb = _db.ShoppingCarts.FirstOrDefault(u =>
                u.ApplicationUserId == userId && u.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                cartFromDb.Count += shoppingCart.Count;
                _db.ShoppingCarts.Update(cartFromDb);
            }
            else
            {
                _db.ShoppingCarts.Add(shoppingCart);
            }

            _db.SaveChanges();
            TempData["success"] = "Đã thêm vào giỏ hàng thành công";
            return RedirectToAction(nameof(Index));
        }

        // Video Reels
        public IActionResult Reels()
        {
            var reelsList = _db.Reels
                .Include(r => r.Product)
                .OrderByDescending(r => r.Id)
                .ToList();

            return View(reelsList);
        }
    }
}