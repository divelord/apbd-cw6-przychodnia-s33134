namespace Przychodnia.DTOs;

public class AppointmentDetailsDto
{
    // DoctorsDetails
    public int IdDoctor { get; set; }
    public string DoctorFullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    // PatientsDetails
    public int IdPatient { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    // AppointmentsDetails
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}