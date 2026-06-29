using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Back_Calendary.DTOs.Calendary;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Models.Calendary;

namespace Back_Calendary.Services.Calendary
{
    public class CycleService : ICycleService
    {
        private readonly ICycleRepository       _cycleRepo;
        private readonly ILogRepository         _logRepo;
        private readonly IPredictionEngine      _engine;
        private readonly IPredictionRepository  _predictionRepo;

        public CycleService(
            ICycleRepository      cycleRepo,
            ILogRepository        logRepo,
            IPredictionEngine     engine,
            IPredictionRepository predictionRepo)
        {
            _cycleRepo      = cycleRepo;
            _logRepo        = logRepo;
            _engine         = engine;
            _predictionRepo = predictionRepo;
        }

        public async Task StartNewCycleAsync(Guid userId, DateOnly startDate)
        {
            Log($"StartNewCycle | usuario={userId} inicio={startDate}");

            var allCycles = await _cycleRepo.GetByUserAsync(userId);

            // Evitar duplicados
            if (allCycles.Any(c => c.StartDate == startDate))
            {
                Log($"Ya existe ciclo con inicio={startDate}. Skipping.");
                return;
            }

            // Cerrar todos los ciclos activos anteriores a startDate
            foreach (var active in allCycles.Where(c => c.IsActive && c.StartDate < startDate))
            {
                int cycleLength = startDate.DayNumber - active.StartDate.DayNumber;
                Log($"Cerrando ciclo {active.Id} | inicio={active.StartDate} longitud={cycleLength}d");
                await _cycleRepo.CloseAsync(
                    active.Id,
                    startDate.AddDays(-1),
                    cycleLength,
                    active.PeriodLength ?? 5);
            }

            await _cycleRepo.CreateAsync(new Cycle
            {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                StartDate = startDate,
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            });
            Log($"Nuevo ciclo creado | inicio={startDate}");

            // ✅ Pasar ciclos y logs ya cargados para evitar doble consulta a BD
            var updatedCycles = await _cycleRepo.GetByUserAsync(userId);
            var logs          = await _logRepo.GetRecentAsync(userId);
            await RefreshPredictionAsync(userId, updatedCycles, logs);
        }

        public async Task CloseCurrentCycleAsync(Guid userId, DateOnly endDate)
        {
            Log($"CloseCurrentCycle | usuario={userId} fin={endDate}");

            var active = await _cycleRepo.GetActiveAsync(userId);
            if (active == null)
            {
                Log("WARNING: No hay ciclo activo");
                return;
            }

            active.EndDate      = endDate;
            active.PeriodLength = endDate.DayNumber - active.StartDate.DayNumber + 1;
            await _cycleRepo.UpdateAsync(active);
            Log($"Sangrado registrado | {active.StartDate} → {endDate} ({active.PeriodLength}d)");

            var updatedCycles = await _cycleRepo.GetByUserAsync(userId);
            var logs          = await _logRepo.GetRecentAsync(userId);
            await RefreshPredictionAsync(userId, updatedCycles, logs);
        }

        public async Task<CycleResponseDto?> GetActiveCycleAsync(Guid userId)
        {
            var c = await _cycleRepo.GetActiveAsync(userId);
            if (c == null) return null;
            return MapToResponseDto(c);
        }

        public async Task<IReadOnlyList<CycleDto>> GetAllAsync(Guid userId)
        {
            var cycles = await _cycleRepo.GetByUserAsync(userId);
            return cycles
                .Select(c => new CycleDto(c.Id, c.StartDate, c.EndDate,
                                          c.CycleLength, c.PeriodLength, c.IsActive))
                .ToList();
        }

        /// <summary>
        /// Retorna null si no hay ciclos — el frontend debe mostrar onboarding.
        /// Retorna predicción estimada (sin guardar) si hay ciclos pero ninguno completo.
        /// Retorna predicción real (guardada) si hay ≥2 ciclos completos.
        /// </summary>
        public async Task<PredictionResponseDto?> GetPredictionAsync(Guid userId)
        {
            Log($"GetPredictionAsync usuario={userId}");

            var cycles = await _cycleRepo.GetByUserAsync(userId);
            if (!cycles.Any())
            {
                Log("Sin ciclos registrados. Devolviendo null.");
                return null;
            }

            var logs  = await _logRepo.GetRecentAsync(userId, 90);
            var today = DateOnly.FromDateTime(DateTime.Now);

            Log($"ciclos={cycles.Count} logs={logs.Count} hoy={today}");

            var result = _engine.Predict(cycles, logs, today);

            if (result.CyclesAnalyzed == 0)
            {
                Log("Predicción orientativa (sin ciclos completos). No se persiste.");
                return ToDto(result);
            }

            await _predictionRepo.SaveAsync(userId, result, today, force: false);
            return ToDto(result);
        }

        // ── Privados ─────────────────────────────────────────────────────────

        /// <summary>
        /// Recibe datos ya cargados para evitar consultas extra a BD.
        /// Solo persiste si hay ciclos completos y la predicción cambió.
        /// </summary>
        private async Task RefreshPredictionAsync(
            Guid userId,
            IReadOnlyList<Cycle>    cycles,
            IReadOnlyList<DailyLog> logs)
        {
            bool hasCompleted = cycles.Any(c => c.CycleLength.HasValue && c.CycleLength > 20);
            if (!hasCompleted)
            {
                Log("RefreshPrediction: sin ciclos completos. Skipping.");
                return;
            }

            var today  = DateOnly.FromDateTime(DateTime.Now);
            var result = _engine.Predict(cycles, logs, today);

            await _predictionRepo.SaveAsync(userId, result, today, force: true);
        }

        // ── Mappers ───────────────────────────────────────────────────────────
        private static CycleResponseDto MapToResponseDto(Cycle c) => new()
        {
            Id            = c.Id,
            UserId        = c.UserId,
            StartDate     = c.StartDate,
            EndDate       = c.EndDate,
            CycleLength   = c.CycleLength,
            PeriodLength  = c.PeriodLength,
            OvulationDate = c.OvulationDate,
            IsActive      = c.IsActive,
            Notes         = c.Notes,
            CreatedAt     = c.CreatedAt
        };

        private static PredictionResponseDto ToDto(PredictionResult r) =>
            new(r.NextPeriodStart, r.NextPeriodLow, r.NextPeriodHigh,
                r.ConfidenceScore, r.OvulationDate,
                r.FertileWindowStart, r.FertileWindowEnd,
                r.CurrentPhase.ToString(), r.CurrentDayInCycle,
                r.DaysUntilNextPeriod, r.AvgCycleLength,
                r.StdDeviation, r.CyclesAnalyzed, r.Insights);

        private static void Log(string msg)
            => Console.WriteLine($"[SERVICE] {msg}");
    }
}