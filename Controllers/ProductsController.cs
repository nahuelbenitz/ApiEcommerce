using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public ProductsController(IProductRepository productRepository, IMapper mapper, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _categoryRepository = categoryRepository;
        }


        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProducts()
        {
            var products = _productRepository.GetProducts();
            var productsDto = _mapper.Map<List<ProductDTO>>(products);

            return Ok(productsDto);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}", Name = "GetProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProduct(int id)
        {
            var product = _productRepository.GetProduct(id);
            if (product == null)
            {
                return NotFound($"El producto con el id {id} no existe");
            }
            var productDTO = _mapper.Map<ProductDTO>(product);
            return Ok(productDTO);
        }

        [HttpGet("searchProductByCategory/{id:int}", Name = "GetProductByCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProductByCategory(int id)
        {
            var products = _productRepository.GetProductForCategory(id);
            if (products.Count == 0)
            {
                return NotFound($"No hay productos asociados a la categoria con id {id}");
            }
            var productsDTO = _mapper.Map<List<ProductDTO>>(products);
            return Ok(productsDTO);
        }

        [HttpGet("searchProductByName/{name}", Name = "GetProductByName")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProductByName(string name)
        {
            var products = _productRepository.SearchProduct(name);
            if (products.Count == 0)
            {
                return NotFound($"No hay productos con el nombre {name} no existen");
            }
            var productsDTO = _mapper.Map<List<ProductDTO>>(products);
            return Ok(productsDTO);
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateProduct([FromBody] CreateProductDTO createProductDTO)
        {
            if (createProductDTO == null)
            {
                return BadRequest(ModelState);
            }

            if (_productRepository.ProductExists(createProductDTO.Name))
            {
                ModelState.AddModelError("CustomError", "El producto ya existe");
                return BadRequest(ModelState);
            }

            if (!_categoryRepository.CategoryExists(createProductDTO.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"La categoria con id {createProductDTO.CategoryId} no  existe");
                return BadRequest(ModelState);
            }

            var product = _mapper.Map<Product>(createProductDTO);

            if (!_productRepository.CreateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al guardar el registro {product.Name}");
                return StatusCode(500, ModelState);
            } 

            var createdProduct = _productRepository.GetProduct(product.Id);
            var producDTO = _mapper.Map<ProductDTO>(createdProduct);

            return CreatedAtRoute("GetProduct", new { id = product.Id }, producDTO);
        }

        [HttpPatch("buyProduct/{name}/{quantity:int}", Name = "BuyProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult BuyProduct(string name, int quantity)
        {
            if (string.IsNullOrEmpty(name) || quantity <= 0)
            {
                return BadRequest("El nombre del producto o la cantidad no son validos");
            }

            var foundProduct = _productRepository.ProductExists(name);

            if (!foundProduct)
            {
                return NotFound($"El producto con el nombre {name} no existe");
            }

            if (!_productRepository.BuyProduct(name, quantity))
            {
                ModelState.AddModelError("CustomError", $"No se pudo comprar el producto {name} o la cantidad solicitada es mayor al stock disponible");
                return BadRequest(ModelState);
            }

            var unidad = quantity > 1 ? "unidades" : "unidad";
            return Ok($"Se compro {quantity} {unidad} del producto {name}");

        }


        [HttpPut("{id:int}", Name = "UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateProduct(int id, [FromBody] UpdateProductDTO updateProductDTO)
        {
            if (updateProductDTO == null)
            {
                return BadRequest(ModelState);
            }

            if (!_productRepository.ProductExists(id))
            {
                ModelState.AddModelError("CustomError", "El producto no existe");
                return BadRequest(ModelState);
            }

            if (!_categoryRepository.CategoryExists(updateProductDTO.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"La categoria con id {updateProductDTO.CategoryId} no  existe");
                return BadRequest(ModelState);
            }

            var product = _mapper.Map<Product>(updateProductDTO);
            product.Id = id;

            if (!_productRepository.UpdateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al actualizar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }


        [HttpDelete("{id:int}", Name = "DeleteProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult DeleteProduct(int id)
        {
            if(id <= 0)
            {
                return BadRequest(ModelState);
            }

            var product = _productRepository.GetProduct(id);
            
            if (product == null)
            {
                return NotFound($"El producto con el id {id} no existe");
            }

            if (!_productRepository.DeleteProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al eliminar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }
    }
}
