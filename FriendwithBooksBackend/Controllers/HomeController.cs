using FriendwithBooksBackend.Data; 
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : Controller
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMemoryCache _cache;

        public HomeController(IBookRepository bookRepository, IMemoryCache cache)
        {
            _bookRepository = bookRepository;
            _cache = cache;
        }

        // GET: api/Home/BestSellers
        [HttpGet("BestSellers")]
        public IActionResult GetBestSeller()
        {
            if (_cache.TryGetValue("BestSellerData", out var cachedData))
                return Ok(cachedData);

            return Ok(new List<object>());
        }

        // GET: api/Home/FlashSale
        [HttpGet("FlashSale")]
        public IActionResult GetFlashSale()
        {
            if (_cache.TryGetValue("FlashSaleData", out var cachedData))
                return Ok(cachedData);

            return Ok(new List<object>());
        }
    }
}
