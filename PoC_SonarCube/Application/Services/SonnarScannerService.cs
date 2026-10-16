using Microsoft.Extensions.Options;
using PoC_SonarCube.Application.Options;
using System.Text;

namespace PoC_SonarCube.Application.Services
{
    //Analisa o codigo com o SonarScanner, que envia os resultados para o SonarQube
    public class SonnarScannerService(RunnerService runner, IOptions<SonarOptions> options)
    {
        public async Task AnalyzeAsync(string workDir, string sha, string? solution, CancellationToken ct)
        {
            var o = options.Value; // configurações (URL, token, chave do projeto)

            // 1) BEGIN: avisa o scanner que a análise vai começar.
            //    projectVersion = 8 primeiros caracteres do SHA, para identificar o commit no Sonar.
            await runner.RunAsync("dotnet",
            [
                "sonarscanner", "begin",
            $"/k:{o.ProjectKey}",
            $"/d:sonar.host.url={o.Url}",
            $"/d:sonar.token={o.Token}",
            $"/d:sonar.projectVersion={sha[..8]}"
            ], workDir, ct);

            // 2) BUILD: compila o projeto. O scanner "observa" a compilação para analisar o código.
            //    --no-incremental força recompilar tudo (evita análise de código desatualizado).
            var build = new List<string> { "build", "--no-incremental" };
            if (!string.IsNullOrWhiteSpace(solution)) build.Insert(1, solution); // "dotnet build <sln> ..."
            await runner.RunAsync("dotnet", build, workDir, ct);

            // 3) END: finaliza e envia os resultados ao servidor SonarQube.
            await runner.RunAsync("dotnet",
                ["sonarscanner", "end", $"/d:sonar.token={o.Token}"], workDir, ct);
        }
    }
}
