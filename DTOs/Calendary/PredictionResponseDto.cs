using System;
using System.Collections.Generic;
 
namespace Back_Calendary.DTOs.Calendary
{
    public record PredictionResponseDto(
        DateOnly NextPeriodStart,
        DateOnly NextPeriodLow,
        DateOnly NextPeriodHigh,
        double ConfidenceScore,
        DateOnly? OvulationDate,
        DateOnly? FertileWindowStart,
        DateOnly? FertileWindowEnd,
        string CurrentPhase,
        int CurrentDayInCycle,
        int DaysUntilNextPeriod,
        double AvgCycleLength,
        double StdDeviation,
        int CyclesAnalyzed,
        List<string> Insights
    );
}