using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PoC_SonarCube.Application.Services;
using PoC_SonarCube.DTOs.AnalisysDto;
using PoC_SonarCube.DTOs.CommitsDto;

namespace PoC_SonarCube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplexityAnalyzeController : ControllerBase
    {
        private readonly CommitAnalysisService _analyzeServices;
        private readonly ResultStoreService _resultServices;

        public ComplexityAnalyzeController(CommitAnalysisService AnalyzeServices, ResultStoreService ResultServices)
        {
            _analyzeServices = AnalyzeServices;
            _resultServices = ResultServices;
        }

        [HttpGet]
        public IActionResult Listar() => Ok(_resultServices.All());

        [HttpPost("AnalyzeCommit")]
        [ProducesResponseType(typeof(CommitReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Analisar([FromBody] AnalysisRequestDto req, CancellationToken ct)
        {
            try
            {
                var report = await _analyzeServices.AnalyzeAsync(req, ct);
                return Ok(report); // 200 com o JSON
            }
            catch (Exception ex)
            {
                return Problem(ex.Message); // 500 com a mensagem de erro
            }
        }

        [HttpGet("{sha}")]
        public IActionResult ObterPorSha(string sha) =>
        _resultServices.Get(sha) is { } resultado ? Ok(resultado) : NotFound();
    }
}
