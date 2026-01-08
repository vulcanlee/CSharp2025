using csFhirApiSample.Models;
using csFhirApiSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace csFhirApiSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly PatientService patientService;

        public PatientController(PatientService patientService)
        {
            this.patientService = patientService;
        }

        [HttpGet]
        public async Task<ActionResult<PatientModel>> Get(string patientId)
        {
            var patient = await patientService.GetPatientAsync(patientId);
            return Ok(patient);
        }
    }
}
