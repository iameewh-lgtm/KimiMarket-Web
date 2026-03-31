using iameewh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iameewh.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded) return RedirectToAction("Index", "Home");

                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.RoleList = GetRoleSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Name = model.Name };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Chặn việc chọn quyền Admin trực tiếp từ form đăng ký
                    string selectedRole = model.Role;
                    if (selectedRole == "Admin")
                    {
                        selectedRole = "Buyer"; // Chuyển về quyền Buyer mặc định
                    }

                    if (await _roleManager.RoleExistsAsync(selectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, selectedRole);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Buyer");
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            }
            ViewBag.RoleList = GetRoleSelectList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserList()
        {
            var users = await _db.ApplicationUsers.ToListAsync();
            var userVMList = new List<UserVM>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userVMList.Add(new UserVM
                {
                    Id = user.Id,
                    Name = user.Name ?? "Chưa cập nhật tên",
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "Buyer"
                });
            }
            return View(userVMList);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(string userId)
        {
            var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            ViewBag.UserName = user.Name;
            ViewBag.UserId = userId;
            ViewBag.CurrentRole = userRoles.FirstOrDefault();

            return View(roles);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, roleName);

            TempData["success"] = "Đã cập nhật quyền thành công.";
            return RedirectToAction(nameof(UserList));
        }

        private List<SelectListItem> GetRoleSelectList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "Người mua hàng (Buyer)", Value = "Buyer" },
                new SelectListItem { Text = "Người bán hàng (Seller)", Value = "Seller" }
            };
        }

        public IActionResult AccessDenied() => View();
    }
}