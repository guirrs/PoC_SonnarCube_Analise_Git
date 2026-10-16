using PoC_SonarCube.DTOs.AnalisysDto;
using PoC_SonarCube.DTOs.FileDto;

namespace PoC_SonarCube.DTOs.CommitsDto
{
    public class CommitAnalysisDto
    {
        public string Sha { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public ComplexityMetricsDto Project { get; set; }
        public IReadOnlyList<FileMetrics> Files { get; set; }
    }
}
