namespace PoC_SonarCube.DTOs.FileDto
{
    public class FileDeltaDto
    {
        public string Path { get; set; }
        public string Status { get; set; }            // A = adicionado, M = modificado, D = removido
        public FileMetricsDto? Before { get; set; }
        public FileMetricsDto? After { get; set; }
        public int ComplexityDelta { get; set; }
        public int CognitiveComplexityDelta { get; set; }
    }
}
