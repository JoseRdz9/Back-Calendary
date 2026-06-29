using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Back_Calendary.Models.Calendary
// {
//     public class Prediction : BaseModel
//     {
//         public Guid Id { get; set; }
//         public Guid UserId { get; set; }
//         public DateOnly NextPeriodStart { get; set; }
//         public DateOnly NextPeriodLow { get; set; }
//         public DateOnly NextPeriodHigh { get; set; }
//         public decimal ConfidenceScore { get; set; }
//         public DateOnly? OvulationDate { get; set; }
//         public DateOnly? FertileWindowStart { get; set; }
//         public DateOnly? FertileWindowEnd { get; set; }
//         public decimal AvgCycleLength { get; set; }
//         public decimal StdDeviation { get; set; }
//         public int CyclesAnalyzed { get; set; }
//         public string CurrentPhase { get; set; } = string.Empty;
//         public int CurrentDayInCycle { get; set; }
//         public bool IsCurrent { get; set; }
//         public DateTime GeneratedAt { get; set; }
//     }
// }

{
    [Table("predictions")]
    public class Prediction : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }
        
        [Column("user_id")]
        public Guid UserId { get; set; }
        
        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; }
        
        [Column("next_period_start")]
        public DateOnly NextPeriodStart { get; set; }
        
        [Column("next_period_start_low")]
        public DateOnly NextPeriodLow { get; set; }
        
        [Column("next_period_start_high")]
        public DateOnly NextPeriodHigh { get; set; }
        
        [Column("confidence_score")]
        public decimal ConfidenceScore { get; set; }
        
        [Column("ovulation_date")]
        public DateOnly? OvulationDate { get; set; }
        
        [Column("fertile_window_start")]
        public DateOnly? FertileWindowStart { get; set; }
        
        [Column("fertile_window_end")]
        public DateOnly? FertileWindowEnd { get; set; }
        
        [Column("current_phase")]
        public string? CurrentPhase { get; set; }
        
        [Column("current_day_in_cycle")]
        public int CurrentDayInCycle { get; set; }
        
        [Column("avg_cycle_length")]
        public decimal AvgCycleLength { get; set; }
        
        [Column("std_deviation")]
        public decimal StdDeviation { get; set; }
        
        [Column("cycles_analyzed")]
        public int CyclesAnalyzed { get; set; }
        
        [Column("insights")]
        public string[]? Insights { get; set; }
        
        [Column("is_current")]
        public bool IsCurrent { get; set; }
    }
}