using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.Models.Calendary;
 
namespace Back_Calendary.Interfaces.Calendary
{
    public interface ILogRepository
    {
        Task<IReadOnlyList<DailyLog>> GetRecentAsync(Guid userId, int days = 60);
        Task<IReadOnlyList<DailyLog>> GetByCycleAsync(Guid cycleId);
        Task UpsertAsync(DailyLog log);
    }
}