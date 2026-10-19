using Microsoft.Extensions.Options;
using PoC_SonarCube.Application.Options;
using PoC_SonarCube.DTOs.AnalisysDto;
using PoC_SonarCube.DTOs.FileDto;
using System.Globalization;
using System.Text.Json;

namespace PoC_SonarCube.Application.APIs
{
    public class SonarClientAPI(HttpClient http, IOptions<SonarOptions> options)
    {
        private string Key => options.Value.ProjectKey;

        // O Sonar ainda processa tudo em segundo plano(fila do Compute Engine). Aqui esperamos essa fila esvaziar.
        public async Task WaitForAnalysisAsync(CancellationToken ct)
        {
            for (var i = 0; i < 150; i++) // 150 tentativas x 2s = ~5 minutos
            {
                using var doc = await GetJsonAsync($"api/ce/component?component={Key}", ct);
                var root = doc.RootElement;

                // "queue" = tarefas pendentes/em andamento. Vazia = terminou de processar.
                if (root.GetProperty("queue").GetArrayLength() == 0)
                {
                    // "current" = última tarefa concluída; o status diz se deu certo
                    var status = root.TryGetProperty("current", out var c)
                        ? c.GetProperty("status").GetString() : null;

                    if (status == "SUCCESS") return;
                    throw new InvalidOperationException($"Processamento no Sonar terminou com status '{status}'.");
                }

                await Task.Delay(2000, ct); // espera 2s e tenta de novo
            }
            throw new TimeoutException("O SonarQube demorou demais para processar a análise.");
        }

        // Busca as métricas do projeto INTEIRO
        public async Task<ComplexityMetricsDto> GetProjectMetricsAsync(CancellationToken ct)
        {
            using var doc = await GetJsonAsync(
                $"api/measures/component?component={Key}&metricKeys=complexity,cognitive_complexity,ncloc", ct);

            var m = ReadMeasures(doc.RootElement.GetProperty("component").GetProperty("measures"));

            // GetValueOrDefault devolve 0 quando a métrica não existe (ex.: projeto sem código)
            return new ComplexityMetricsDto{
                Complexity = m.GetValueOrDefault("complexity"),
                CognitiveComplexity = m.GetValueOrDefault("cognitive_complexity"),
                Ncloc = m.GetValueOrDefault("ncloc")
            };
        }

        // Busca as métricas de CADA arquivo do projeto
        public async Task<List<FileMetricsDto>> GetFileMetricsAsync(CancellationToken ct)
        {
            var files = new List<FileMetricsDto>();

            // A API devolve no máximo 500 itens por página, então percorremos as páginas
            for (var page = 1; ; page++)
            {
                // qualifiers=FIL: apenas arquivos (e não pastas ou o projeto)
                using var doc = await GetJsonAsync(
                    $"api/measures/component_tree?component={Key}&qualifiers=FIL" +
                    $"&metricKeys=complexity,cognitive_complexity&ps=500&p={page}", ct);

                foreach (var c in doc.RootElement.GetProperty("components").EnumerateArray())
                {
                    var m = ReadMeasures(c.GetProperty("measures"));
                    files.Add(new FileMetricsDto {
                        Path = c.GetProperty("path").GetString()!, // caminho relativo do arquivo
                        Complexity = m.GetValueOrDefault("complexity"),
                        CognitiveComplexity = m.GetValueOrDefault("cognitive_complexity")
                    });
                }

                // Para quando já passamos do total de itens
                var paging = doc.RootElement.GetProperty("paging");
                if (page * paging.GetProperty("pageSize").GetInt32() >= paging.GetProperty("total").GetInt32())
                    break;
            }
            return files;
        }

        // Converte a lista de medidas do JSON ([{metric:"ncloc", value:"38"}, ...])
        // em um dicionário { "ncloc": 38, ... }. O Sonar manda os valores como texto.
        private static Dictionary<string, int> ReadMeasures(JsonElement measures) =>
            measures.EnumerateArray().ToDictionary(
                m => m.GetProperty("metric").GetString()!,
                m => int.Parse(m.GetProperty("value").GetString()!, CultureInfo.InvariantCulture));

        // Faz o GET, valida o status HTTP e devolve o JSON já interpretado
        private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
        {
            using var resp = await http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode(); // lança exceção se for 401, 404, 500...
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
    }
}
