using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class OrdersHistory
{
    public int Id { get; set; }

    public DateTime? Date { get; set; }

    public int? OrderDetailId { get; set; }

    public int? StatusId { get; set; }
}
