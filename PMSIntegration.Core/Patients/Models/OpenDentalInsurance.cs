namespace PMSIntegration.Core.Patients.Models;

/// <summary>
/// OpenDental-specific insurance information
/// Stores additional OpenDental fields that might be needed for reference
/// </summary>
public class OpenDentalInsurance : Insurance
{
    /// <summary>
    /// OpenDental's unique identifiers (for reference/updates)
    /// </summary>
    public int InsSubNum { get; set; }
    public int PatPlanNum { get; set; }
    
    /// <summary>
    /// Convert to generic Insurance for billing
    /// </summary>
    public Insurance ToGenericInsurance()
    {
        return new Insurance
        {
            PatientId = this.PatientId,
            CarrierName = this.CarrierName,
            PolicyNumber = this.PolicyNumber,
            GroupNumber = this.GroupNumber,
            PolicyholderName = this.PolicyholderName,
            Relationship = this.Relationship,
            Priority = this.Priority,
            IsActive = this.IsActive
        };
    }
}