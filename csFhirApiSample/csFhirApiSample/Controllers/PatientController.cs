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
        private readonly ObservationHeightWeightService observationHeightWeightService;

        public PatientController(PatientService patientService,
            ObservationHeightWeightService observationHeightWeightService)
        {
            this.patientService = patientService;
            this.observationHeightWeightService = observationHeightWeightService;
        }

        [HttpGet]
        public async Task<ActionResult<PatientModel>> Get(string patientId)
        {
            var patient = await patientService.GetPatientAsync(patientId);
            patient = await observationHeightWeightService.GetAsync(patient);
            return Ok(patient);
        }
    }
}
