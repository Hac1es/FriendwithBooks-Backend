using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FriendwithBooksBackend.Services
{
    public class CachingServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingServices> _logger;
        private const string FlashSaleCacheKey = "FlashSaleData";
        private const string BestSellerCacheKey = "BestSellerData";
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(2);

        public CachingServices(
            IServiceProvider serviceProvider, 
            IMemoryCache cache,
            ILogger<CachingServices> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateCacheData(stoppingToken);
                    await Task.Delay(_refreshInterval, stoppingToken);
                }
                catch (Exception ex) when (ex is DbUpdateException || ex is InvalidOperationException)
                {
                    _logger.LogError(ex, "Database error while updating cache. Will retry in {RetryInterval} minutes", 
                        _retryInterval.TotalMinutes);
                    await Task.Delay(_retryInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Normal shutdown, don't report as error
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in caching service. Will retry in {RetryInterval} minutes", 
                        _retryInterval.TotalMinutes);
                    await Task.Delay(_retryInterval, stoppingToken);
                }
            }
        }

        private async Task UpdateCacheData(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBookRepository>();

            // Cache FlashSale
            try
            {
                await UpdateFlashSaleCache(repo, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update flash sale cache");
                // Continue with best seller cache even if flash sale fails
            }

            // Cache BestSeller
            try
            {
                await UpdateBestSellerCache(repo, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update best seller cache");
            }
        }

        private async Task UpdateFlashSaleCache(IBookRepository repo, CancellationToken stoppingToken)
        {
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
                            book.Author,
                            ImgURL = book.ImgURL1,
                            flash.DiscountPercent,
                            flash.StartTime,
                            flash.EndTime
                        }
                    )
                    .ToListAsync(stoppingToken);

                _cache.Set(FlashSaleCacheKey, flashSaleData, _refreshInterval);
                _logger.LogInformation("Updated flash sale cache with {Count} items", flashSaleData.Count);
            }
            else
            {
                _cache.Remove(FlashSaleCacheKey);
                _logger.LogInformation("No active flash sales found, removed from cache");
            }
        }

        private async Task UpdateBestSellerCache(IBookRepository repo, CancellationToken stoppingToken)
        {
            var bestSellerData = await repo.GetBooks()
                .OrderByDescending(c => c.BookID)
                .Select(c => new
                {
                    c.BookID,
                    c.Title,
                    c.Author,
                    c.Description,
                    ImgURL = c.ImgURL1
                })
                .Take(10)
                .ToListAsync(stoppingToken);

            _cache.Set(BestSellerCacheKey, bestSellerData, _refreshInterval);
            _logger.LogInformation("Updated best seller cache with {Count} items", bestSellerData.Count);
        }
    }
}