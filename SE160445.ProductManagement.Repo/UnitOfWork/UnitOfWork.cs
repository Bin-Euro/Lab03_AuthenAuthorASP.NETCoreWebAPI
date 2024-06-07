using SE160445.ProductManagement.Repo.Models;
using SE160445.ProductManagement.Repo.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SE160445.ProductManagement.Repo.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PhungnnseContext _context;
        private IGenericRepository<Category> _categoryRepository;
        private IGenericRepository<Product> _productRepository;

        public UnitOfWork(PhungnnseContext context)
        {
            _context = context;
        }

        public IGenericRepository<Category> CategoryRepository
        {
            get
            {
                return _categoryRepository ??= new GenericRepository<Category>(_context);
            }
        }

        public IGenericRepository<Product> ProductRepository
        {
            get
            {
                return _productRepository ??= new GenericRepository<Product>(_context);
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
