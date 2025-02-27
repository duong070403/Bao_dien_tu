using System;
using System.Collections.Generic;

namespace BTL_Xay_dung_bao_dien_tu.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Role { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<News> News { get; set; } = new List<News>();
}
