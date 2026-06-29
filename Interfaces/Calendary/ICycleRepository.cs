using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.Models.Calendary;
 
namespace Back_Calendary.Interfaces.Calendary
{
    public interface ICycleRepository
    {
        Task<IReadOnlyList<Cycle>> GetByUserAsync(Guid userId, int limit = 24);
        Task<Cycle?> GetActiveAsync(Guid userId);
        Task<Guid> CreateAsync(Cycle cycle);
        Task CloseAsync(Guid cycleId, DateOnly endDate, int cycleLength, int periodLength);
        Task UpdateAsync(Cycle cycle);
    }
}