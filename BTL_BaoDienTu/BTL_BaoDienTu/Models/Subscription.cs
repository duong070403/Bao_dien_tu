using System;
using System.Collections.Generic;

namespace BTL_BaoDienTu.Models;

public partial class Subscription
{
    public int SubscriptionId { get; set; }

    public string UserEmail { get; set; } = null!;

    public DateTime? SubscribedAt { get; set; }
}
