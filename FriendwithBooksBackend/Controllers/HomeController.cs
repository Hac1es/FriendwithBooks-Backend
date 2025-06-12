using FriendwithBooksBackend.Data; 
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IBookRepository bookRepository, 
            IMemoryCache cache,
            ILogger<HomeController> logger)
        {
            _bookRepository = bookRepository;
            _cache = cache;
            _logger = logger;
        }

        // GET: api/Home/BestSellers
        [HttpGet("BestSellers")]
        public async Task<IActionResult> GetBestSeller()
        {
            try
            {
                if (_cache.TryGetValue("BestSellerData", out var cachedData))
                {
                    _logger.LogInformation("Retrieved best sellers from cache");
                    return Ok(cachedData);
                }

                _logger.LogWarning("Cache miss for best sellers, fetching from database");
                var bestSellers = await _bookRepository.GetBooks()
                    .Select(c => new
                    {
                        c.BookID,
                        c.Title,
                        c.Description,
                        ImgURL = c.ImgURL1
                    })
                    .Take(10)
                    .ToListAsync();

                return Ok(bestSellers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving best sellers");
                return StatusCode(500, "An error occurred while retrieving best sellers");
            }
        }

        // GET: api/Home/FlashSale
        [HttpGet("FlashSale")]
        public async Task<IActionResult> GetFlashSale()
        {
            try
            {
                if (_cache.TryGetValue("FlashSaleData", out var cachedData))
                {
                    _logger.LogInformation("Retrieved flash sale from cache");
                    return Ok(cachedData);
                }

                _logger.LogWarning("Cache miss for flash sale, fetching from database");
                var now = DateTime.UtcNow;
                var flashSale = await _bookRepository.GetFlashSale()
                    .Where(f => f.StartTime <= now && f.EndTime >= now)
                    .Join(
                        _bookRepository.GetBooks(),
                        flash => flash.BookID,
                        book => book.BookID,
                        (flash, book) => new
                        {
                            book.BookID,
                            book.Title,
                            book.Price,
                            ImgURL = book.ImgURL1,
                            flash.DiscountPercent,
                            flash.StartTime,
                            flash.EndTime
                        }
                    )
                    .ToListAsync();

                return Ok(flashSale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flash sale");
                return StatusCode(500, "An error occurred while retrieving flash sale");
            }
        }
    }
}
