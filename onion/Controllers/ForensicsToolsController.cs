using Microsoft.AspNetCore.Mvc;
using onion.Models;
using onion.Services;
using System;
using System.Threading.Tasks;

namespace onion.Controllers
{
    [Route("[controller]/[action]")]
    public class ForensicsToolsController : Controller
    {
        private readonly IForensicsAnalysisService _analysisService;

        public ForensicsToolsController(IForensicsAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeWithAI([FromBody] ForensicsToolsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TargetUrl))
            {
                return BadRequest(new { Error = "No URL was provided." });
            }

            try
            {
                var analysisResult = await _analysisService.AnalyzeWebsiteAsync(model.TargetUrl);
                return Ok(new { AiAnalysis = analysisResult });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
