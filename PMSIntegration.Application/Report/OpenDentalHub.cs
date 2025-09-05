namespace PMSIntegration.Application.Report;

public class OpenDentalHub
{
    public string GetPatientFirstLetter(string patientName)
    {
        var lastName = patientName.Split(' ').LastOrDefault();
        return string.IsNullOrEmpty(lastName) ? "Unknown" : lastName[0].ToString().ToUpper();
    }

    public string GetPatientFolderName(string patientName)
    {
        return patientName
            .Replace(" ", "");
    }
}