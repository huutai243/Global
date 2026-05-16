using AutoMapper;
using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;

namespace ECommerce.Catalog.Service.Mapping;

public sealed class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Product, ProductResponse>()
            .ForMember(response => response.Status, options => options.MapFrom(product => product.Status.ToString()));
    }
}
