using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.DTOs.Calendary;
using Back_Calendary.Models.Calendary;
 
namespace Back_Calendary.Interfaces.Calendary
{
    public interface ILogService
    {
        Task UpsertAsync(Guid userId, LogCreateDto dto);
        Task<IReadOnlyList<DailyLog>> GetRecentAsync(Guid userId, int days = 30);
    }
}