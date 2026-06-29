using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Models.Calendary;
using Supabase;
 
namespace Back_Calendary.Repositories.Calendary
{
    public class CycleRepository : ICycleRepository
    {
        private readonly Client _supabase;
 
        public CycleRepository(Client supabase)
        {
            _supabase = supabase;
        }
 
        public async Task<IReadOnlyList<Cycle>> GetByUserAsync(Guid userId, int limit = 24)
        {
            var response = await _supabase
                .From<Cycle>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Order("start_date", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();
 
            return response.Models;
        }
 
        public async Task<Cycle?> GetActiveAsync(Guid userId)
        {
            var response = await _supabase
                .From<Cycle>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Single();
 
            return response;
        }
 
        public async Task<Guid> CreateAsync(Cycle cycle)
        {
            var response = await _supabase.From<Cycle>().Insert(cycle);
            return response.Models[0].Id;
        }
 
        public async Task CloseAsync(Guid cycleId, DateOnly endDate, int cycleLength, int periodLength)
        {
            await _supabase
                .From<Cycle>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cycleId.ToString())
                .Set(c => c.EndDate, endDate)
                .Set(c => c.CycleLength, cycleLength)
                .Set(c => c.PeriodLength, periodLength)
                .Set(c => c.IsActive, false)
                .Update();
        }

        public async Task UpdateAsync(Cycle cycle)
        {
            await _supabase
                .From<Cycle>()
                .Where(c => c.Id == cycle.Id)
                .Set(c => c.EndDate, cycle.EndDate)
                .Set(c => c.PeriodLength, cycle.PeriodLength)
                // Solo actualizar estos campos, NO IsActive
                .Update();
        }
    }
}