using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iameewh.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nhập tên danh mục!")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<Product>? Products { get; set; }
    }
}