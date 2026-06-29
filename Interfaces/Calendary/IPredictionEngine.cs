using System;
using System.Collections.Generic;
using Back_Calendary.Models.Calendary;
using Back_Calendary.Services.Calendary;
 
namespace Back_Calendary.Interfaces.Calendary
{
    public interface IPredictionEngine
    {
        PredictionResult Predict(
            IReadOnlyList<Cycle> historicalCycles,
            IReadOnlyList<DailyLog> recentLogs,
            DateOnly today);
    }
}