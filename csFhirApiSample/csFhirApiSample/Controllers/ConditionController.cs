using csFhirApiSample.Models;
using csFhirApiSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace csFhirApiSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConditionController : ControllerBase
    {
        private readonly ConditionService conditionService;

        public ConditionController(ConditionService conditionService)
        {
            this.conditionService = conditionService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConditionNode>>> GetAsync(string encounterId)
        {
            var result = await conditionService.GetByEncounterAsync(encounterId);
            return Ok(result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<ConditionNode>>> GetByPatientAsync(string patientId)
        {
            var result = await conditionService.GetByPatientAsync(patientId);
            return Ok(result);
        }
    }
}
