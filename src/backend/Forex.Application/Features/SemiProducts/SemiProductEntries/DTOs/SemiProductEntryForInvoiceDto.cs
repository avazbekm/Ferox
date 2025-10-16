﻿namespace Forex.Application.Features.SemiProducts.SemiProductEntries.DTOs;

using Forex.Application.Features.Manufactories.DTOs;
using Forex.Application.Features.SemiProducts.SemiProducts.DTOs;

public sealed record SemiProductEntryForInvoiceDto
{
    public long Id { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CostDelivery { get; set; }
    public decimal TransferFee { get; set; }

    public long InvoiceId { get; set; }

    public long SemiProductId { get; set; }
    public SemiProductForSemiProductEntryDto SemiProduct { get; set; } = default!;

    public long ManufactoryId { get; set; }
    public ManufactoryForSemiProductEntryDto Manufactory { get; set; } = default!;
}