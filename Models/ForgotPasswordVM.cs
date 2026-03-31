using System.ComponentModel.DataAnnotations;

namespace iameewh.Models
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Vui lòng nhập Email để tìm lại tài khoản.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;
    }
}