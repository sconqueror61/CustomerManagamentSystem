using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class OrdersDetail
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public int? SupplierId { get; set; }

    public double? Price { get; set; }

    public int? Amount { get; set; }

    public int? ProductId { get; set; }

    public int? UserId { get; set; }

    public int? StatusId { get; set; }

    public DateTime? Date { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? RecordDate { get; set; }
}
