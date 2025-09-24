using PMSIntegration.Core.Patients.Models;

namespace PMSIntegration.Infrastructure.OpenDental.DTO;

public static class OpenDentalInsuranceMapper
{
    /// <summary>
    /// Convert OpenDental DTO to simplified Insurance for billing
    /// This is the method used in PatientExportService
    /// </summary>
    public static Insurance ToBillingInsurance(this OpenDentalInsuranceDto dto)
    {
        return new Insurance
        {
            PatientId = dto.PatNum,
            CarrierName = dto.CarrierName ?? "Unknown Carrier",
            PolicyNumber = dto.SubscriberID ?? dto.PatID ?? "",
            GroupNumber = dto.GroupNum ?? "",
            PolicyholderName = dto.SubscriberName ?? "Self",
            Relationship = MapRelationship(dto.Relationship),
            Priority = MapPriority(dto.Ordinal),
            IsActive = dto.IsPending?.ToLower() != "true"
        };
    }
    
    private static string MapRelationship(string? relationship)
    {
        if (string.IsNullOrEmpty(relationship))
            return "Self";
            
        var lower = relationship.ToLower();
        
        if (lower.Contains("self"))
            return "Self";
        if (lower.Contains("spouse"))
            return "Spouse";
        if (lower.Contains("child") || lower.Contains("dependent"))
            return "Child";
            
        return "Other";
    }
    
    private static string MapPriority(int ordinal)
    {
        return ordinal switch
        {
            1 => "Primary",
            2 => "Secondary",
            _ => "Primary" // Default to Primary for any other value
        };
    }
}