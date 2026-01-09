using csFhirApiSample.Models;
using csFhirApiSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace csFhirApiSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EncounterController : ControllerBase
    {
        private readonly EncounterService encounterService;

        public EncounterController(EncounterService encounterService)
        {
            this.encounterService = encounterService;
        }

        [HttpGet]
        public async Task<ActionResult<List<EncounterNode>>> GetAsync(string patientId)
        {
            var result = await encounterService.GetAsync(patientId);
            return Ok(result);
        }
    }
}
