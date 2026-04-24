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
    public async Task<IActionResult> Get(
        [FromQuery] string? status,
        [FromQuery] string? patientLastName
    )
    {
        // TODO

        var appointments = await _appointmentsService.GetAllAppointmentsAsync();

        return Ok(appointments);
    }

    // GET /api/appointments/{idAppointment}
    [Route("{idAppointment:int}")]
    [HttpGet]
    public async Task<IActionResult> GetById(int idAppointment)
    {
        // TODO

        return Ok();
    }

    // POST /api/appointments
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateAppointmentRequestDto appointmentRequest)
    {
        // TODO

        return CreatedAtAction("", null);
    }

    // PUT /api/appointments/{idAppointment}
    [Route("{idAppointment:int}")]
    [HttpPut]
    public async Task<IActionResult> Put(int idAppointment, [FromBody] UpdateAppointmentRequestDto appointmentRequest)
    {
        // TODO

        return Ok();
    }

    // DELETE /api/appointments/{idAppointment}
    [Route("{idAppointment:int}")]
    [HttpDelete]
    public async Task<IActionResult> Delete(int idAppointment)
    {
        // TODO

        return NoContent();
    }
}