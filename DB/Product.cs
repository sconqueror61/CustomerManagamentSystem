using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class Product
{
    public int Id { get; set; }

    public string? Description { get; set; }

    public string? Explanation { get; set; }

    public double Price { get; set; }

    public int CategoryId { get; set; }

    public bool? Breakibility { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }

    public int? CreaterUserId { get; set; }
}
