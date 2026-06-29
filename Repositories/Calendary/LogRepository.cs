using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Models.Calendary;
using Supabase;
 
namespace Back_Calendary.Repositories.Calendary
{
    public class LogRepository : ILogRepository
    {
        private readonly Client _supabase;
 
        public LogRepository(Client supabase)
        {
            _supabase = supabase;
        }
 
        public async Task<IReadOnlyList<DailyLog>> GetRecentAsync(Guid userId, int days = 60)
        {
            string cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-days))
                                    .ToString("yyyy-MM-dd");
 
            var response = await _supabase
                .From<DailyLog>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("log_date", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, cutoff)
                .Order("log_date", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();
 
            return response.Models;
        }
 
        public async Task<IReadOnlyList<DailyLog>> GetByCycleAsync(Guid cycleId)
        {
            var response = await _supabase
                .From<DailyLog>()
                .Filter("cycle_id", Supabase.Postgrest.Constants.Operator.Equals, cycleId.ToString())
                .Get();
 
            return response.Models;
        }
 
        public async Task UpsertAsync(DailyLog log)
        {
            await _supabase.From<DailyLog>().Upsert(log);
        }
    }
}