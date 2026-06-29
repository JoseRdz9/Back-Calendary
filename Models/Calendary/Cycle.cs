// namespace MenstrualCycleApi.Models;
 
// public class Cycle
// {
//     public Guid Id { get; set; }
//     public Guid UserId { get; set; }
//     public DateOnly StartDate { get; set; }
//     public DateOnly? EndDate { get; set; }
//     public int? CycleLength { get; set; }
//     public int? PeriodLength { get; set; }
//     public DateOnly? OvulationDate { get; set; }
//     public bool IsActive { get; set; }
//     public string? Notes { get; set; }
//     public DateTime CreatedAt { get; set; }
// }
using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Back_Calendary.Models.Calendary
{
    [Table("cycles")]
    public class Cycle : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }
        
        [Column("user_id")]
        public Guid UserId { get; set; }
        
        [Column("start_date")]
        public DateOnly StartDate { get; set; }
        
        [Column("end_date")]
        public DateOnly? EndDate { get; set; }
        
        [Column("cycle_length")]
        public int? CycleLength { get; set; }
        
        [Column("period_length")]
        public int? PeriodLength { get; set; }
        
        [Column("ovulation_date")]
        public DateOnly? OvulationDate { get; set; }
        
        [Column("is_active")]
        public bool IsActive { get; set; }
        
        [Column("notes")]
        public string? Notes { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        
        
    }

    
}
