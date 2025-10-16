﻿namespace Forex.Domain.Entities.Products;

using Forex.Domain.Commons;
using Forex.Domain.Entities;

public class ProductEntry : Auditable
{
    public int Count { get; set; }
    public decimal CostPrice { get; set; }     // tannarxi
    public decimal CostPreparation { get; set; }  // tayyorlashga ketgan xarajat summasi

    public long ProductTypeId { get; set; }  // 
    public ProductType ProductType { get; set; } = default!;  // razmeri 24-29, 30-35, 36-41

    public long ShopId { get; set; }
    public Shop Shop { get; set; } = default!;

    public long EmployeeId { get; set; }
    public User Employee { get; set; } = default!;
}