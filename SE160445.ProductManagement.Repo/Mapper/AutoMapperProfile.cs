using AutoMapper;
using SE160445.ProductManagement.Repo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SE160445.ProductManagement.Repo.DTOs;
using SE160445.ProductManagement.Repo.DTOs.Product;
using SE160445.ProductManagement.Repo.DTOs.Category;

namespace SE160445.ProductManagement.Repo.Mapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Product, ProductRes>().ReverseMap();
            CreateMap<ProductReq, Product>().ReverseMap();
            CreateMap<ProductReq, Product>().ReverseMap();
            CreateMap<CategoryReq, Category>().ReverseMap();
            CreateMap<ProductRes, Product>().ReverseMap();
            CreateMap<CategoryRes, Category>().ReverseMap();

        }
    }
}
