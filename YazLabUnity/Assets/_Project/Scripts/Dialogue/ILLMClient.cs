using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonEscape.Dialogue
{
    /// <summary>
    /// Yerel dil modeli istemcisi. Asla istisna firlatmaz; her hata LLMResult/HealthStatus ile doner,
    /// boylece sunucu kapali olsa da oyun akisi bozulmaz.
    /// </summary>
    public interface ILLMClient
    {
        Task<HealthStatus> CheckHealthAsync(CancellationToken cancellation);

        /// <summary>Sohbet gecmisini gonderir; yanit JSON semasina uygun uretilir.</summary>
        Task<LLMResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellation);
    }
}
