using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Service
{
    public int Id { get; set; }

    public string Description { get; set; } = null!;

    public int? CreaterUserId { get; set; }

    public int? Cost { get; set; }

    public virtual User? CreaterUser { get; set; }
}
