using System;
using System.Collections.Generic;

namespace SE160445.ProductManagement.Repo.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int CategoryId { get; set; }

    public int UnitsOfStock { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Category Category { get; set; } = null!;
}
