using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE160445.ProductManagement.Repo.Constants;
using SE160445.ProductManagement.Repo.DTOs.Product;
using SE160445.ProductManagement.Repo.Models;
using SE160445.ProductManagement.Repo.UnitOfWork;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SE160445.ProductManagement.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor for ProductsController
        /// </summary>
        /// <param name="unitOfWork">UnitOfWork instance for database operations</param>
        /// <param name="mapper">Mapper instance for mapping models to entities</param>
        public ProductsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves list products.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetListProducts(
    [FromQuery(Name = "page")][Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")] int page = 1,
            [FromQuery(Name = "page-size")][Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than 0")] int pagesize = 10,
            [FromQuery(Name = "sort-direction")] Sort sortdirection = Sort.asc,
            [FromQuery(Name = "search")] string? search = "",
            [FromQuery(Name = "min-stock")][Range(0, int.MaxValue, ErrorMessage = "Min stock must be greater than or equal to 0")] int? minstock = null,
            [FromQuery(Name = "max-stock")][Range(0, int.MaxValue, ErrorMessage = "Max stock must be greater than or equal to 0")] int? maxstock = null,
            [FromQuery(Name = "min-price")][Range(0, double.MaxValue, ErrorMessage = "Min price must be greater than or equal to 0")] decimal? minprice = null,
            [FromQuery(Name = "max-price")][Range(0, double.MaxValue, ErrorMessage = "Max price must be greater than or equal to 0")] decimal? maxprice = null)
        {
            try
            {
                var validationResults = new List<ValidationResult>();

                // Check minStock < maxStock
                if (minstock.HasValue && maxstock.HasValue && minstock >= maxstock)
                {
                    return BadRequest("Min stock must be less than max stock");
                }

                // Check minPrice < maxPrice
                if (minprice.HasValue && maxprice.HasValue && minprice >= maxprice)
                {
                    return BadRequest("Min price must be less than max price");
                }

                var products = await _unitOfWork.ProductRepository.GetListAsync();

                // Filter by search keyword
                if (!string.IsNullOrEmpty(search))
                {
                    products = products.Where(p => p.ProductName.Contains(search));
                }

                // Filter by stock units range
                if (minstock.HasValue)
                {
                    products = products.Where(p => p.UnitsOfStock >= minstock);
                }
                if (maxstock.HasValue)
                {
                    products = products.Where(p => p.UnitsOfStock <= maxstock);
                }

                // Filter by unit price range
                if (minprice.HasValue)
                {
                    products = products.Where(p => p.UnitPrice >= minprice);
                }
                if (maxprice.HasValue)
                {
                    products = products.Where(p => p.UnitPrice <= maxprice);
                }

                // Sort the products
                if (minstock.HasValue || maxstock.HasValue)
                {
                    products = sortdirection == Sort.asc ? products.OrderBy(p => p.UnitsOfStock) : products.OrderByDescending(p => p.UnitsOfStock);
                }
                else if (minprice.HasValue || maxprice.HasValue)
                {
                    products = sortdirection == Sort.asc ? products.OrderBy(p => p.UnitPrice) : products.OrderByDescending(p => p.UnitPrice);
                }
                else
                {
                    products = sortdirection == Sort.asc ? products.OrderBy(p => p.ProductName) : products.OrderByDescending(p => p.ProductName);
                }

                // Pagination
                var totalCount = products.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pagesize);
                products = products.Skip((page - 1) * pagesize).Take(pagesize);
                var productRess = _mapper.Map<IEnumerable<ProductRes>>(products);

                // Lấy danh sách tuyên bố từ mã thông báo JWT
                var claims = User.Claims;

                // Kiểm tra xem có tuyên bố roles không
                var rolesClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                // Kiểm tra xem tuyên bố roles có chứa giá trị "Admin" hay không
                if (rolesClaim != null && rolesClaim.Value == "Admin")
                {
                    // Nếu có, cho phép truy cập vào tài nguyên
                    // Đoạn mã xử lý của bạn ở đây
                    return Ok(new { totalcount = totalCount, totalpages = totalPages, data = productRess });
                }
                else
                {
                    // Nếu không, từ chối truy cập và trả về lỗi 403 Forbidden
                    return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this resource.");
                }

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving data: {ex.Message}");
            }
        }


        /// <summary>
        /// Retrieves a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to retrieve</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                if (product == null)
                    return NotFound($"Product with ID {id} not found");
                var productRes = _mapper.Map<ProductRes>(product);


                return Ok(productRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving data: {ex.Message}");
            }
        }


        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="productRes">The product object to create</param>
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductReq productReq)
        {
            try
            {
                if (productReq == null)
                    return BadRequest("Product object is null");

                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(productReq.CategoryId);
                if (category == null)
                    return BadRequest($"Category with ID {productReq.CategoryId} does not exist");

                var product = _mapper.Map<Product>(productReq);

                await _unitOfWork.ProductRepository.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                var createdProductRes = _mapper.Map<ProductRes>(product);


                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, createdProductRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating product: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="id">The ID of the product to update</param>
        /// <param name="productReq">The updated product object</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductReq productReq)
        {
            try
            {
                if (productReq == null || id != productReq.ProductId)
                    return BadRequest("Invalid product data");

                var existingProduct = await _unitOfWork.ProductRepository.GetByIdAsync(productReq.ProductId);
                if (existingProduct == null)
                    return NotFound($"Product with ID {id} not found");

                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(productReq.CategoryId);
                if (category == null)
                    return BadRequest($"Category with ID {productReq.CategoryId} does not exist");

                var product = _mapper.Map(productReq, existingProduct);

                await _unitOfWork.ProductRepository.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                // Ánh xạ từ Products sang ProductRes
                var updatedProductRes = _mapper.Map<ProductRes>(product);


                return Ok(updatedProductRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating product: {ex.Message}");
            }
        }


        /// <summary>
        /// Deletes a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to delete</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var existingProduct = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                if (existingProduct == null)
                    return NotFound($"Product with ID {id} not found");

                await _unitOfWork.ProductRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                // Trả về thông báo thành công
                return Ok($"Product with ID {id} has been successfully deleted");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting product: {ex.Message}");
            }
        }
        /// <summary>
        /// Retrieves List products by category ID.
        /// </summary>
        /// <param name="categoryId">The ID of the category.</param>
        /// <returns>List of products that belong to the specified category.</returns>

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetListProductsByCategoryId(int categoryId)
        {
            try
            {
                var productsQuery = _unitOfWork.ProductRepository.GetAllAsQueryable()
                    .Where(p => p.CategoryId == categoryId);

                var products = await productsQuery.ToListAsync();
                var productRess = _mapper.Map<IEnumerable<ProductRes>>(products);

                return Ok(productRess);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving data: {ex.Message}");
            }
        }
    }
}
