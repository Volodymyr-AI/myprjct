using System.Text.Json.Serialization;

namespace PMSIntegration.Infrastructure.OpenDental.DTO;

public class OpenDentalInsuranceDto
{
    /// <summary>
    /// Patient ID
    /// </summary>
    [JsonPropertyName("PatNum")]
    public int PatNum { get; set; }
    
    /// <summary>
    /// Insurance subscriber number (unique ID)
    /// </summary>
    [JsonPropertyName("InsSubNum")]
    public int InsSubNum { get; set; }
    
    /// <summary>
    /// Patient plan number (links patient to plan)
    /// </summary>
    [JsonPropertyName("PatPlanNum")]
    public int PatPlanNum { get; set; }
    
    /// <summary>
    /// Insurance carrier name
    /// </summary>
    [JsonPropertyName("CarrierName")]
    public string? CarrierName { get; set; }
    
    /// <summary>
    /// Subscriber/Policy ID
    /// </summary>
    [JsonPropertyName("SubscriberID")]
    public string? SubscriberID { get; set; }
    
    /// <summary>
    /// Alternative patient ID (sometimes used as policy number)
    /// </summary>
    [JsonPropertyName("PatID")]
    public string? PatID { get; set; }
    
    /// <summary>
    /// Subscriber name (policyholder)
    /// </summary>
    [JsonPropertyName("subscriber")]
    public string? SubscriberName { get; set; }
    
    /// <summary>
    /// Relationship to subscriber
    /// </summary>
    [JsonPropertyName("Relationship")]
    public string? Relationship { get; set; }
    
    /// <summary>
    /// Group number
    /// </summary>
    [JsonPropertyName("GroupNum")]
    public string? GroupNum { get; set; }
    
    /// <summary>
    /// Ordinal number (1=Primary, 2=Secondary)
    /// </summary>
    [JsonPropertyName("Ordinal")]
    public int Ordinal { get; set; }
    
    /// <summary>
    /// Is pending verification ("true"/"false" as string)
    /// </summary>
    [JsonPropertyName("IsPending")]
    public string? IsPending { get; set; }
}