using Przychodnia.DTOs;

namespace Przychodnia.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync();
}