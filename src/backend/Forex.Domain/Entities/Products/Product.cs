﻿namespace Forex.Domain.Entities.Products;

using Forex.Domain.Commons;

public class Product : Auditable
{
    public string Name { get; set; } = string.Empty;
    public string? NormalizedName { get; set; } = string.Empty;
    public int Code { get; set; }  // mahsulot kodi 101 misol uchun
    public string? ImagePath { get; set; } = string.Empty;

    public long UnitMeasureId { get; set; }
    public UnitMeasure UnitMeasure { get; set; } = default!;

    public ICollection<ProductType> ProductTypes { get; set; } = default!;
}
