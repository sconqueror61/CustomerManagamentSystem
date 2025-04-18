using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Pcategory
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryDesc { get; set; } = null!;

    public int? CreaterUserId { get; set; }
}
