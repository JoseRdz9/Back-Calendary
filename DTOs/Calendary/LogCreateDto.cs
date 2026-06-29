using System;
using System.Collections.Generic;
 
namespace Back_Calendary.DTOs.Calendary
{
    public record LogCreateDto(
        DateOnly LogDate,
        int FlowIntensity,
        decimal? BasalTemp,
        int CervicalMucus,
        int MoodScore,
        int PainLevel,
        int EnergyLevel,
        string? Notes,
        List<int> SymptomIds
    );
}