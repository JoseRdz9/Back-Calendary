using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.DTOs.Calendary;
using Back_Calendary.Models.Calendary;
 
namespace Back_Calendary.Interfaces.Calendary
{
    public interface ICycleService
    {
        Task StartNewCycleAsync(Guid userId, DateOnly startDate);
        Task CloseCurrentCycleAsync(Guid userId, DateOnly endDate);
        // Task<Cycle?> GetActiveCycleAsync(Guid userId);
        Task<IReadOnlyList<CycleDto>> GetAllAsync(Guid userId);
        Task<PredictionResponseDto> GetPredictionAsync(Guid userId);
        Task<CycleResponseDto?> GetActiveCycleAsync(Guid userId);

        
    }
}
 