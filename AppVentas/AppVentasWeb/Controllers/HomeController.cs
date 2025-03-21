using AppVentasWeb.Common;
using AppVentasWeb.Data;
using AppVentasWeb.Data.Entidades;
using AppVentasWeb.DTOs;
using AppVentasWeb.Helper;
using AppVentasWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace AppVentasWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContex _context;
        private readonly IUserHelper _userHelper;
        private readonly IOrdersHelper _ordersHelper;

        public HomeController(ILogger<HomeController> logger, DataContex context, IUserHelper userHelper, IOrdersHelper ordersHelper)
        {
            _logger = logger;
            _context = context;
            _userHelper = userHelper;
            _ordersHelper = ordersHelper;
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "NameDesc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "PriceDesc" : "Price";
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewData["CurrentFilter"] = searchString;


            IQueryable<Producto> query = _context.Productos
            .Include(p => p.ProductImages)
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Categoria);
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => (p.Name.ToLower().Contains(searchString.ToLower()) ||
                p.ProductCategories.Any(pc => pc.Categoria.Nombre.ToLower().Contains(searchString.ToLower()))) &&
                p.Stock > 0);
            }
            else
            {
                query = query.Where(p => p.Stock > 0);
            }

            switch (sortOrder)
            {
                case "NameDesc":
                    query = query.OrderByDescending(p => p.Name);
                    break;

                case "Price":
                    query = query.OrderBy(p => p.Price);
                    break;

                case "PriceDesc":
                    query = query.OrderByDescending(p => p.Price);
                    break;

                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            int pageSize = 8;

            HomeViewModel model = new()
            {
                Productos = await PaginatedList<Producto>.CreateAsync(query, pageNumber ?? 1, pageSize),
                Categorias = await _context.Categorias.ToListAsync(),
            };

            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user != null)
            {
                model.Quantity = await _context.TemporalSales
                                        .Where(ts => ts.User.Id == user.Id)
                                        .SumAsync(ts => ts.Quantity);

                ViewBag.Quantity = model.Quantity;

            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("error/404")]
        public IActionResult Error404()
        {
            return View();
        }

        public async Task<IActionResult> Add(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            Producto producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }
            TemporalSale temporalSale = new()
            {
                Producto = producto,
                Quantity = 1,
                User = user
            };
            _context.TemporalSales.Add(temporalSale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Producto product = await _context.Productos
            .Include(p => p.ProductImages)
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            string categories = string.Empty;
            foreach (ProductCategory? category in product.ProductCategories)
            {
                categories += $"{category.Categoria.Nombre}, ";
            }
            categories = categories.Substring(0, categories.Length - 2);

            AddProductToCartViewModel model = new()
            {
                Categories = categories,
                Description = product.Description,
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                ProductImages = product.ProductImages,
                Quantity = 1,
                Stock = product.Stock,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(AddProductToCartViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            Producto product = await _context.Productos.FindAsync(model.Id);
            if (product == null)
            {
                return NotFound();
            }
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }
            TemporalSale temporalSale = new()
            {
                Producto = product,
                Quantity = model.Quantity,
                Remarks = model.Remarks,
                User = user
            };
            _context.TemporalSales.Add(temporalSale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> ShowCart()
        {
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }
            List<TemporalSale> temporalSales = await _context.TemporalSales
            .Include(ts => ts.Producto)
            .ThenInclude(p => p.ProductImages)
            .Where(ts => ts.User.Id == user.Id)
            .ToListAsync();

            ShowCartViewModel model = new()
            {
                User = user,
                TemporalSales = temporalSales,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShowCart(ShowCartViewModel model)
        {
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }
            model.User = user;
            model.TemporalSales = await _context.TemporalSales
            .Include(ts => ts.Producto)
            .ThenInclude(p => p.ProductImages)
            .Where(ts => ts.User.Id == user.Id)
            .ToListAsync();

            TempData["MontoTotal"] = model.Value.ToString();
            return RedirectToAction("Index", "WebPay");

            //Response response = await _ordersHelper.ProcessOrderAsync(model);

            //if (response.IsSuccess)
            //{
            //    //return RedirectToAction(nameof(OrderSuccess));
            //}
            //ModelState.AddModelError(string.Empty, response.Message);
            //return View(model);
        }

        public async Task<IActionResult> DecreaseQuantity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(id);
            if (temporalSale == null)
            {
                return NotFound();
            }
            if (temporalSale.Quantity > 1)
            {
                temporalSale.Quantity--;
                _context.TemporalSales.Update(temporalSale);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ShowCart));
        }

        public async Task<IActionResult> IncreaseQuantity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(id);
            if (temporalSale == null)
            {
                return NotFound();
            }
            temporalSale.Quantity++;
            _context.TemporalSales.Update(temporalSale);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ShowCart));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(id);
            if (temporalSale == null)
            {
                return NotFound();
            }
            _context.TemporalSales.Remove(temporalSale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ShowCart));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(id);
            if (temporalSale == null)
            {
                return NotFound();
            }

            EditTemporalSaleViewModel model = new()
            {
                Id = temporalSale.Id,
                Quantity = temporalSale.Quantity,
                Remarks = temporalSale.Remarks,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditTemporalSaleViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    TemporalSale temporalSale = await _context.TemporalSales.FindAsync(id);
                    temporalSale.Quantity = model.Quantity;
                    temporalSale.Remarks = model.Remarks;
                    _context.Update(temporalSale);
                    await _context.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                    return View(model);
                }
                return RedirectToAction(nameof(ShowCart));
            }
            return View(model);
        }

        [Authorize]
        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}