using PoC_SonarCube.DTOs.AnalisysDto;
using PoC_SonarCube.DTOs.FileDto;

namespace PoC_SonarCube.DTOs.CommitsDto
{
    public class CommitReportDto
    {
        public string Sha { get; set; }
        public string? ParentSha { get; set; }
        public ComplexityMetricsDto Project { get; set; }
        public ComplexityMetricsDto? ProjectDelta { get; set; }
        public IReadOnlyList<FileDeltaDto> ChangedFiles { get; set; }

    }
}
