using System;
using System.Threading.Tasks;
using Back_Calendary.Services.Calendary;

namespace Back_Calendary.Interfaces.Calendary
{
    public interface IPredictionRepository
    {
        /// <param name="today">Fecha local calculada en el servicio (única fuente de verdad).</param>
        /// <param name="force">true = reemplazar aunque sea el mismo día (datos cambiaron).</param>
        Task SaveAsync(Guid userId, PredictionResult result, DateOnly today, bool force = false);
    }
}