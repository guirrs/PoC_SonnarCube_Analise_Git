using System.Text;

namespace PoC_SonarCube.Application.Services
{
    public class GitServives(RunnerService runner)
    {

        //Usuado como base de comparacao quando nao haver o primeiro commit no repositorio
        private const string EmptyTree = "4b825dc642cb6eb9a060e54bf8d69288fbd2f8d9";

        // Converte "HEAD", "main~2", SHA curto etc. no SHA completo (40 caracteres).
        // Também valida que o commit existe
        public async Task<string> ResolveAsync(string repo, string rev, CancellationToken ct) =>
            (await runner.RunAsync("git", ["-C", repo, "rev-parse", "--verify", rev], null, ct)).Trim();

        // Descobre o commit pai ("sha^"). Se não existir (commit raiz), devolve null.
        public async Task<string?> ParentAsync(string repo, string sha, CancellationToken ct)
        {
            try
            {
                return (await runner.RunAsync("git",
                    ["-C", repo, "rev-parse", "--verify", "--quiet", $"{sha}^"], null, ct)).Trim();
            }
            catch (InvalidOperationException) { return null; } // commit raiz: não há pai
        }

        // Cria uma cópia do código naquele commit em outra pasta (worktree), sem alterar a pasta de trabalho e a branch
        public async Task AddWorktreeAsync(string repo, string path, string sha, CancellationToken ct)
        {
            // Se sobrou uma pasta de uma execução anterior que falhou, apaga
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);

            // "prune" limpa registros de worktrees que não existem mais
            await  runner.RunAsync("git", ["-C", repo, "worktree", "prune"], null, ct);

            // --detach: não cria nem troca de branch, só abre o commit solto
            await runner.RunAsync("git", ["-C", repo, "worktree", "add", "--detach", path, sha], null, ct);
        }

        // Remove o worktree (apaga a pasta temporária e o registro no Git)
        public Task RemoveWorktreeAsync(string repo, string path, CancellationToken ct) =>
            runner.RunAsync("git", ["-C", repo, "worktree", "remove", "--force", path], null, ct);

        // Lista os arquivos .cs que mudaram entre o pai e o commit, com o status de cada um.
        public async Task<IReadOnlyList<(string Status, string Path)>> ChangedCsFilesAsync(
            string repo, string? parentSha, string sha, CancellationToken ct)
        {
            // --name-status: mostra só o status e o nome (A/M/D + caminho)
            // --no-renames: renomeação aparece como D (antigo) + A (novo), mais simples de tratar
            // "-- *.cs": filtra apenas arquivos C#
            var output = await runner.RunAsync("git",
                ["-C", repo, "diff", "--name-status", "--no-renames", parentSha ?? EmptyTree, sha, "--", "*.cs"],
                null, ct);

            // Cada linha vem como "M<TAB>caminho/arquivo.cs"
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.Split('\t'))
                .Select(p => (Status: p[0][..1], Path: p[^1])) // primeira letra do status e último campo (caminho)
                .ToList();
        }
    }
}
