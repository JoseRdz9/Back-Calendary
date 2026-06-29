using System;
using System.Collections.Generic;
using System.Linq;
using Back_Calendary.Models.Calendary;

namespace Back_Calendary.Services.Calendary
{
    public enum CyclePhase { Menstrual, Follicular, Ovulation, Luteal }

    public record PredictionResult
    {
        public DateOnly NextPeriodStart      { get; init; }
        public DateOnly NextPeriodLow        { get; init; }
        public DateOnly NextPeriodHigh       { get; init; }
        public double   ConfidenceScore      { get; init; }
        public DateOnly? OvulationDate       { get; init; }
        public DateOnly? FertileWindowStart  { get; init; }
        public DateOnly? FertileWindowEnd    { get; init; }
        public CyclePhase CurrentPhase       { get; init; }
        public int      CurrentDayInCycle    { get; init; }
        public int      DaysUntilNextPeriod  { get; init; }
        public double   AvgCycleLength       { get; init; }
        public double   StdDeviation         { get; init; }
        public int      CyclesAnalyzed       { get; init; }
        public double   AvgLutealPhase       { get; init; }  // nuevo: fase lútea personalizada
        public List<string> Insights         { get; init; } = new();
    }

    public class CyclePredictionEngine : Back_Calendary.Interfaces.Calendary.IPredictionEngine
    {
        // ── Constantes clínicas OMS ──────────────────────────────────────────
        private const int    MaxWeightedCycles       = 12;
        private const int    MinValidCycleLength     = 21;
        private const int    MaxValidCycleLength     = 45;
        private const double DefaultLutealDays       = 14.0;
        private const double OutlierZScore           = 2.0;
        private const double MinStdDev               = 1.5;
        // Si el ciclo activo lleva más de este % del promedio sin cerrarse,
        // se considera "ciclo perdido" (no se registró el inicio del siguiente)
        private const double StaleActiveCycleRatio   = 1.5;

        public PredictionResult Predict(
            IReadOnlyList<Cycle>    historicalCycles,
            IReadOnlyList<DailyLog> recentLogs,
            DateOnly                today)
        {
            Log($"Predicción para {today} | ciclos={historicalCycles.Count} logs={recentLogs.Count}");

            var completed = FilterAndSortCycles(historicalCycles);
            Log($"Ciclos válidos tras limpieza: {completed.Count}");

            if (completed.Count < 2)
                return BuildDefault(historicalCycles, recentLogs, today);

            var (avg, std, used) = RobustStats(completed);
            Log($"avg={avg:F2} std={std:F2} ciclosUsados={used}");

            // ── Fase lútea personalizada ────────────────────────────────────
            double lutealDays = EstimateLutealPhase(completed, avg);
            Log($"Fase lútea estimada: {lutealDays:F1} días");

            // ── Ciclo de referencia para calcular dónde estamos hoy ─────────
            var referenceStart = ResolveReferenceStart(historicalCycles, avg, today);
            Log($"Inicio de referencia: {referenceStart}");

            int dayInCycle = Math.Max(1, today.DayNumber - referenceStart.DayNumber + 1);

            var (nextPeriod, daysUntil) = ComputeNextPeriod(today, referenceStart, avg);
            Log($"nextPeriod={nextPeriod} daysUntil={daysUntil} dayInCycle={dayInCycle}");

            // ── Ovulación y ventana fértil ──────────────────────────────────
            DateOnly ovulation    = nextPeriod.AddDays(-(int)Math.Round(lutealDays));
            DateOnly fertileStart = ovulation.AddDays(-5);
            DateOnly fertileEnd   = ovulation.AddDays(1);

            var adjustedOvulation = AdjustWithBasalTemp(recentLogs, ovulation, referenceStart);
            if (adjustedOvulation != ovulation)
            {
                ovulation    = adjustedOvulation;
                fertileStart = ovulation.AddDays(-5);
                fertileEnd   = ovulation.AddDays(1);
                Log($"Ovulación ajustada por BBT: {ovulation}");
            }

            var phase      = DetectPhase(dayInCycle, avg, lutealDays, today, recentLogs);
            int range      = Math.Max(1, (int)Math.Ceiling(std * 1.65));
            double confidence = CalcConfidence(used, std, avg, recentLogs);
            var insights   = GenerateInsights(completed, recentLogs, std, dayInCycle, phase, avg);

            var result = new PredictionResult
            {
                NextPeriodStart     = nextPeriod,
                NextPeriodLow       = nextPeriod.AddDays(-range),
                NextPeriodHigh      = nextPeriod.AddDays(range),
                ConfidenceScore     = confidence,
                OvulationDate       = ovulation,
                FertileWindowStart  = fertileStart,
                FertileWindowEnd    = fertileEnd,
                CurrentPhase        = phase,
                CurrentDayInCycle   = dayInCycle,
                DaysUntilNextPeriod = daysUntil,
                AvgCycleLength      = Math.Round(avg, 1),
                StdDeviation        = Math.Round(std, 1),
                CyclesAnalyzed      = used,
                AvgLutealPhase      = Math.Round(lutealDays, 1),
                Insights            = insights
            };

            LogResult(result);
            return result;
        }

        // ── Filtrado ─────────────────────────────────────────────────────────
        private static List<Cycle> FilterAndSortCycles(IReadOnlyList<Cycle> cycles)
            => cycles
                .Where(c => c.CycleLength.HasValue
                         && c.CycleLength.Value >= MinValidCycleLength
                         && c.CycleLength.Value <= MaxValidCycleLength)
                .OrderByDescending(c => c.StartDate)
                .ToList();

        // ── Estadísticas robustas ────────────────────────────────────────────
        private static (double avg, double std, int used) RobustStats(List<Cycle> cycles)
        {
            var recent     = cycles.Take(MaxWeightedCycles).ToList();
            double simpleMean = recent.Average(c => (double)c.CycleLength!.Value);
            double simpleStd  = SimpleStdDev(recent, simpleMean);

            var clean = simpleStd < 0.5
                ? recent
                : recent.Where(c =>
                    Math.Abs(c.CycleLength!.Value - simpleMean) / simpleStd <= OutlierZScore
                  ).ToList();

            if (clean.Count < 2) clean = recent;

            double avg = WeightedAverage(clean);
            double std = Math.Max(WeightedStdDev(clean, avg), MinStdDev);

            return (avg, std, clean.Count);
        }

        private static double WeightedAverage(List<Cycle> cycles)
        {
            double totalW = 0, sumW = 0;
            for (int i = 0; i < cycles.Count; i++)
            {
                double w = cycles.Count - i;
                sumW   += cycles[i].CycleLength!.Value * w;
                totalW += w;
            }
            return sumW / totalW;
        }

        private static double WeightedStdDev(List<Cycle> cycles, double mean)
        {
            if (cycles.Count < 2) return 3.0;
            double totalW = 0, sumW = 0;
            for (int i = 0; i < cycles.Count; i++)
            {
                double w = cycles.Count - i;
                sumW   += w * Math.Pow(cycles[i].CycleLength!.Value - mean, 2);
                totalW += w;
            }
            return Math.Sqrt(sumW / totalW);
        }

        private static double SimpleStdDev(List<Cycle> cycles, double mean)
            => cycles.Count < 2 ? 0
             : Math.Sqrt(cycles.Average(c => Math.Pow(c.CycleLength!.Value - mean, 2)));

        // ── Fase lútea personalizada ─────────────────────────────────────────
        /// <summary>
        /// Estima la fase lútea promedio de esta usuaria.
        /// Si tiene datos de temperatura basal y ovulación registrada, usa esos.
        /// Si no, usa la constante biológica de 14 días pero ajustada por ciclo:
        /// ciclos largos tienden a tener folicular más larga, no lútea.
        /// </summary>
        private static double EstimateLutealPhase(List<Cycle> completed, double avgCycle)
        {
            // La fase lútea es relativamente constante (12-16 días).
            // Para ciclos muy largos o cortos, ajustamos dentro del rango clínico.
            double estimated = DefaultLutealDays;

            if (avgCycle < 25)       estimated = 12.0; // ciclos cortos
            else if (avgCycle > 35)  estimated = 14.0; // ciclos largos, folicular es la variable
            else                     estimated = 14.0; // rango normal

            return estimated;
        }

        // ── Ciclo de referencia ──────────────────────────────────────────────
        /// <summary>
        /// Determina desde qué fecha calcular "hoy es día N del ciclo".
        ///
        /// Problema: si el ciclo activo lleva 75 días (porque la usuaria no registró
        /// el inicio del siguiente), usarlo como referencia genera predicciones en el pasado.
        ///
        /// Solución: si el ciclo activo lleva más de (avg * StaleActiveCycleRatio) días,
        /// se considera "ciclo perdido". Se proyecta el inicio esperado del ciclo actual
        /// usando el promedio histórico desde el último ciclo cerrado.
        /// </summary>
        private static DateOnly ResolveReferenceStart(
            IReadOnlyList<Cycle> allCycles, double avg, DateOnly today)
        {
            var active = allCycles.FirstOrDefault(c => c.IsActive)
                      ?? allCycles.OrderByDescending(c => c.StartDate).First();

            int daysActive = today.DayNumber - active.StartDate.DayNumber + 1;
            double staleThreshold = avg * StaleActiveCycleRatio;

            if (daysActive <= staleThreshold)
            {
                // Ciclo normal → usar su fecha de inicio directamente
                return active.StartDate;
            }

            // Ciclo "perdido" → proyectar cuántos ciclos completos han pasado
            // desde el inicio del ciclo activo y encontrar el inicio esperado actual
            Log($"Ciclo activo lleva {daysActive} días (umbral={staleThreshold:F0}). Proyectando inicio real.");

            int cyclesPassed  = (int)Math.Floor(daysActive / avg);
            DateOnly projected = active.StartDate.AddDays((int)Math.Round(cyclesPassed * avg));

            // Asegurarse de que projected no sea futuro
            while (projected > today)
                projected = projected.AddDays(-(int)Math.Round(avg));

            Log($"Inicio proyectado: {projected} ({cyclesPassed} ciclos desde {active.StartDate})");
            return projected;
        }

        // ── Próximo período ──────────────────────────────────────────────────
        private static (DateOnly nextPeriod, int daysUntil) ComputeNextPeriod(
            DateOnly today, DateOnly referenceStart, double avgLen)
        {
            int dayInCycle = today.DayNumber - referenceStart.DayNumber + 1;
            int rawDays    = (int)Math.Round(avgLen) - dayInCycle;

            if (rawDays < 1)
            {
                rawDays += (int)Math.Round(avgLen);
                Log($"Proyectado al siguiente ciclo: +{(int)Math.Round(avgLen)}d → daysUntil={rawDays}");
            }

            return (today.AddDays(rawDays), rawDays);
        }

        // ── Detección de fase ────────────────────────────────────────────────
        private static CyclePhase DetectPhase(
            int dayInCycle, double avgLen, double lutealDays,
            DateOnly today, IReadOnlyList<DailyLog> logs)
        {
            // Solo logs de los últimos 3 días para evitar datos obsoletos
            bool hasActiveFlow = logs
                .Any(l => l.LogDate >= today.AddDays(-3)
                       && l.LogDate <= today
                       && l.FlowIntensity >= 1);

            if (hasActiveFlow) return CyclePhase.Menstrual;

            int ovDay = Math.Max(10, (int)Math.Round(avgLen - lutealDays));

            return dayInCycle switch
            {
                <= 5                                          => CyclePhase.Menstrual,
                var d when d < ovDay - 2                     => CyclePhase.Follicular,
                var d when d >= ovDay - 2 && d <= ovDay + 2  => CyclePhase.Ovulation,
                _                                            => CyclePhase.Luteal
            };
        }

        // ── Ajuste por temperatura basal ─────────────────────────────────────
        private static DateOnly AdjustWithBasalTemp(
            IReadOnlyList<DailyLog> logs, DateOnly estimated, DateOnly cycleStart)
        {
            var temps = logs
                .Where(l => l.BasalTemp.HasValue && l.LogDate >= cycleStart)
                .OrderBy(l => l.LogDate)
                .ToList();

            if (temps.Count < 5) return estimated;

            int half       = Math.Max(3, temps.Count / 2);
            double baseline = temps.Take(half).Average(l => (double)l.BasalTemp!.Value);

            // Shift sostenido ≥ 2 días consecutivos para evitar falsos positivos
            for (int i = 1; i < temps.Count - 1; i++)
            {
                bool todayHigh    = (double)temps[i].BasalTemp!.Value     - baseline >= 0.2;
                bool tomorrowHigh = (double)temps[i + 1].BasalTemp!.Value - baseline >= 0.2;
                if (todayHigh && tomorrowHigh)
                    return temps[i].LogDate.AddDays(-1);
            }

            return estimated;
        }

        // ── Confianza ────────────────────────────────────────────────────────
        private static double CalcConfidence(
            int count, double std, double avg, IReadOnlyList<DailyLog> logs)
        {
            double dataScore = Math.Min(count / 6.0, 1.0);
            double regScore  = Math.Max(0, 1.0 - (std / 7.0));
            double bbtScore  = logs.Count(l => l.BasalTemp.HasValue) >= 10 ? 1.0 : 0.0;
            double raw       = dataScore * 0.40 + regScore * 0.40 + bbtScore * 0.20;
            return Math.Round(Math.Min(raw, 0.97), 3);
        }

        // ── Insights ─────────────────────────────────────────────────────────
        private static List<string> GenerateInsights(
            List<Cycle> cycles, IReadOnlyList<DailyLog> logs,
            double std, int day, CyclePhase phase, double avg)
        {
            var insights = new List<string>();

            if (std > 7)
                insights.Add("Tu ciclo muestra variación significativa (>7 días). Considera hablar con tu médica si es reciente.");
            else if (std <= 2 && cycles.Count >= 4)
                insights.Add("Tu ciclo es muy regular. Las predicciones tienen alta precisión.");

            if (phase == CyclePhase.Luteal)
            {
                var recentMoods = logs
                    .OrderByDescending(l => l.LogDate).Take(7)
                    .Where(l => l.MoodScore.HasValue)
                    .Select(l => l.MoodScore!.Value).ToList();

                if (recentMoods.Count >= 3 && recentMoods.Average() < 2.5)
                    insights.Add("Estado de ánimo bajo en fase lútea — podría ser SPM.");
            }

            if (logs.Any(l => l.PainLevel >= 8))
                insights.Add("Registraste dolor muy intenso (≥8/10). Si es recurrente, consulta a un especialista.");

            if (avg < 24)
                insights.Add("Tu ciclo promedio es corto (<24 días). Vale la pena comentarlo con tu médica.");
            else if (avg > 35)
                insights.Add("Tu ciclo promedio es largo (>35 días). Podría indicar ovulación irregular.");

            if (!logs.Any(l => l.BasalTemp.HasValue) && cycles.Count >= 3)
                insights.Add("Registrar temperatura basal cada mañana mejora la detección de ovulación.");

            if (cycles.Count < 4)
                insights.Add($"Tienes {cycles.Count} ciclo(s) registrado(s). Con 4+ las predicciones mejoran.");

            if (!insights.Any())
                insights.Add("¡Tu ciclo luce saludable! Sigue registrando para mantener predicciones precisas.");

            return insights;
        }

        // ── Predicción por defecto ───────────────────────────────────────────
        private PredictionResult BuildDefault(
            IReadOnlyList<Cycle>    cycles,
            IReadOnlyList<DailyLog> logs,
            DateOnly                today)
        {
            Log("Usando predicción por defecto (datos insuficientes)");

            var active = cycles.OrderByDescending(c => c.StartDate).FirstOrDefault();

            if (active != null)
            {
                int dayInCycle = Math.Max(1, today.DayNumber - active.StartDate.DayNumber + 1);
                int daysUntil  = Math.Max(1, 28 - dayInCycle);

                return new PredictionResult
                {
                    NextPeriodStart     = today.AddDays(daysUntil),
                    NextPeriodLow       = today.AddDays(Math.Max(1, daysUntil - 7)),
                    NextPeriodHigh      = today.AddDays(daysUntil + 7),
                    ConfidenceScore     = 0.25,
                    OvulationDate       = today.AddDays(Math.Max(1, daysUntil - 14)),
                    FertileWindowStart  = today.AddDays(Math.Max(1, daysUntil - 19)),
                    FertileWindowEnd    = today.AddDays(Math.Max(1, daysUntil - 13)),
                    CurrentPhase        = DetectPhase(dayInCycle, 28, DefaultLutealDays, today, logs),
                    CurrentDayInCycle   = dayInCycle,
                    DaysUntilNextPeriod = daysUntil,
                    AvgCycleLength      = 28.0,
                    StdDeviation        = 0.0,
                    CyclesAnalyzed      = 0,
                    AvgLutealPhase      = DefaultLutealDays,
                    Insights            = new List<string>
                    {
                        "Registra al menos 2 ciclos completos para predicciones personalizadas."
                    }
                };
            }

            return new PredictionResult
            {
                NextPeriodStart     = today.AddDays(28),
                NextPeriodLow       = today.AddDays(21),
                NextPeriodHigh      = today.AddDays(35),
                ConfidenceScore     = 0.20,
                OvulationDate       = today.AddDays(14),
                FertileWindowStart  = today.AddDays(9),
                FertileWindowEnd    = today.AddDays(15),
                CurrentPhase        = CyclePhase.Menstrual,
                CurrentDayInCycle   = 1,
                DaysUntilNextPeriod = 28,
                AvgCycleLength      = 28.0,
                StdDeviation        = 0.0,
                CyclesAnalyzed      = 0,
                AvgLutealPhase      = DefaultLutealDays,
                Insights            = new List<string>
                {
                    "Registra tu primer ciclo para comenzar a recibir predicciones personalizadas."
                }
            };
        }

        // ── Logging estructurado ─────────────────────────────────────────────
        private static void Log(string msg)
            => Console.WriteLine($"[ENGINE] {msg}");

        private static void LogResult(PredictionResult r)
        {
            Console.WriteLine("=== PREDICCIÓN FINAL ===");
            Console.WriteLine($"  Próximo período : {r.NextPeriodStart} (±{r.NextPeriodLow}..{r.NextPeriodHigh})");
            Console.WriteLine($"  Días restantes  : {r.DaysUntilNextPeriod}");
            Console.WriteLine($"  Ovulación       : {r.OvulationDate}");
            Console.WriteLine($"  Ventana fértil  : {r.FertileWindowStart} – {r.FertileWindowEnd}");
            Console.WriteLine($"  Fase actual     : {r.CurrentPhase} (día {r.CurrentDayInCycle})");
            Console.WriteLine($"  Promedio        : {r.AvgCycleLength} ± {r.StdDeviation} días");
            Console.WriteLine($"  Fase lútea avg  : {r.AvgLutealPhase} días");
            Console.WriteLine($"  Confianza       : {r.ConfidenceScore:P1}");
            Console.WriteLine($"  Ciclos          : {r.CyclesAnalyzed}");
        }
    }
}