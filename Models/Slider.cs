using System.ComponentModel.DataAnnotations;

namespace iameewh.Models
{
    public class Slider
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề banner")]
        public string Title { get; set; }

        public string? ImageUrl { get; set; }
    }
}