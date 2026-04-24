using System.ComponentModel.DataAnnotations;

namespace Przychodnia.DTOs;

public class UpdateAppointmentRequestDto
{
    [Required]
    public int IdPatient { get; set; }
    [Required]
    public int IdDoctor { get; set; }
    [Required]
    public DateTime AppointmentDate { get; set; }
    [Required]
    [RegularExpression("Scheduled|Completed|Cancelled", ErrorMessage = "Invalid status")]
    public string Status { get; set; } = string.Empty;
    [Required]
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}