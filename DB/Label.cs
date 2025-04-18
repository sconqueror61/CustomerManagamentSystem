using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Label
{
    public int Id { get; set; }

    public string Description { get; set; } = null!;
}
