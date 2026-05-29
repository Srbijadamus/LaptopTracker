using LaptopTracker.Data;
using LaptopTracker.Models.Agent;
using Microsoft.EntityFrameworkCore;

namespace LaptopTracker.Services
{
    public interface IIdempotencyService
    {
        Task<string?> GetCachedResponseAsync(string requestId, string endpoint);
        Task StoreResponseAsync(string requestId, string endpoint, string responseJson);
    }

    public class IdempotencyService : IIdempotencyService
    {
        private readonly LaptopTrackerDbContext _context;

        public IdempotencyService(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetCachedResponseAsync(string requestId, string endpoint)
        {
            var record = await _context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.Endpoint == endpoint);
            return record?.ResponseJson;
        }

        public async Task StoreResponseAsync(string requestId, string endpoint, string responseJson)
        {
            _context.IdempotencyRecords.Add(new IdempotencyRecord
            {
                RequestId    = requestId,
                Endpoint     = endpoint,
                ResponseJson = responseJson,
                CreatedAt    = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
