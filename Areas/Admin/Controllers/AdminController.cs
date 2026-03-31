using Microsoft.AspNetCore.Mvc;
using iameewh.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace iameewh.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Dashboard()
        {
            // 1. Tổng doanh thu: Sử dụng AsEnumerable để tránh lỗi tính tổng (Sum) đối với kiểu decimal trên SQLite
            ViewBag.TotalRevenue = _db.OrderHeaders.AsEnumerable().Sum(u => u.OrderTotal);

            // 2. Tổng số lượng đơn hàng
            ViewBag.TotalOrders = _db.OrderHeaders.Count();

            // 3. Tổng số lượng khách hàng
            ViewBag.TotalUsers = _db.ApplicationUsers.Count();

            // 4. Danh sách 5 sản phẩm bán chạy nhất
            var topProducts = _db.OrderDetails
                .Include(u => u.Product)
                .GroupBy(u => u.ProductId)
                .Select(g => new {
                    Product = g.First().Product,
                    TotalQty = g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToList();

            return View(topProducts);
        }
    }
}