using PMSIntegration.Core.Patients.Models;
using PMSIntegration.Core.Patients.ValueObjects;
using PMSIntegration.Infrastructure.OpenDental.DTO;

namespace PMSIntegration.Infrastructure.OpenDental.Extensions;

public static class OpenDentalInsuranceMapper
{
    public static PatientInsurance ToDomain(this OpenDentalInsuranceDto dto)
    {
        return PatientInsurance.Create(
            patientId: new PatientId(dto.PatNum),
            insSubNum: dto.InsSubNum,
            patPlanNum: dto.PatPlanNum,
            carrierName: dto.CarrierName ?? "",
            subscriberId: dto.SubscriberID ?? "",
            subscriberName: dto.SubscriberName ?? "",
            relationship: dto.Relationship ?? "",
            groupName: dto.GroupName ?? "",
            groupNumber: dto.GroupNum ?? "",
            planType: dto.PlanTypeDescription ?? "",
            ordinal: dto.OrdinalDescription ?? "",
            isPending: dto.IsPending?.ToLower() == "true",
            isMedical: dto.IsMedical?.ToLower() == "true",
            employerName: dto.EmployerName ?? "",
            planNote: dto.PlanNote ?? ""
        );
    }
}