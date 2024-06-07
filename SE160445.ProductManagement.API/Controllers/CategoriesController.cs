using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SE160445.ProductManagement.Repo.Constants;
using SE160445.ProductManagement.Repo.DTOs.Category;
using SE160445.ProductManagement.Repo.Models;
using SE160445.ProductManagement.Repo.UnitOfWork;

namespace SE160445.ProductManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoriesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetListCategories(int page = 1, int pageSize = 10, Sort sortDirection = Sort.asc, string? search = null)
        {
            try
            {
                var categories = await _unitOfWork.CategoryRepository.GetListAsync();

                if (!string.IsNullOrEmpty(search))
                {
                    categories = categories.Where(c => c.CategoryName.Contains(search));
                }

                switch (sortDirection)
                {
                    case Sort.asc:
                        categories = categories.OrderBy(c => c.CategoryName);
                        break;
                    case Sort.desc:
                        categories = categories.OrderByDescending(c => c.CategoryName);
                        break;
                    default:
                        return BadRequest("Invalid sort direction");
                }

                var totalCount = categories.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                categories = categories.Skip((page - 1) * pageSize).Take(pageSize);

                var categoryRes = _mapper.Map<IEnumerable<CategoryRes>>(categories);

                return Ok(new { TotalCount = totalCount, TotalPages = totalPages, Data = categoryRes });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving data: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
                if (category == null)
                    return NotFound($"Category with ID {id} not found");

                var categoryRes = _mapper.Map<CategoryRes>(category);

                return Ok(categoryRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving data: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryReq categoryReq)
        {
            try
            {
                if (categoryReq == null)
                    return BadRequest("Category object is null");

                var category = _mapper.Map<Category>(categoryReq);

                await _unitOfWork.CategoryRepository.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                var createdCategoryRes = _mapper.Map<CategoryRes>(category);

                return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, createdCategoryRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating category: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryReq categoryReq)
        {
            try
            {
                if (categoryReq == null || id != categoryReq.CategoryID)
                    return BadRequest("Invalid category data");

                var existingCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                    return NotFound($"Category with ID {id} not found");

                var category = _mapper.Map(categoryReq, existingCategory);

                await _unitOfWork.CategoryRepository.UpdateAsync(category);
                await _unitOfWork.SaveChangesAsync();

                var updatedCategoryRes = _mapper.Map<CategoryRes>(category);

                return Ok(updatedCategoryRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating category: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var existingCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                    return NotFound($"Category with ID {id} not found");

                await _unitOfWork.CategoryRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                var deletedCategoryRes = _mapper.Map<CategoryRes>(existingCategory);

                return Ok(deletedCategoryRes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting category: {ex.Message}");
            }
        }
    }
}
