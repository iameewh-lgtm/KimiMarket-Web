using Microsoft.AspNetCore.Mvc;
using iameewh.Models;
using iameewh.Utility; // Khai báo thư viện VnPay của bạn ở đây
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;

namespace iameewh.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public OrderController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        // 1. Lấy lịch sử đơn hàng của người dùng
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var orders = _db.OrderHeaders.Where(u => u.ApplicationUserId == userId).ToList();
            return View(orders);
        }

        // 2. Hiển thị trang Thanh toán (Checkout)
        public IActionResult Checkout()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var cartList = _db.ShoppingCarts
                .Include(u => u.Product)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            if (!cartList.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId);

            OrderHeader orderHeader = new OrderHeader
            {
                ApplicationUserId = userId,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber
            };

            foreach (var cart in cartList)
            {
                orderHeader.OrderTotal += (cart.Product.Price * cart.Count);
            }

            ViewBag.CartList = cartList;
            return View(orderHeader);
        }

        // 3. Xử lý lưu đơn hàng và CHUYỂN HƯỚNG VNPAY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(OrderHeader orderHeader, string Address, string PaymentMethod)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var cartList = _db.ShoppingCarts
                .Include(u => u.Product)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            orderHeader.ApplicationUserId = userId;
            orderHeader.OrderDate = DateTime.Now;

            if (PaymentMethod == "VNPay")
            {
                orderHeader.OrderStatus = "Chờ thanh toán VNPay";
            }
            else
            {
                orderHeader.OrderStatus = "Đang xử lý"; // COD
            }

            foreach (var cart in cartList)
            {
                orderHeader.OrderTotal += (cart.Product.Price * cart.Count);
            }

            // Lưu Header
            _db.OrderHeaders.Add(orderHeader);
            _db.SaveChanges();

            // Lưu Chi tiết đơn hàng
            foreach (var cart in cartList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = orderHeader.Id,
                    Price = cart.Product.Price,
                    Count = cart.Count
                };
                _db.OrderDetails.Add(orderDetail);
            }

            // Xóa giỏ hàng sau khi tạo đơn
            _db.ShoppingCarts.RemoveRange(cartList);
            _db.SaveChanges();

            // TẠO URL THANH TOÁN VNPAY
            if (PaymentMethod == "VNPay")
            {
                string vnp_Returnurl = _configuration["VnPay:ReturnUrl"];
                string vnp_Url = _configuration["VnPay:BaseUrl"];
                string vnp_TmnCode = _configuration["VnPay:TmnCode"];
                string vnp_HashSecret = _configuration["VnPay:HashSecret"];

                VnPayLibrary vnpay = new VnPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", (orderHeader.OrderTotal * 100).ToString("0"));
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "ThanhToanDonHang_" + orderHeader.Id);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", orderHeader.Id.ToString());

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
                return Redirect(paymentUrl);
            }

            TempData["success"] = "Đặt hàng thành công!";
            return RedirectToAction("OrderConfirmation", new { id = orderHeader.Id });
        }

        // 4. Hiển thị trang Xác nhận thành công
        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        // 5. Hàm nhận kết quả và KIỂM TRA CHỮ KÝ từ VNPay
        public IActionResult VnPayReturn()
        {
            if (Request.Query.Count > 0)
            {
                string vnp_HashSecret = _configuration["VnPay:HashSecret"];
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                // Đưa toàn bộ dữ liệu trả về vào thư viện để xác thực
                foreach (var s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s.Key, s.Value);
                    }
                }

                string vnp_TxnRef = Request.Query["vnp_TxnRef"];
                string vnp_SecureHash = Request.Query["vnp_SecureHash"];
                string vnp_ResponseCode = Request.Query["vnp_ResponseCode"];

                // Kiểm tra chữ ký bảo mật
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        var order = _db.OrderHeaders.FirstOrDefault(u => u.Id.ToString() == vnp_TxnRef);
                        if (order != null)
                        {
                            order.OrderStatus = "Đã thanh toán";
                            _db.SaveChanges();
                            TempData["success"] = "Thanh toán VNPay thành công!";
                            return RedirectToAction("OrderConfirmation", new { id = order.Id });
                        }
                    }
                    else
                    {
                        TempData["error"] = "Thanh toán VNPay thất bại hoặc đã bị hủy.";
                        return RedirectToAction("Index", "Cart");
                    }
                }
                else
                {
                    TempData["error"] = "Lỗi xác thực chữ ký VNPay (Gian lận hoặc lỗi kết nối).";
                    return RedirectToAction("Index", "Cart");
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}