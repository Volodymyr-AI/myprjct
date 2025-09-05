using PMSIntegration.Core.Patients.ValueObjects;

namespace PMSIntegration.Core.Patients.Models;

public class PatientInsurance
{
    public PatientId PatientId { get; private set; }
    public int InsSubNum { get; private set; }
    public int PatPlanNum { get; private set; }
    public string CarrierName { get; private set; }
    public string SubscriberId { get; private set; }
    public string SubscriberName { get; private set; }
    public string Relationship { get; private set; }
    public string GroupName { get; private set; }
    public string GroupNumber { get; private set; }
    public string PlanType { get; private set; }
    public string Ordinal { get; private set; } // Primary, Secondary, etc.
    public bool IsPending { get; private set; }
    public bool IsMedical { get; private set; }
    public string EmployerName { get; private set; }
    public string PlanNote { get; private set; }
    
    private PatientInsurance() { }

    public static PatientInsurance Create(
        PatientId patientId,
        int insSubNum,
        int patPlanNum,
        string carrierName,
        string subscriberId,
        string subscriberName,
        string relationship,
        string groupName,
        string groupNumber,
        string planType,
        string ordinal,
        bool isPending,
        bool isMedical,
        string employerName,
        string planNote)
    {
        return new PatientInsurance
        {
            PatientId = patientId,
            InsSubNum = insSubNum,
            PatPlanNum = patPlanNum,
            CarrierName = carrierName ?? "",
            SubscriberId = subscriberId ?? "",
            SubscriberName = subscriberName ?? "",
            Relationship = relationship ?? "",
            GroupName = groupName ?? "",
            GroupNumber = groupNumber ?? "",
            PlanType = planType ?? "",
            Ordinal = ordinal ?? "",
            IsPending = isPending,
            IsMedical = isMedical,
            EmployerName = employerName ?? "",
            PlanNote = planNote ?? ""
        };
    }
}