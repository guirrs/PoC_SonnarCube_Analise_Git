namespace PoC_SonarCube.DTOs.FileDto
{
    public class FileMetrics
    {
        public string Path { get; set; }
        public int Complexity { get; set; }
        public int CognitiveComplexity { get; set; }
    }
}
