// public class DailyLog
// {
//     public Guid Id { get; set; }
//     public Guid UserId { get; set; }
//     public Guid? CycleId { get; set; }
//     public DateOnly LogDate { get; set; }
//     public int FlowIntensity { get; set; }
//     public decimal? BasalTemp { get; set; }
//     public int CervicalMucus { get; set; }
//     public int MoodScore { get; set; }
//     public int PainLevel { get; set; }
//     public int EnergyLevel { get; set; }
//     public string? Notes { get; set; }
// }
using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Back_Calendary.Models.Calendary
{
    [Table("daily_logs")]  
    public class DailyLog : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }
        
        [Column("user_id")]
        public Guid UserId { get; set; }
        
        [Column("cycle_id")]
        public Guid? CycleId { get; set; }
        
        [Column("log_date")]
        public DateOnly LogDate { get; set; }
        
        [Column("flow_intensity")]
        public int? FlowIntensity { get; set; }
        
        [Column("is_menstruation")]
        public bool IsMenstruation { get; set; }
        
        [Column("is_ovulation")]
        public bool IsOvulation { get; set; }
        
        [Column("symptoms")]
        public string[]? Symptoms { get; set; }
        
        [Column("mood")]
        public int? Mood { get; set; }
        
        [Column("basal_temp")]
        public decimal? BasalTemp { get; set; }
        
        [Column("mood_score")]
        public int? MoodScore { get; set; }
        
        [Column("pain_level")]
        public int? PainLevel { get; set; }
        
        [Column("cervical_mucus")]
        public int? CervicalMucus { get; set; }
        
        [Column("energy_level")]
        public int? EnergyLevel { get; set; }
        
        [Column("notes")]
        public string? Notes { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        

    }
}