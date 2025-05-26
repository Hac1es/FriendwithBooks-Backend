using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FriendwithBooksBackend.Services
{
    public class CachingServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private const string FlashSaleCacheKey = "FlashSaleData";
        private const string BestSellerCacheKey = "BestSellerData";
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(10);

        public CachingServices(IServiceProvider serviceProvider, IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IBookRepository>();

                // Cache FlashSale
                var now = DateTime.UtcNow;
                var latestStartTime = await repo.GetFlashSale()
                    .Where(f => f.StartTime <= now && f.EndTime >= now)
                    .MaxAsync(f => (DateTime?)f.StartTime, stoppingToken);

                if (latestStartTime != null)
                {
                    var utcStartTime = DateTime.SpecifyKind(latestStartTime.Value, DateTimeKind.Utc);

                    var flashSaleData = await repo.GetBooks()
                        .Join(
                            repo.GetFlashSale().Where(f => f.StartTime == utcStartTime),
                            book => book.BookID,
                            flash => flash.BookID,
                            (book, flash) => new
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
                        .ToListAsync(stoppingToken);

                    _cache.Set(FlashSaleCacheKey, flashSaleData, _refreshInterval);
                }
                else
                {
                    _cache.Remove(FlashSaleCacheKey);
                }

                // Cache BestSeller
                var bestSellerData = await repo.GetBooks()
                    .Select(c => new
                    {
                        c.BookID,
                        c.Title,
                        c.Description,
                        ImgURL = c.ImgURL1
                    })
                    .Take(10)
                    .ToListAsync(stoppingToken);

                _cache.Set(BestSellerCacheKey, bestSellerData, _refreshInterval);

                await Task.Delay(_refreshInterval, stoppingToken);
            }
        }
    }
}