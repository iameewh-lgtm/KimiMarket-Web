using Microsoft.AspNetCore.Mvc;
using iameewh.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace iameewh.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var shoppingCarts = _db.ShoppingCarts
                .Include(u => u.Product)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            return View(shoppingCarts);
        }

        public IActionResult Plus(int cartId)
        {
            var cart = _db.ShoppingCarts.FirstOrDefault(c => c.Id == cartId);
            if (cart != null)
            {
                cart.Count += 1;
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cart = _db.ShoppingCarts.FirstOrDefault(c => c.Id == cartId);
            if (cart != null)
            {
                if (cart.Count <= 1)
                {
                    _db.ShoppingCarts.Remove(cart);
                }
                else
                {
                    cart.Count -= 1;
                }
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cart = _db.ShoppingCarts.FirstOrDefault(c => c.Id == cartId);
            if (cart != null)
            {
                _db.ShoppingCarts.Remove(cart);
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}