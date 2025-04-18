using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class ProductImage
{
    public string Id { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public string? PictureId { get; set; }
}
