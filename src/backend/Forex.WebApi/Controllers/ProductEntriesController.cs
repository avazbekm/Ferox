namespace Forex.WebApi.Controllers;

using Forex.Application.Features.Files.Queries.GetPresignedUrl;
using Forex.Application.Features.Products.ProductEntries.Commands;
using Forex.Application.Features.Products.ProductEntries.Queries;
using Forex.WebApi.Controllers.Common;
using Forex.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

public class ProductEntriesController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductEntryCommand command)
        => Ok(new Response { Data = await Mediator.Send(command, Ct) });

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProductEntryCommand command)
        => Ok(new Response { Data = await Mediator.Send(command, Ct) });

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
        => Ok(new Response { Data = await Mediator.Send(new DeleteProductEntryCommand(id), Ct) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(ProductEntryFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query, Ct) });

    [HttpPost("image/upload-url")]
    public async Task<IActionResult> GenerateImageUploadUrl(GenerateUploadUrlRequest request)
    {
        // Auto-detect MinIO public endpoint from request host
        var requestHost = $"{Request.Host}";

        return Ok(new Response
        {
            Data = await Mediator.Send(
                new GetPresignedUrlQuery(request.FileName, "products", requestHost),
                Ct)
        });
    }
}

public sealed record GenerateUploadUrlRequest
{
    public required string FileName { get; init; }
}
