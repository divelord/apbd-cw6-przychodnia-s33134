namespace Przychodnia.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime ErrorDate { get; set; } =  DateTime.Now;
}