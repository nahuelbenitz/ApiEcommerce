using ApiEcommerce.Models;
using ApiEcommerce.Repository.IRepository;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ApiEcommerce.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public ProductRepository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool BuyProduct(string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name) || quantity <= 0)
            {
                return false;
            }

            var product = _context.Products.FirstOrDefault(p => p.Name.ToLower().Trim() == name.ToLower().Trim());

            if (product is null || product.Stock < quantity)
            {
                return false;
            }

            product.Stock -= quantity;
            _context.Products.Update(product);

            return Save();
        }

        public bool CreateProduct(Product product)
        {
            if (product is null)
            {
                return false;
            }

            product.CreationDate = DateTime.Now;
            product.UpdateDate = DateTime.Now;

            _context.Products.Add(product);
            return Save();
        }

        public bool DeleteProduct(Product product)
        {
            if (product is null)
            {
                return false;
            }

            _context.Products.Remove(product);
            return Save();
        }

        public Product? GetProduct(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            return _context.Products.Include(p => p.Category).FirstOrDefault(x => x.Id == id);

        }

        public ICollection<Product> GetProductForCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return new List<Product>();
            }

            return _context.Products.Include(x => x.Category).Where(p => p.CategoryId == categoryId).OrderBy(x => x.Name).ToList();
        }

        public ICollection<Product> GetProducts()
        {
            return _context.Products.Include(p => p.Category).OrderBy(x => x.Description).ToList();
        }

        public bool ProductExists(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return _context.Products.Any(x => x.Id == id);
        }

        public bool ProductExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return _context.Products.Any(p => p.Name.ToLower().Trim() == name.ToLower().Trim());
        }

        public bool Save()
        {
            return _context.SaveChanges() >= 0;
        }

        public ICollection<Product> SearchProduct(string name)
        {
            IQueryable<Product> query = _context.Products;

            string searchTermLower = name.ToLower().Trim();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Include(x => x.Category).Where(p => p.Name.ToLower().Trim().Contains(searchTermLower) || p.Description.ToLower().Trim().Contains(searchTermLower));
            }

            return query.OrderBy(p => p.Name).ToList();
        }

        public bool UpdateProduct(Product product)
        {
            if (product is null)
            {
                return false;
            }

            product.UpdateDate = DateTime.Now;
            _context.Products.Update(product);
            return Save();
        }
    }
}
