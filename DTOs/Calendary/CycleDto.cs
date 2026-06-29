using System;
 
namespace Back_Calendary.DTOs.Calendary
{
    public record CycleDto(
        Guid Id,
        DateOnly StartDate,
        DateOnly? EndDate,
        int? CycleLength,
        int? PeriodLength,
        bool IsActive
    );
 
    // Request para iniciar ciclo (esto faltaba y causaba el error en CycleController)
    public record StartCycleRequest(DateOnly StartDate);
    //Cycle CloseCycleRequest
    public record CloseCycleRequest(DateOnly EndDate);
}