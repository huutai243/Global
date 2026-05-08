using AutoMapper;
using ECommerce.Catalog.Core.Models;
using ECommerce.Catalog.Core.Responses;

namespace ECommerce.Catalog.Service.Mapping;

public sealed class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Product, ProductResponse>()
            .ForMember(response => response.Status, options => options.MapFrom(product => product.Status.ToString()));
    }
}
