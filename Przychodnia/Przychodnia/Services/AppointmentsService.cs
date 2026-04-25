using Microsoft.Data.SqlClient;
using Przychodnia.DTOs;

namespace Przychodnia.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly string _connectionString;

    public AppointmentsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var query = """
                    SELECT
                        a.IdAppointment,
                        a.AppointmentDate,
                        a.Status,
                        a.Reason,
                        p.FirstName + N' ' + p.LastName AS PatientFullName,
                        p.Email AS PatientEmail
                    FROM dbo.Appointments a
                    JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                    WHERE (@Status IS NULL OR a.Status = @Status)
                      AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                    ORDER BY a.AppointmentDate;
                    """;
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var appointments = new List<AppointmentListDto>();

        while (await reader.ReadAsync(ct))
        {
            var appointment = new AppointmentListDto()
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            };

            appointments.Add(appointment);
        }

        return appointments;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var query = """"
                    SELECT 
                        p.Email AS PatientEmail, p.PhoneNumber AS PatientPhoneNumber, 
                        d.LicenseNumber AS DoctorLicenseNumber, a.InternalNotes, a.CreatedAt
                    FROM Patients p
                    JOIN Appointments a ON p.IdPatient = a.IdPatient
                    JOIN Doctors d ON d.IdDoctor = a.IdDoctor
                    WHERE a.IdAppointment = @Id;
                    """";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return new AppointmentDetailsDto()
            {
                PatientEmail = reader.GetString(0),
                PatientPhoneNumber = reader.GetString(1),
                DoctorLicenseNumber = reader.GetString(2),
                InternalNotes = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return null;
    }

    public async Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto appointmentDto, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            var patientQuery = """
                               SELECT 1
                               FROM dbo.Patients
                               WHERE IdPatient = @IdPatient
                                   AND IsActive = 1;
                               """;
            await using var patientCommand = new SqlCommand(patientQuery, connection, (SqlTransaction) transaction);
            patientCommand.Parameters.AddWithValue("@IdPatient", (object?)appointmentDto.IdPatient ?? DBNull.Value);

            var patientExists = await patientCommand.ExecuteScalarAsync(ct);

            if (patientExists == null)
            {
                throw new ArgumentException("Brak pacjenta o podanym ID lub pacjent jest nieaktywny");
            }

            var doctorQuery = """
                              SELECT 1
                              FROM dbo.Doctors
                              WHERE IdDoctor = @IdDoctor
                                  AND IsActive = 1;
                              """;
            await using var doctorCommand = new SqlCommand(doctorQuery, connection, (SqlTransaction) transaction);
            doctorCommand.Parameters.AddWithValue("@IdDoctor", (object?)appointmentDto.IdDoctor ?? DBNull.Value);

            var doctorExists = await doctorCommand.ExecuteScalarAsync(ct);

            if (doctorExists == null)
            {
                throw new ArgumentException("Brak doktora o podanym ID lub doktor jest nieaktywny");
            }

            var visitQuery = """
                             SELECT 1
                             FROM dbo.Appointments
                             WHERE IdDoctor = @IdDoctor
                                AND AppointmentDate = @AppointmentDate;
                             """;
            await using var visitCommand = new SqlCommand(visitQuery, connection, (SqlTransaction) transaction);
            visitCommand.Parameters.AddWithValue("@IdDoctor", (object?)appointmentDto.IdDoctor ?? DBNull.Value);
            visitCommand.Parameters.AddWithValue("@AppointmentDate", (object?)appointmentDto.AppointmentDate ?? DBNull.Value);

            var visitExists = await visitCommand.ExecuteScalarAsync(ct);

            if (visitExists != null)
            {
                throw new InvalidOperationException("Podany doktor ma wizytę tego dnia");
            }

            var insertQuery = """
                                INSERT INTO Appointments 
                                    (IdPatient, IdDoctor, AppointmentDate, Status, Reason) 
                                OUTPUT INSERTED.IdAppointment
                                VALUES (
                                    @IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason                                                                                           
                                );
                              """;
            await using var insertCommand = new SqlCommand(insertQuery, connection, (SqlTransaction) transaction);
            insertCommand.Parameters.AddWithValue("@IdPatient", (object?)appointmentDto.IdPatient ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@IdDoctor", (object?)appointmentDto.IdDoctor ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@AppointmentDate", (object?)appointmentDto.AppointmentDate ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@Reason", (object?)appointmentDto.Reason ?? DBNull.Value);

            var insertResult = (int) await insertCommand.ExecuteScalarAsync(ct);

            await transaction.CommitAsync(ct);

            return insertResult;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto appointmentDto, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            var checkQuery = """
                             SELECT Status
                             FROM dbo.Appointments 
                             WHERE IdAppointment = @IdAppointment;
                             """;
            await using var checkCommand = new SqlCommand(checkQuery, connection, (SqlTransaction)transaction);
            checkCommand.Parameters.AddWithValue("@IdAppointment", id);

            var s = await checkCommand.ExecuteScalarAsync(ct);
            var status = Convert.ToString(s);

            if (status == null)
            {
                return false;
            }

            if (status == "Completed")
            {
                throw new InvalidOperationException("Nie wolno zmienić terminu wizyty, która jest zakończona");
            }

            var patientQuery = """
                               SELECT 1
                               FROM dbo.Patients
                               WHERE IdPatient = @IdPatient
                                   AND IsActive = 1;
                               """;
            await using var patientCommand = new SqlCommand(patientQuery, connection, (SqlTransaction)transaction);
            patientCommand.Parameters.AddWithValue("@IdPatient", (object?)appointmentDto.IdPatient ?? DBNull.Value);

            var patientExists = await patientCommand.ExecuteScalarAsync(ct);

            if (patientExists == null)
            {
                throw new ArgumentException("Brak pacjenta o podanym ID lub pacjent jest nieaktywny");
            }

            var doctorQuery = """
                              SELECT 1
                              FROM dbo.Doctors
                              WHERE IdDoctor = @IdDoctor
                                  AND IsActive = 1;
                              """;
            await using var doctorCommand = new SqlCommand(doctorQuery, connection, (SqlTransaction)transaction);
            doctorCommand.Parameters.AddWithValue("@IdDoctor", (object?)appointmentDto.IdDoctor ?? DBNull.Value);

            var doctorExists = await doctorCommand.ExecuteScalarAsync(ct);

            if (doctorExists == null)
            {
                throw new ArgumentException("Brak doktora o podanym ID lub doktor jest nieaktywny");
            }

            var collisionQuery = """
                                 SELECT 1
                                 FROM dbo.Appointments
                                 WHERE IdDoctor = @IdDoctor
                                     AND AppointmentDate = @AppointmentDate
                                     AND IdAppointment <> @IdAppointment;
                                 """;
            await using var collisionCommand = new SqlCommand(collisionQuery, connection, (SqlTransaction)transaction);
            collisionCommand.Parameters.AddWithValue("@IdDoctor", (object?)appointmentDto.IdDoctor ?? DBNull.Value);
            collisionCommand.Parameters.AddWithValue("@AppointmentDate", (object?)appointmentDto.AppointmentDate ?? DBNull.Value);
            collisionCommand.Parameters.AddWithValue("@IdAppointment", id);

            if (await collisionCommand.ExecuteScalarAsync(ct) != null)
            {
                throw new InvalidOperationException("Lekarz ma zaplanowaną wizytę w tym terminie");
            }

            var updateQuery = """
                              UPDATE dbo.Appointments
                              SET IdPatient = @IdPatient,
                                  IdDoctor = @IdDoctor,
                                  AppointmentDate = @AppointmentDate,
                                  Status = @Status,
                                  Reason = @Reason,
                                  InternalNotes =  @InternalNotes
                              WHERE IdAppointment = @IdAppointment;
                              """;
            await using var updateCommand = new SqlCommand(updateQuery, connection, (SqlTransaction)transaction);
            updateCommand.Parameters.AddWithValue("@IdPatient", (object?)appointmentDto.IdPatient ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@IdDoctor", (object?)appointmentDto.IdDoctor ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@AppointmentDate", (object?)appointmentDto.AppointmentDate ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@Status", (object?)appointmentDto.Status ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@Reason", (object?)appointmentDto.Reason ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@InternalNotes", (object?)appointmentDto.InternalNotes ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@IdAppointment", id);

            await updateCommand.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> DeleteAppointmentAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            var checkQuery = """
                             SELECT Status
                             FROM dbo.Appointments 
                             WHERE IdAppointment = @IdAppointment;
                             """;
            await using var checkCommand = new SqlCommand(checkQuery, connection, (SqlTransaction)transaction);
            checkCommand.Parameters.AddWithValue("@IdAppointment", id);

            var s = await checkCommand.ExecuteScalarAsync(ct);
            var status = Convert.ToString(s);

            if (status == null)
            {
                return false;
            }

            if (status == "Completed")
            {
                throw new InvalidOperationException("Nie można usunąć zakończonej wizyty");
            }

            var deleteQuery = """
                              DELETE FROM dbo.Appointments 
                              WHERE IdAppointment = @IdAppointment;
                              """;
            await using var deleteCommand = new SqlCommand(deleteQuery, connection, (SqlTransaction)transaction);
            deleteCommand.Parameters.AddWithValue("@IdAppointment", id);

            await deleteCommand.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}