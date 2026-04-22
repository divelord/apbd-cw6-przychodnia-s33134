using Microsoft.AspNetCore.Http;
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

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var appointments = await _appointmentsService.GetAllAppointmentsAsync();
        
        return Ok(appointments);
    }
}