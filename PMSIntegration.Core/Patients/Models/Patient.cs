namespace PMSIntegration.Core.Patients.Models;

public class Patient
{
    public int Id { get;  set; }
    public string FirstName { get;  set; }
    public string LastName { get;  set; }
    public string Phone { get;  set; }
    public string Email { get;  set; }
    public string Address { get;  set; }
    public string City { get;  set; }
    public string State { get;  set; }
    public string ZipCode { get;  set; }
    public DateTime DateOfBirth { get;  set; }
    public bool ReportReady { get;  set; }
    
    //private Patient() { }

    public static Patient Create(
        string firstName, string lastName,
        string phone, string email,
        string address, string city, string state, string zipCode,
        DateTime dateOfBirth)
    {
        return new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
            Address = address,
            City = city,
            State = state,
            ZipCode = zipCode,
            DateOfBirth = dateOfBirth,
            ReportReady = false
        };
    }
    
    public void MarkReportAsReady() => ReportReady = true;
}