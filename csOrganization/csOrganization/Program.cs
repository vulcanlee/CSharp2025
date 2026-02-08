using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace csOrganization;

internal class Program
{
    private const string FHIR_BASE = "https://server.fire.ly";

    private const string ORG_ID_SYSTEM = "https://vulcan.org/fhir/identifier/org-code";
    private const string DEPT_ID_SYSTEM = "https://vulcan.org/fhir/identifier/dept-code";
    private record HospitalSeed(string Code, string LegalName, string DisplayName);

    static async System.Threading.Tasks.Task Main(string[] args)
    {
        var client = CreateFhirClient(FHIR_BASE);

        // 建立 3 家醫院（院級 Organization）
        var hospitals = new[]
        {
            new HospitalSeed("NCKUH", "國立成功大學醫學院附設醫院", "成大醫院"),
            new HospitalSeed("CMH",   "奇美醫院", "奇美"),
            new HospitalSeed("KGH",   "郭綜合醫院", "郭綜合")
        };

        Organization createdHospital = new Organization();
        Organization createdDept = new Organization();
        foreach (var h in hospitals)
        {
            // 建院級 Organization
            var hospitalOrg = BuildHospitalOrganization(h);

            try
            {
                createdHospital = await client.CreateAsync(hospitalOrg);
                Console.WriteLine($"已經建立醫院組織 : {h.DisplayName} => {createdHospital.Id}");
                Console.WriteLine($"{createdHospital.ToJson()}");
            }
            catch (FhirOperationException ex)
            {
                Console.WriteLine($"建立醫院組織發生錯誤 : {h.DisplayName}");
                Console.WriteLine(ex.Message);
                if (ex.Outcome != null) Console.WriteLine(ex.Outcome.ToJson());
                continue;
            }

            // 建立婦產科（Department）並 partOf 指向該醫院
            var obgynDept = BuildDepartmentOrganization(
                deptCode: $"{h.Code}-OBGYN",
                deptName: "婦產科",
                parentHospitalId: createdHospital.Id
            );

            try
            {
                createdDept = await client.CreateAsync(obgynDept);
                Console.WriteLine($"已經建立醫院科室 : {h.DisplayName} / 婦產科 => {createdDept.Id}");
                Console.WriteLine($"{createdDept.ToJson()}");
            }
            catch (FhirOperationException ex)
            {
                Console.WriteLine($"建立醫院科室發生錯誤 : {h.DisplayName} / 婦產科");
                Console.WriteLine(ex.Message);
                if (ex.Outcome != null) Console.WriteLine(ex.Outcome.ToJson());
            }

            Console.WriteLine("--------------------------------------------------");

            #region 移除剛剛建立的 醫院與科室 紀錄
            Console.WriteLine($"開始刪除測試資料 : {h.DisplayName} 以及其下的科室...");
            try
            {
                await client.DeleteAsync($"Organization/{createdDept.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("刪除測試資料發生錯誤 : ");
                Console.WriteLine(ex.Message);
            }
            try
            {
                await client.DeleteAsync($"Organization/{createdHospital.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("刪除測試資料發生錯誤 : ");
                Console.WriteLine(ex.Message);
            }
            #endregion
        }
    }

    private static FhirClient CreateFhirClient(string baseUrl)
    {
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            VerifyFhirVersion = true,
            Timeout = 60_000
        };

        var client = new FhirClient(baseUrl, settings);
        return client;
    }

    private static Organization BuildHospitalOrganization(HospitalSeed h)
    {
        Organization organization = new Organization
        {
            Active = true,
            Identifier = new List<Identifier>
            {
                new Identifier(ORG_ID_SYSTEM, h.Code) { Use = Identifier.IdentifierUse.Official }
            },
            Name = h.LegalName,
            Alias = new List<string> { h.DisplayName },
            Type = new List<CodeableConcept>
            {
                // HL7 organization-type: prov = Healthcare Provider
                new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/organization-type",
                    "prov",
                    "Healthcare Provider",
                    text: "Hospital"
                )
            }
        };
        return organization;
    }

    private static Organization BuildDepartmentOrganization(string deptCode, string deptName, string parentHospitalId)
    {
        return new Organization
        {
            Active = true,
            Identifier = new List<Identifier>
            {
                new Identifier(DEPT_ID_SYSTEM, deptCode) { Use = Identifier.IdentifierUse.Official }
            },
            Name = deptName,
            Type = new List<CodeableConcept>
            {
                // HL7 organization-type: dept = Hospital Department
                new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/organization-type",
                    "dept",
                    "Hospital Department",
                    text: deptName
                )
            },
            PartOf = new ResourceReference($"Organization/{parentHospitalId}")
        };
    }
}
