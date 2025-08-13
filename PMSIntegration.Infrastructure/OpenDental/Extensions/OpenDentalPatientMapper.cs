using PMSIntegration.Core.Patients.Models;
using PMSIntegration.Core.Patients.ValueObjects;
using PMSIntegration.Infrastructure.OpenDental.DTO;

namespace PMSIntegration.Infrastructure.OpenDental.Extensions;

public static class OpenDentalPatientMapper
{
    public static Patient ToDomain(this OpenDentalPatientDto dto)
    {
        var patient = Patient.Create(
            firstName: dto.FName ?? "",
            lastName: dto.LName ?? "",
            phone: GetBestPhone(dto.HmPhone, dto.WirelessPhone, dto.WkPhone),
            email: dto.Email ?? "",
            address: CombineAddress(dto.Address, dto.Address2),
            city: dto.City ?? "",
            state: dto.State ?? "",
            zipCode: dto.Zip ?? "",
            dateOfBirth: ParseDate(dto.Birthdate)
        );
        
        patient.SetId(new PatientId(dto.PatNum));
        return patient;
    }

    private static string GetBestPhone(string? homePhone, string? wirelessPhone, string? workPhone)
    {
        if (!string.IsNullOrEmpty(homePhone)) return homePhone;
        if (!string.IsNullOrEmpty(wirelessPhone)) return wirelessPhone;
        if (!string.IsNullOrEmpty(workPhone)) return workPhone;
        return "No phone number";
    }

    private static string CombineAddress(string? address1, string? address2)
    {
        var parts = new[] { address1, address2 }
            .Where(a => !string.IsNullOrEmpty(a))
            .ToArray();
        
        return string.Join(',', parts);
    }

    private static DateTime ParseDate(string? dateStr)
    {
        if(string.IsNullOrEmpty(dateStr)) return DateTime.MinValue;
        
        if(DateTime.TryParse(dateStr, out var date)) return date;
        
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
            if (DateTime.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
        }
        
        return DateTime.MinValue;
    }
}