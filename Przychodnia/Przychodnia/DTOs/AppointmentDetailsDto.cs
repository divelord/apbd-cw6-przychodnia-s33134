namespace Przychodnia.DTOs;

public class AppointmentDetailsDto
{
    // DoctorsDetails
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhoneNumber { get; set; } = string.Empty;
    public string DoctorLicenseNumber { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}