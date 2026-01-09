using csFhirApiSample.Helpers;
using csFhirApiSample.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System.Net.Http.Headers;

namespace csFhirApiSample.Services;

public class PatientService
{
    public async System.Threading.Tasks.Task<PatientModel> GetPatientAsync(string patientId)
    {
        // 1. 先建立 HttpClient，預設好 Authorization header
        HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(MagicObjectHelper.FhirBaseUrl)
        };

        FhirClientSettings settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };

        FhirClient fhirClient = new FhirClient(MagicObjectHelper.FhirBaseUrl, httpClient, settings);

        var patient = await fhirClient.ReadAsync<Patient>($"Patient/{patientId}");

        System.Console.WriteLine($"Read back patient: {patient.Name[0].ToString()}");
        PatientModel patientModel = new PatientModel();
        patientModel.Id = patient.Id;
        patientModel.Name = patient.Name[0].ToString();
        patientModel.Gender = patient.Gender?.ToString().ToLowerInvariant();
        // 取出 Patient.Identifier 裡 system = "urn:oid:2.16.840.1.113883.4.3.25" 的 identifier 當身分證號
        const string nationalIdSystem = "urn:oid:2.16.840.1.113883.4.3.25";
        var nationalId = patient.Identifier?
            .FirstOrDefault(id => id.System == nationalIdSystem)?
            .Value;
        patientModel.NationalId = nationalId;
        // 依照實作的血型 extension URL 調整這一行
        const string bloodTypeExtensionUrl = "http://example.org/fhir/StructureDefinition/patient-blood-type";
        // 從 Patient.extension 取出血型
        var bloodTypeExt = patient.Extension?.FirstOrDefault(e => e.Url == bloodTypeExtensionUrl);
        var bloodTypeCode = (bloodTypeExt?.Value as CodeableConcept)?.Coding?.FirstOrDefault()?.Code;
        // 或者如果是 valueString，就改成：
        // var bloodTypeCode = (bloodTypeExt?.Value as FhirString)?.Value;
                patientModel.BloodType = bloodTypeCode; 
        patientModel.BirthDate = patient.BirthDate;
        return patientModel;

    }
}
