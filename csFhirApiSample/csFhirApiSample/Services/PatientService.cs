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
        patientModel.BirthDate = patient.BirthDate;
        return patientModel;

    }
}
