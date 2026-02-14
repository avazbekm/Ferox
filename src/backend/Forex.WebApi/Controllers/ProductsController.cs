namespace Forex.WebApi.Controllers;

using Forex.Application.Features.Products.Products.Commands;
using Forex.Application.Features.Products.Products.DTOs;
using Forex.Application.Features.Products.Products.Queries;
using Forex.WebApi.Controllers.Common;
using Forex.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

public class ProductsController
    : QueryControllers<ProductDto, GetAllProductsQuery, GetProductByIdQuery>
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
        => Ok(new Response { Data = await Mediator.Send(command, Ct) });

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductCommand command)
        => Ok(new Response { Data = await Mediator.Send(command, Ct) });

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
        => Ok(new Response { Data = await Mediator.Send(new DeleteProductCommand(id), Ct) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(ProductFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query, Ct) });
}
