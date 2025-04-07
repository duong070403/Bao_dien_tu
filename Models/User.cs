using System;
using System.Collections.Generic;

namespace WebBaoDienTu.Models;

public partial class User
{
    public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<News> News { get; set; } = new List<News>();
        public virtual ICollection<NewsSharing> NewsSharings { get; set; } = new List<NewsSharing>();
}
