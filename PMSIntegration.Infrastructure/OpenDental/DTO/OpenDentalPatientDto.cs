using System.Text.Json.Serialization;

namespace PMSIntegration.Infrastructure.OpenDental.DTO;

public class OpenDentalPatientDto
{
    /// <summary>
    /// Patient ID in OpenDental
    /// </summary>
    [JsonPropertyName("PatNum")]
    public int PatNum { get; set; }
    
    /// <summary>
    /// Last name
    /// </summary>
    [JsonPropertyName("LName")]
    public string? LName { get; set; }
    
    /// <summary>
    /// First name
    /// </summary>
    [JsonPropertyName("FName")]
    public string? FName { get; set; }
    
    /// <summary>
    /// Birth date (format: yyyy-MM-dd or MM/dd/yyyy)
    /// </summary>
    [JsonPropertyName("Birthdate")]
    public string? Birthdate { get; set; }
    
    /// <summary>
    /// Home phone
    /// </summary>
    [JsonPropertyName("HmPhone")]
    public string? HmPhone { get; set; }
    
    /// <summary>
    /// Mobile phone
    /// </summary>
    [JsonPropertyName("WirelessPhone")]
    public string? WirelessPhone { get; set; }
    
    /// <summary>
    /// Work phone
    /// </summary>
    [JsonPropertyName("WkPhone")]
    public string? WkPhone { get; set; }
    
    /// <summary>
    /// Email address
    /// </summary>
    [JsonPropertyName("Email")]
    public string? Email { get; set; }
    
    /// <summary>
    /// Street address line 1
    /// </summary>
    [JsonPropertyName("Address")]
    public string? Address { get; set; }
    
    /// <summary>
    /// Street address line 2
    /// </summary>
    [JsonPropertyName("Address2")]
    public string? Address2 { get; set; }
    
    /// <summary>
    /// City
    /// </summary>
    [JsonPropertyName("City")]
    public string? City { get; set; }
    
    /// <summary>
    /// State abbreviation
    /// </summary>
    [JsonPropertyName("State")]
    public string? State { get; set; }
    
    /// <summary>
    /// ZIP code
    /// </summary>
    [JsonPropertyName("Zip")]
    public string? Zip { get; set; }
    
    /// <summary>
    /// Last modified timestamp (used for incremental sync)
    /// </summary>
    [JsonPropertyName("DateTStamp")]
    public string? DateTStamp { get; set; }
}