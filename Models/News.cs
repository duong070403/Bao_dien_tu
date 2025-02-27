using System;
using System.Collections.Generic;

namespace BTL_Xay_dung_bao_dien_tu.Models;

public partial class News
{
    public int NewsId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int AuthorId { get; set; }

    public int CategoryId { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}
