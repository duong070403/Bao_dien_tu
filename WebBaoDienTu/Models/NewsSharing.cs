using System;
using System.Collections.Generic;

namespace WebBaoDienTu.Models;

public partial class NewsSharing
{
 public int ShareId { get; set; }
        public int NewsId { get; set; }
        public int SenderId { get; set; }
        public string ReceiverEmail { get; set; } = null!;
        public DateTime SharedAt { get; set; } = DateTime.Now;

        public virtual News News { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
}
