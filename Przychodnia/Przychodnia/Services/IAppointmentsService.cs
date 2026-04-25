using Przychodnia.DTOs;

namespace Przychodnia.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName, CancellationToken ct);
    Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int id, CancellationToken ct);
    Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto appointment, CancellationToken ct);
    Task<bool> UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto appointment, CancellationToken ct);
    Task<bool> DeleteAppointmentAsync(int id, CancellationToken ct);
}