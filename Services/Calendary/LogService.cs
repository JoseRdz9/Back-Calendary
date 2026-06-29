using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.DTOs.Calendary;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Models.Calendary;
 
namespace Back_Calendary.Services.Calendary
{
    public class LogService : ILogService
    {
        private readonly ILogRepository _logRepo;
        private readonly ICycleRepository _cycleRepo;
 
        public LogService(ILogRepository logRepo, ICycleRepository cycleRepo)
        {
            _logRepo   = logRepo;
            _cycleRepo = cycleRepo;
        }
 
        public async Task UpsertAsync(Guid userId, LogCreateDto dto)
        {
            var active = await _cycleRepo.GetActiveAsync(userId);
 
            var log = new DailyLog
            {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                CycleId      = active?.Id,
                LogDate      = dto.LogDate,
                FlowIntensity = dto.FlowIntensity,
                BasalTemp    = dto.BasalTemp,
                CervicalMucus = dto.CervicalMucus,
                MoodScore    = dto.MoodScore,
                PainLevel    = dto.PainLevel,
                EnergyLevel  = dto.EnergyLevel,
                Notes        = dto.Notes
            };
 
            await _logRepo.UpsertAsync(log);
        }
 
        public async Task<IReadOnlyList<DailyLog>> GetRecentAsync(Guid userId, int days = 30)
            => await _logRepo.GetRecentAsync(userId, days);
    }
}