using System.Diagnostics;

namespace PoC_SonarCube.Application.Services
{
    // Ele fara o papel de terminal para conectar o git com o .NET, automação de processos, e tambem para conectar com o SonarQube
    public class RunnerService
    {
        public async Task<string> RunAsync(
            string file, //programa que ira executar
            IEnumerable<string> args, //argumentos que serao passados para o programa
            string? WorkingDirectory = null, //Onde o comando vai rodar
            CancellationToken ct = default)
        {
            //configura a saida padrao e de erro
            var psi = new ProcessStartInfo(file)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = WorkingDirectory ?? Environment.CurrentDirectory,
            };

            //trata de espacoes e aspas sozinhos
            foreach(var a in args) psi.ArgumentList.Add(a);

            using var p = Process.Start(psi)
             ?? throw new InvalidOperationException($"Não foi possível iniciar {file}");

            // Lê stdout e stderr ao mesmo tempo. Se lesse um de cada vez, o processo poderia travar quando o buffer do outro enchesse.
            var stdout = p.StandardOutput.ReadToEndAsync(ct); //stdout eh o output do sistema
            var stderr = p.StandardError.ReadToEndAsync(ct); //stderr eh o output do sistema, mas de erro, caso o comando falhe
            await p.WaitForExitAsync(ct);

            var output = await stdout;
            var error = await stderr;

            // Exit code diferente de 0 significa que o comando falhou
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"{file} falhou (exit {p.ExitCode}): {error}{output}");

            return output; // o que o comando imprimiu
        }
    }
}