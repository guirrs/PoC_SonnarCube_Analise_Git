using PoC_SonarCube.Application.APIs;
using PoC_SonarCube.DTOs.AnalisysDto;
using PoC_SonarCube.DTOs.CommitsDto;
using PoC_SonarCube.DTOs.FileDto;

namespace PoC_SonarCube.Application.Services
{
    public class CommitAnalysisService(GitServices git, SonarScannerService scanner, SonarClientAPI sonar, ResultStoreService store)
    {
        // Trava que permite UMA análise por vez, necessário porque todas usam o mesmo projeto no Sonar e ele não deve
        // receber análises simultâneas
        private static readonly SemaphoreSlim Gate = new(1, 1);

        public async Task<CommitReportDto> AnalyzeAsync(AnalysisRequestDto req, CancellationToken ct)
        {
            await Gate.WaitAsync(ct); // espera a vez; se outra análise estiver rodando, fica na fila
            try
            {
                // 1) Descobre o SHA completo do commit e o do pai
                var sha = await git.ResolveAsync(req.RepoPath, req.CommitSha, ct);
                var parentSha = await git.ParentAsync(req.RepoPath, sha, ct);

                // 2) Garante que o pai foi analisado ANTES do commit atual.
                //    Assim, o "código novo" do Sonar (previous version) fica coerente.
                CommitAnalysisDto? parent = parentSha is null ? null : await EnsureAnalyzedAsync(req, parentSha, ct);
                var current = await EnsureAnalyzedAsync(req, sha, ct);

                // 3) Quais arquivos .cs mudaram neste commit
                var changed = await git.ChangedCsFilesAsync(req.RepoPath, parentSha, sha, ct);

                // Dicionários por caminho para buscar rápido as métricas de cada arquivo
                var before = parent?.Files.ToDictionary(f => f.Path) ?? []; // métricas no pai
                var after = current.Files.ToDictionary(f => f.Path);         // métricas no commit

                // 4) Para cada arquivo alterado, calcula a variação de complexidade
                var deltas = changed.Select(c =>
                {
                    before.TryGetValue(c.Path, out var b); // null se o arquivo é novo
                    after.TryGetValue(c.Path, out var a);  // null se o arquivo foi removido
                    return new FileDeltaDto {
                        Path = c.Path,
                        Status = c.Status,
                        Before = b,
                        After = a,
                        ComplexityDelta = (a?.Complexity ?? 0) - (b?.Complexity ?? 0),
                        CognitiveComplexityDelta = (a?.CognitiveComplexity ?? 0) - (b?.CognitiveComplexity ?? 0)
                    };
                }).ToList();

                // Variação do projeto inteiro (atual - pai). Sem pai, não há com o que comparar.
                var projectDelta = parent is null ? null : new ComplexityMetricsDto {
                    Complexity = current.Project.Complexity - parent.Project.Complexity,
                    CognitiveComplexity = current.Project.CognitiveComplexity - parent.Project.CognitiveComplexity,
                    Ncloc = current.Project.Ncloc - parent.Project.Ncloc
                };

                // 5) Monta a resposta final
                return new CommitReportDto{
                    Sha = sha,
                    ParentSha = parentSha,
                    Project = current.Project,
                    ProjectDelta = projectDelta,
                    ChangedFiles = deltas
                };
            }
            finally { Gate.Release(); } // libera a vez, mesmo se ocorrer erro
        }

        // Garante que um commit tem análise. Se já está no cache, devolve direto;
        // se não, executa o processo completo.
        private async Task<CommitAnalysisDto> EnsureAnalyzedAsync(AnalysisRequestDto req, string sha, CancellationToken ct)
        {
            if (store.Get(sha) is { } cached) return cached; // já analisado: pula tudo

            // Pasta temporária única para este commit (usa 12 caracteres do SHA)
            var dir = Path.Combine(Path.GetTempPath(), "sonar-poc", sha[..12]);

            // Cria a cópia do código naquele commit
            await git.AddWorktreeAsync(req.RepoPath, dir, sha, ct);
            try
            {
                await scanner.AnalyzeAsync(dir, sha, req.SolutionPath, ct); // begin + build + end
                await sonar.WaitForAnalysisAsync(ct);                       // espera o Sonar processar

                // Só agora lê os resultados, já atualizados
                var result = new CommitAnalysisDto {
                    Sha = sha,
                    AnalyzedAt = DateTime.UtcNow,
                    Project = await sonar.GetProjectMetricsAsync(ct),
                    Files = await sonar.GetFileMetricsAsync(ct)
                    };

                store.Save(result); // guarda no cache
                return result;
            }
            finally
            {
                // Sempre apaga a pasta temporária, dando certo ou não.
                // CancellationToken.None: a limpeza roda mesmo se a requisição foi cancelada.
                await git.RemoveWorktreeAsync(req.RepoPath, dir, CancellationToken.None);
            }
        }
    }
}
