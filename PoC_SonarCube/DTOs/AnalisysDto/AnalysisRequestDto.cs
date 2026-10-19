namespace PoC_SonarCube.DTOs.AnalisysDto
{
    public class AnalysisRequestDto
    {
        public string RepoPath { get; set; }
        public string CommitSha { get; set; }
        public string? SolutionPath { get; set; }
    }
}
