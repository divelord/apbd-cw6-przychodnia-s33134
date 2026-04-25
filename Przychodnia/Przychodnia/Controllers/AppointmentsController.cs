using Microsoft.AspNetCore.Mvc;
using Przychodnia.DTOs;
using Przychodnia.Services;

namespace Przychodnia.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;

    public AppointmentsController(IAppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }

    // GET /api/appointments
    // GET /api/appointments?status=Scheduled&patientLastName=Kowalska
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] string? patientLastName, CancellationToken ct)
    {
        var appointments = await _appointmentsService.GetAllAppointmentsAsync(status, patientLastName, ct);

        if (!appointments.Any())
        {
            var error = new ErrorResponseDto
            {
                Message = "Brak wizyty o podanych danych"
            };
            return NotFound(error);
        }

        return Ok(appointments);
    }

    // GET /api/appointments/{idAppointment}
    [Route("{idAppointment:int}")]
    [HttpGet]
    public async Task<IActionResult> GetById(int idAppointment, CancellationToken ct)
    {
        var appointment = await _appointmentsService.GetAppointmentByIdAsync(idAppointment, ct);

        if (appointment == null)
        {
            var error = new ErrorResponseDto
            {
                Message = "Brak wizyty o podanym ID"
            };
            return NotFound(error);
        }

        return Ok(appointment);
    }

    // POST /api/appointments
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateAppointmentRequestDto appointmentRequest, CancellationToken ct)
    {
        if (appointmentRequest.AppointmentDate < DateTime.Now)
        {
            var error = new ErrorResponseDto
            {
                Message = "Termin wizyty nie może być w przeszłości"
            };
            return BadRequest(error);
        }

        try
        {
            int appointmentId = await _appointmentsService.CreateAppointmentAsync(appointmentRequest, ct);

            return CreatedAtAction(nameof(GetById), new { idAppointment = appointmentId }, new { idAppointment = appointmentId });
        }
        catch (InvalidOperationException ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };

            return Conflict(error);
        }
        catch (ArgumentException ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };

            return BadRequest(error);
        }
        catch (Exception ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };

            return BadRequest(error);
        }
    }

    // PUT /api/appointments/{idAppointment}
    [Route("{idAppointment:int}")]
    [HttpPut]
    public async Task<IActionResult> Put(int idAppointment, [FromBody] UpdateAppointmentRequestDto appointmentRequest, CancellationToken ct)
    {
        if (appointmentRequest.AppointmentDate < DateTime.Now)
        {
            var error = new ErrorResponseDto
            {
                Message = "Termin wizyty nie może być w przeszłości"
            };
            return BadRequest(error);
        }

        try
        {
            bool isUpdated = await _appointmentsService.UpdateAppointmentAsync(idAppointment, appointmentRequest, ct);

            if (!isUpdated)
            {
                var error = new ErrorResponseDto
                {
                    Message = "Brak wizyty o podanym ID"
                };
                return NotFound(error);
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };
            return Conflict(error);
        }
        catch (ArgumentException ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };
            return BadRequest(error);
        }
        catch (Exception ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };

            return BadRequest(error);
        }
    }

    // DELETE /api/appointments/{idAppointment}
    [Route("{idAppointment:int}")]
    [HttpDelete]
    public async Task<IActionResult> Delete(int idAppointment, CancellationToken ct)
    {
        try
        {
            bool isDeleted = await _appointmentsService.DeleteAppointmentAsync(idAppointment, ct);

            if (!isDeleted)
            {
                var error = new ErrorResponseDto
                {
                    Message = "Nie znaleziono wizyty o podanym ID"
                };
                return NotFound(error);
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };

            return Conflict(error);
        }
        catch (Exception ex)
        {
            var error = new ErrorResponseDto
            {
                Message = ex.Message
            };

            return BadRequest(error);
        }
    }
}