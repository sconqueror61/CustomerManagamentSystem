using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Customer
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public int? Referance { get; set; }

    public string Code { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string Mail { get; set; } = null!;

    public string Tel { get; set; } = null!;

    public string? ServiceArea { get; set; }

    public bool IsDeleted { get; set; }

    public int? ServiceId { get; set; }

    public DateTime? RecordDate { get; set; }

    public int? CreaterUserId { get; set; }
}
