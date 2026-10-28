using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace DungeonEscape.Dialogue
{
    /// <summary>
    /// Ollama HTTP API istemcisi (/api/tags, /api/chat). Istekler UnityWebRequest ile gonderilir ve
    /// tamamlanmasi await ile beklenir; ana is parcacigi bloklanmaz. Hicbir hata disari istisna olarak sizmaz.
    /// </summary>
    public class OllamaClient : ILLMClient
    {
        private readonly OllamaSettings _settings;

        public OllamaClient(OllamaSettings settings) => _settings = settings;

        private string Url(string path) => _settings.baseUrl.TrimEnd('/') + path;

        public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellation)
        {
            try
            {
                using var request = UnityWebRequest.Get(Url("/api/tags"));
                request.timeout = _settings.healthTimeoutSeconds;
                await SendAsync(request, cancellation);

                if (request.result != UnityWebRequest.Result.Success)
                    return new HealthStatus(false, false,
                        $"Ollama sunucusuna ulaşılamıyor ({_settings.baseUrl}). 'ollama serve' çalışıyor mu?");

                var models = JObject.Parse(request.downloadHandler.text)["models"]?
                    .Select(m => (string)m["name"]).ToList() ?? new List<string>();
                bool available = models.Any(IsConfiguredModel);
                return available
                    ? new HealthStatus(true, true, "Hazır")
                    : new HealthStatus(true, false,
                        $"Model yüklü değil: {_settings.model}. Terminalde: ollama pull {_settings.model}");
            }
            catch (OperationCanceledException)
            {
                return new HealthStatus(false, false, "İptal edildi");
            }
            catch (Exception e)
            {
                return new HealthStatus(false, false, $"Sunucu yanıtı okunamadı: {e.Message}");
            }
        }

        public async Task<LLMResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellation)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var body = new JObject
                {
                    ["model"] = _settings.model,
                    ["stream"] = false,
                    ["keep_alive"] = _settings.keepAlive,
                    ["options"] = new JObject { ["temperature"] = _settings.temperature },
                    ["messages"] = JArray.FromObject(messages),
                };
                if (!string.IsNullOrEmpty(jsonSchema)) body["format"] = JToken.Parse(jsonSchema);

                using var request = new UnityWebRequest(Url("/api/chat"), UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString())),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = _settings.timeoutSeconds
                };
                request.SetRequestHeader("Content-Type", "application/json");
                await SendAsync(request, cancellation);
                float latency = (float)watch.Elapsed.TotalSeconds;

                var failure = Classify(request, latency);
                if (failure.HasValue) return failure.Value;

                var content = (string)JObject.Parse(request.downloadHandler.text)["message"]?["content"];
                return string.IsNullOrWhiteSpace(content)
                    ? LLMResult.Fail(LLMErrorKind.EmptyResponse, "Model boş yanıt döndürdü.", latency)
                    : LLMResult.Ok(content, latency);
            }
            catch (OperationCanceledException)
            {
                return LLMResult.Fail(LLMErrorKind.Cancelled, "İstek iptal edildi.", (float)watch.Elapsed.TotalSeconds);
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                return LLMResult.Fail(LLMErrorKind.EmptyResponse, $"Sunucu yanıtı okunamadı: {e.Message}",
                    (float)watch.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                // Ornegin guvensiz HTTP engeli veya gecersiz adres: istek hic gonderilemedi
                return LLMResult.Fail(LLMErrorKind.ServerUnreachable, $"İstek gönderilemedi ({_settings.baseUrl}): {e.Message}",
                    (float)watch.Elapsed.TotalSeconds);
            }
        }

        private LLMResult? Classify(UnityWebRequest request, float latency)
        {
            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    return null;
                case UnityWebRequest.Result.ConnectionError when IsTimeout(request.error):
                    return LLMResult.Fail(LLMErrorKind.Timeout,
                        $"Model {_settings.timeoutSeconds} sn içinde yanıt vermedi.", latency);
                case UnityWebRequest.Result.ConnectionError:
                    return LLMResult.Fail(LLMErrorKind.ServerUnreachable,
                        $"Ollama sunucusuna ulaşılamıyor ({_settings.baseUrl}): {request.error}", latency);
                case UnityWebRequest.Result.ProtocolError when request.responseCode == 404:
                    return LLMResult.Fail(LLMErrorKind.ModelMissing,
                        $"Model bulunamadı: {_settings.model}. Terminalde: ollama pull {_settings.model}", latency);
                default:
                    return LLMResult.Fail(LLMErrorKind.HttpError,
                        $"HTTP {request.responseCode}: {request.error}", latency);
            }
        }

        private static bool IsTimeout(string error) =>
            error != null && error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;

        private bool IsConfiguredModel(string name) =>
            name == _settings.model || (!_settings.model.Contains(':') && name == _settings.model + ":latest");

        /// <summary>UnityWebRequest'i bloklamadan bekler; iptal edilirse istegi durdurur.</summary>
        private static async Task SendAsync(UnityWebRequest request, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var completion = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => completion.TrySetResult(true);
            using (cancellation.Register(() =>
                   {
                       request.Abort();
                       completion.TrySetCanceled();
                   }))
            {
                await completion.Task;
            }
        }
    }
}
