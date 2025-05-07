using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class UserBasket
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ProductId { get; set; }

    public DateTime? RecordDate { get; set; }

    public int? Amount { get; set; }

    public bool? IsDeleted { get; set; }
}
