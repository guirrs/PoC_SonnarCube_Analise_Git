using PoC_SonarCube.DTOs.CommitsDto;
using System.Collections.Concurrent;

namespace PoC_SonarCube.Application.Services
{
    // Eh um cache -memoria temporaria do programa- que apaga desaparece quando reinicia o programa.
    //
    public class ResultStoreService
    {
        private readonly ConcurrentDictionary<string, CommitAnalysisDto> _items = new(); // chave = SHA

        // Busca a análise de um commit (null se nunca foi analisado)
        public CommitAnalysisDto? Get(string sha) => _items.GetValueOrDefault(sha);

        // Salva (ou sobrescreve) a análise de um commit
        public void Save(CommitAnalysisDto a) => _items[a.Sha] = a;

        // Todas as análises, da mais antiga para a mais recente
        public IEnumerable<CommitAnalysisDto> All() => _items.Values.OrderBy(a => a.AnalyzedAt);
    }
}
