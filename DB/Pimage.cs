using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Pimage
{
    public int Id { get; set; }

    public string? PictureUrl { get; set; }

    public int? ProductId { get; set; }

    public int? CreaterUserId { get; set; }
}
