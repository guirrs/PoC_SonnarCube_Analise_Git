namespace PoC_SonarCube.Application.Options
{
    public class SonarOptions
    {
        public string Url { get; set; } = "http://localhost:9000";
        public string Token { get; set; } = "";
        public string ProjectKey { get; set; } = "poc-complexidade";
    }
}
