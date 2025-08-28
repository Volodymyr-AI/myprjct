namespace PMSIntegration.Core.Patients.ValueObjects;

public readonly record struct PatientId(int Value)
{
    public override string ToString() => Value.ToString();
}