using System;
using System.Collections.Generic;

namespace WebBaoDienTu.Models;

public partial class News
{
      public int NewsId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public int AuthorId { get; set; }
        public int CategoryId { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual User Author { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<NewsSharing> NewsSharings { get; set; } = new List<NewsSharing>();
}
