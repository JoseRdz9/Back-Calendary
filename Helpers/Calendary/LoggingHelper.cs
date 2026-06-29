// Back_Calendary/Helpers/LoggingHelper.cs
using System;
using System.Text;
using System.Collections.Generic;
using Back_Calendary.Models.Calendary;
using Back_Calendary.Services.Calendary;

namespace Back_Calendary.Helpers
{
    public static class PredictionLoggingHelper
    {
        public static string LogCycleData(IReadOnlyList<Cycle> cycles)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== CICLOS ANALIZADOS ({cycles.Count}) ===");
            
            var completed = cycles.Where(c => c.CycleLength.HasValue && c.CycleLength > 0).ToList();
            sb.AppendLine($"Ciclos completados con longitud: {completed.Count}");
            
            foreach (var cycle in cycles.Take(10)) // Limitar a 10 para no saturar
            {
                sb.AppendLine($"- Ciclo {cycle.Id}: Inicio={cycle.StartDate}, Fin={cycle.EndDate}, Longitud={cycle.CycleLength}, Activo={cycle.IsActive}");
            }
            
            if (cycles.Count > 10)
                sb.AppendLine($"... y {cycles.Count - 10} ciclos más");
                
            return sb.ToString();
        }
        
        public static string LogDailyLogs(IReadOnlyList<DailyLog> logs, int maxLogs = 20)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== REGISTROS DIARIOS ({logs.Count}) ===");
            
            foreach (var log in logs.OrderByDescending(l => l.LogDate).Take(maxLogs))
            {
                sb.AppendLine($"- Fecha={log.LogDate}, Flow={log.FlowIntensity}, Temp={log.BasalTemp}, Mood={log.MoodScore}, Pain={log.PainLevel}");
            }
            
            if (logs.Count > maxLogs)
                sb.AppendLine($"... y {logs.Count - maxLogs} registros más");
                
            return sb.ToString();
        }
        
        public static string LogPredictionResult(PredictionResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== RESULTADO DE PREDICCIÓN ===");
            sb.AppendLine($"Próximo período: {result.NextPeriodStart} (ventana: {result.NextPeriodLow} - {result.NextPeriodHigh})");
            sb.AppendLine($"Días hasta próximo período: {result.DaysUntilNextPeriod}");
            sb.AppendLine($"Ovulación: {result.OvulationDate}");
            sb.AppendLine($"Ventana fértil: {result.FertileWindowStart} - {result.FertileWindowEnd}");
            sb.AppendLine($"Fase actual: {result.CurrentPhase}");
            sb.AppendLine($"Día del ciclo: {result.CurrentDayInCycle}");
            sb.AppendLine($"Longitud promedio: {result.AvgCycleLength} (±{result.StdDeviation})");
            sb.AppendLine($"Confianza: {result.ConfidenceScore}");
            sb.AppendLine($"Ciclos analizados: {result.CyclesAnalyzed}");
            sb.AppendLine($"Insights: {string.Join("; ", result.Insights)}");
            return sb.ToString();
        }
        
        public static string LogCalculationDetails(List<Cycle> completed, double avg, double std, int dayInCycle, int daysUntil, DateOnly today, DateOnly? ovulation)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DETALLES DE CÁLCULO ===");
            sb.AppendLine($"Fecha actual: {today}");
            sb.AppendLine($"Ciclos completados: {completed.Count}");
            sb.AppendLine($"Longitud promedio ponderada: {avg:F2}");
            sb.AppendLine($"Desviación estándar: {std:F2}");
            sb.AppendLine($"Día actual en ciclo: {dayInCycle}");
            sb.AppendLine($"Días hasta próximo período: {daysUntil}");
            sb.AppendLine($"Ovulación calculada (sin ajuste): {ovulation}");
            
            if (completed.Any())
            {
                var lengths = string.Join(", ", completed.Select(c => c.CycleLength));
                sb.AppendLine($"Longitudes de ciclos: [{lengths}]");
            }
            
            return sb.ToString();
        }
    }
}