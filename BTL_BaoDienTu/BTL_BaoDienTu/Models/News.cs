using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTL_BaoDienTu.Models;

public partial class News
{
    public int NewsId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public int AuthorId { get; set; }

    public int CategoryId { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<NewsSharing> NewsSharings { get; set; } = new List<NewsSharing>();
}
