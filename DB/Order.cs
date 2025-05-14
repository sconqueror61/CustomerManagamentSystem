using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Order
{
    public int Id { get; set; }

    public int? TotalAmount { get; set; }

    public double? TotalPrice { get; set; }

    public int? UserId { get; set; }

    public DateTime? Date { get; set; }

    public int? StatusId { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? RecordDate { get; set; }
}
