using PMSIntegration.Core.Patients.ValueObjects;

namespace PMSIntegration.Core.Patients.Models;

public class Patient
{
    public PatientId Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public bool ReportReady { get; private set; }
    private bool _isIdSet = false;
    
    private Patient() { }

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
            ReportReady = false,
            _isIdSet = false
        };
    }

    public void SetId(PatientId id)
    {
        if( _isIdSet)
            throw new InvalidOperationException("ID has already been set");
        Id = id;
        _isIdSet = true;
    }
    
    public void MarkReportAsReady() => ReportReady = true;
}