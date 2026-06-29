using System;

namespace Back_Calendary.DTOs.Calendary
{
    public class CycleResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? CycleLength { get; set; }
        public int? PeriodLength { get; set; }
        public DateOnly? OvulationDate { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}