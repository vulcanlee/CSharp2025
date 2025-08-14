using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace csPatientCRUD;

internal class Program
{
    // HAPI R4 public server
    private const string FhirBaseUrl = "https://hapi.fhir.org/baseR4";

    static async System.Threading.Tasks.Task Main()
    {
        //string GivenName = { new HumanName().WithGiven("Jane").AndFamily("Doe") },
        string GivenName = "Vulcan20250814111";
        string FamilynName = "Lee";

         var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            PreferredReturn = Prefer.ReturnRepresentation,
            Timeout = 60_000
        };

        var client = new FhirClient(FhirBaseUrl, settings);

        try
        {
            // ============== Create ==============
            string identityValue = $"MRN-{Guid.NewGuid():N}".Substring(0, 12); 
            var newPatient = new Patient
            {
                Identifier =
                {
                    new Identifier("http://example.org/mrn", identityValue)
                },
                Name = { new HumanName().WithGiven(GivenName).AndFamily(FamilynName) },
                Gender = AdministrativeGender.Female,
                BirthDate = "1990-01-01",
                Telecom = { new ContactPoint(ContactPoint.ContactPointSystem.Phone, ContactPoint.ContactPointUse.Mobile, "0912-345-678") },
                Active = true
            };

            Console.WriteLine("Creating Patient ...");
            var json = newPatient.ToJson();
            Console.WriteLine($"JSON: {json}");
            var created = await client.CreateAsync(newPatient); // POST /Patient
            Console.WriteLine($"Created: id={created.Id}, version={created.Meta?.VersionId}");

            PressAnyKeyToContinue();

            // ============== Read ==============
            Console.WriteLine("Reading Patient by id ...");
            var readBack = await client.ReadAsync<Patient>($"Patient/{created.Id}"); // GET /Patient/{id}
            Console.WriteLine($"Read: {readBack.Name?.FirstOrDefault()} | active={readBack.Active}");

            PressAnyKeyToContinue();

            // ============== Update ==============
            Console.WriteLine("Updating Patient (add email, set active=false) ...");
            readBack.Active = false;
            readBack.Telecom.Add(new ContactPoint(ContactPoint.ContactPointSystem.Email, null, $"{GivenName}.{FamilynName}@example.org"));
            var updated = await client.UpdateAsync(readBack); // PUT /Patient/{id}
            Console.WriteLine($"Updated: version={updated.Meta?.VersionId}, telecom={string.Join(", ", updated.Telecom.Select(t => $"{t.System}:{t.Value}"))}");

            PressAnyKeyToContinue();

            // ============== Search ==============
            // 以 family name 搜尋，或用 identifier 精準搜尋
            Console.WriteLine($@"Searching Patient by family name '{FamilynName}' ...");
            var bundle = await client.SearchAsync<Patient>(new string[] { $"family={FamilynName}", "_count=5" }); // GET /Patient?family=Doe&_count=5
            Console.WriteLine($"Search total (if provided): {bundle.Total}");
            foreach (var entry in bundle.Entry ?? Enumerable.Empty<Bundle.EntryComponent>())
            {
                if (entry.Resource is Patient p)
                    Console.WriteLine($" - {p.Id} | {p.Name?.FirstOrDefault()} | active={p.Active}");
            }

            PressAnyKeyToContinue();

            // ============== Delete ==============
            Console.WriteLine("Deleting Patient ...");
            await client.DeleteAsync($"Patient/{created.Id}"); // DELETE /Patient/{id}
            Console.WriteLine("Deleted.");

            // 驗證刪除（預期 404）
            try
            {
                await client.ReadAsync<Patient>($"Patient/{created.Id}");
                Console.WriteLine("⚠️ Still readable (server may be eventual consistent).");
            }
            catch (FhirOperationException foe) when ((int)foe.Status == 404)
            {
                Console.WriteLine("Confirmed 404 Not Found after delete.");
            }

            PressAnyKeyToContinue();

        }
        catch (FhirOperationException foe)
        {
            Console.WriteLine($"FHIR error: HTTP {(int)foe.Status} {foe.Status}");
            if (foe.Outcome is OperationOutcome oo)
            {
                foreach (var i in oo.Issue)
                    Console.WriteLine($" - {i.Severity} {i.Code}: {i.Details?.Text}");
            }
            else
            {
                Console.WriteLine(foe.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR: " + ex.Message);
        }
    }

    // press any key to continue
    private static void PressAnyKeyToContinue()
    {
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
