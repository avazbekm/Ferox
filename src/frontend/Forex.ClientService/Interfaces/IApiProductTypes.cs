namespace Forex.ClientService.Interfaces;

using Forex.ClientService.Models.Commons;
using Forex.ClientService.Models.Requests;
using Forex.ClientService.Models.Responses;
using Refit;

public interface IApiProductTypes
{
    [Get("/product-types")]
    Task<Response<List<ProductTypeResponse>>> GetAll();

    [Post("/product-types/filter")]
    Task<Response<List<ProductTypeResponse>>> Filter(FilteringRequest request);

    [Post("/product-types")]
    Task<Response<long>> Create([Body] ProductTypeRequest request);

    [Put("/product-types")]
    Task<Response<bool>> Update([Body] ProductTypeRequest request);

    [Delete("/product-types/{id}")]
    Task<Response<bool>> Delete(long id);
}
