using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Session
{
    public int Id { get; set; }

    public DateTime EnterTime { get; set; }

    public DateTime? ExitTime { get; set; }

    public int CustomerId { get; set; }

    public string? Culture { get; set; }

    public string? Ipadress { get; set; }
}
