using PMSIntegration.Core.Patients.Models;

namespace PMSIntegration.Infrastructure.OpenDental.DTO;

public static class OpenDentalPatientMapper
{
    /// <summary>
    /// Convert OpenDental DTO to Patient domain model
    /// </summary>
    public static Patient ToDomain(this OpenDentalPatientDto dto)
    {
        // Since we removed PatientId value object, use simple approach
        return new Patient
        {
            Id = dto.PatNum,  // Direct assignment now
            FirstName = dto.FName ?? "",
            LastName = dto.LName ?? "",
            Phone = GetBestPhone(dto.HmPhone, dto.WirelessPhone, dto.WkPhone),
            Email = dto.Email ?? "",
            Address = CombineAddress(dto.Address, dto.Address2),
            City = dto.City ?? "",
            State = dto.State ?? "",
            ZipCode = dto.Zip ?? "",
            DateOfBirth = ParseDate(dto.Birthdate),
            ReportReady = false
        };
    }

    private static string GetBestPhone(string? homePhone, string? wirelessPhone, string? workPhone)
    {
        if (!string.IsNullOrEmpty(homePhone)) return homePhone;
        if (!string.IsNullOrEmpty(wirelessPhone)) return wirelessPhone;
        if (!string.IsNullOrEmpty(workPhone)) return workPhone;
        return "";
    }

    private static string CombineAddress(string? address1, string? address2)
    {
        var parts = new[] { address1, address2 }
            .Where(a => !string.IsNullOrEmpty(a))
            .ToArray();
        
        return parts.Any() ? string.Join(", ", parts) : "";
    }

    private static DateTime ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) 
            return DateTime.MinValue;
        
        if (DateTime.TryParse(dateStr, out var date)) 
            return date;
        
        // Try specific formats
        var formats = new[]
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "M/d/yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, null, 
                System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
        }
        
        return DateTime.MinValue;
    }
}