using System;
using System.Linq;
using System.Threading.Tasks;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Models.Calendary;
using Back_Calendary.Services.Calendary;
using Supabase;

namespace Back_Calendary.Repositories.Calendary
{
    public class PredictionRepository : IPredictionRepository
    {
        private readonly Client _supabase;

        public PredictionRepository(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task SaveAsync(Guid userId, PredictionResult result, DateOnly today, bool force = false)
        {
            Log($"SaveAsync | hoy={today} force={force}");

            var existing = await _supabase
                .From<Prediction>()
                .Filter("user_id",    Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("is_current", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Get();

            var current = existing.Models.FirstOrDefault();

            if (current != null)
            {
                // ✅ Usar ToLocalTime() solo si el Kind es Utc; si es Unspecified asumirlo local
                var generatedDate = DateOnly.FromDateTime(
                    current.GeneratedAt.Kind == DateTimeKind.Utc
                        ? current.GeneratedAt.ToLocalTime()
                        : current.GeneratedAt);

                Log($"Existente | generada={generatedDate} hoy={today} force={force}");

                // ✅ Comparación segura sin double ==
                if (IsSamePrediction(current, result))
                {
                    Log("✅ Predicción idéntica a la existente. Skipping.");
                    return;
                }

                if (!force && generatedDate == today)
                {
                    Log("✅ Predicción de hoy ya existe. Skipping.");
                    return;
                }

                Log("Desactivando anterior.");
                await _supabase
                    .From<Prediction>()
                    .Filter("user_id",    Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Filter("is_current", Supabase.Postgrest.Constants.Operator.Equals, "true")
                    .Set(p => p.IsCurrent, false)
                    .Update();
            }
            else
            {
                Log("Sin predicción vigente, insertando primera.");
            }

            Log($"Insertando predicción | fecha={today}.");
            await _supabase.From<Prediction>().Insert(new Prediction
            {
                Id                 = Guid.NewGuid(),
                UserId             = userId,
                NextPeriodStart    = result.NextPeriodStart,
                NextPeriodLow      = result.NextPeriodLow,
                NextPeriodHigh     = result.NextPeriodHigh,
                ConfidenceScore    = (decimal)result.ConfidenceScore,
                OvulationDate      = result.OvulationDate,
                FertileWindowStart = result.FertileWindowStart,
                FertileWindowEnd   = result.FertileWindowEnd,
                AvgCycleLength     = (decimal)result.AvgCycleLength,
                StdDeviation       = (decimal)result.StdDeviation,
                CyclesAnalyzed     = result.CyclesAnalyzed,
                CurrentPhase       = result.CurrentPhase.ToString(),
                CurrentDayInCycle  = result.CurrentDayInCycle,
                IsCurrent          = true,
                GeneratedAt        = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            });
        }

        /// <summary>
        /// Comparación segura: usa tolerancia para decimales en vez de ==.
        /// Evita falsos "distintos" por precisión floating point.
        /// </summary>
        private static bool IsSamePrediction(Prediction current, PredictionResult next)
        {
            const double tolerance = 0.05;

            return current.NextPeriodStart   == next.NextPeriodStart
                && current.OvulationDate     == next.OvulationDate
                && current.CyclesAnalyzed    == next.CyclesAnalyzed
                && current.CurrentDayInCycle == next.CurrentDayInCycle
                && Math.Abs((double)current.AvgCycleLength - Math.Round(next.AvgCycleLength, 1)) < tolerance;
        }

        private static void Log(string msg)
            => Console.WriteLine($"[PREDICTION_REPO] {msg}");
    }
}