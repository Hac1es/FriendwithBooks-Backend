using FriendwithBooksBackend.Data; 
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public HomeController(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        // GET: api/Home/BestSellers
        [HttpGet("BestSellers")]
        public async Task<IActionResult> GetBestSeller()
        {
            var result = await _bookRepository.GetBooks()
            .Select(c => new
            {
                Title = c.Title,
                Description = c.Description,
                ImgURL = c.ImgURL
            })
            .Take(10)
            .ToListAsync();
            var count = await _bookRepository.GetBooks().CountAsync();
            Console.WriteLine("Count: " + count);
            return Ok(result);
        }
    }
}
