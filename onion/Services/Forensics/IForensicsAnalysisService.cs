using System.Threading.Tasks;

namespace onion.Services
{
    public interface IForensicsAnalysisService
    {
        Task<string> AnalyzeWebsiteAsync(string targetUrl);
    }
}
