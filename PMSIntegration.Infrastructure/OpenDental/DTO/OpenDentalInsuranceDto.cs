using System.Text.Json.Serialization;

namespace PMSIntegration.Infrastructure.OpenDental.DTO;

public class OpenDentalInsuranceDto
{
    [JsonPropertyName("PatNum")]
    public int PatNum { get; set; }
    
    [JsonPropertyName("InsSubNum")]
    public int InsSubNum { get; set; }
    
    [JsonPropertyName("Subscriber")]
    public int Subscriber { get; set; }
    
    [JsonPropertyName("subscriber")]
    public string? SubscriberName { get; set; }
    
    [JsonPropertyName("SubscriberID")]
    public string? SubscriberID { get; set; }
    
    [JsonPropertyName("SubscNote")]
    public string? SubscNote { get; set; }
    
    [JsonPropertyName("PatPlanNum")]
    public int PatPlanNum { get; set; }
    
    [JsonPropertyName("Ordinal")]
    public int Ordinal { get; set; }
    
    [JsonPropertyName("ordinal")]
    public string? OrdinalDescription { get; set; }
    
    [JsonPropertyName("IsPending")]
    public string? IsPending { get; set; }
    
    [JsonPropertyName("Relationship")]
    public string? Relationship { get; set; }
    
    [JsonPropertyName("PatID")]
    public string? PatID { get; set; }
    
    [JsonPropertyName("CarrierNum")]
    public int CarrierNum { get; set; }
    
    [JsonPropertyName("CarrierName")]
    public string? CarrierName { get; set; }
    
    [JsonPropertyName("PlanNum")]
    public int PlanNum { get; set; }
    
    [JsonPropertyName("GroupName")]
    public string? GroupName { get; set; }
    
    [JsonPropertyName("GroupNum")]
    public string? GroupNum { get; set; }
    
    [JsonPropertyName("PlanNote")]
    public string? PlanNote { get; set; }
    
    [JsonPropertyName("FeeSched")]
    public int FeeSched { get; set; }
    
    [JsonPropertyName("feeSchedule")]
    public string? FeeSchedule { get; set; }
    
    [JsonPropertyName("PlanType")]
    public string? PlanType { get; set; }
    
    [JsonPropertyName("planType")]
    public string? PlanTypeDescription { get; set; }
    
    [JsonPropertyName("CopayFeeSched")]
    public int CopayFeeSched { get; set; }
    
    [JsonPropertyName("EmployerNum")]
    public int EmployerNum { get; set; }
    
    [JsonPropertyName("employer")]
    public string? EmployerName { get; set; }
    
    [JsonPropertyName("IsMedical")]
    public string? IsMedical { get; set; }
}