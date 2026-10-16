namespace PoC_SonarCube.DTOs.FileDto
{
    public class FileDelta
    {
        public string Path { get; set; }
        public string Status { get; set; }            // A = adicionado, M = modificado, D = removido
        public FileMetrics? Before { get; set; }
        public FileMetrics? After { get; set; }
        public int ComplexityDelta { get; set; }
        public int CognitiveComplexityDelta { get; set; }
    }
}
