using Microsoft.AspNetCore.Mvc;
using iameewh.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;

namespace iameewh.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var userList = _db.ApplicationUsers.ToList();
            return View(userList);
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng này!" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // Đang bị khóa -> Tiến hành mở khóa
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                // Đang hoạt động -> Tiến hành khóa tài khoản
                objFromDb.LockoutEnd = DateTime.Now.AddYears(100);
            }

            _db.SaveChanges();
            return Json(new { success = true, message = "Thao tác thành công!" });
        }
    }
}