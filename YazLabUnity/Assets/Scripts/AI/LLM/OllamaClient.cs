using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient : MonoBehaviour
{
    [SerializeField] private string ollamaUrl = "http://localhost:11434/api/generate";
    [SerializeField] private string modelName = "llama3.2:1b";

    [TextArea(3, 10)]
    [SerializeField] private string systemPrompt = 
        "Sen kilitli bir geçidi koruyan bilge bir muhafızsın. " +
        "Yanıtlarını SADECE aşağıdaki JSON formatında ver, ekstra hiçbir açıklama yazma:\n" +
        "{\n" +
        "  \"dialogue\": \"Oyuncuya cevabın\",\n" +
        "  \"action\": \"OPEN_DOOR veya REJECT\",\n" +
        "  \"confidence\": 0.95\n" +
        "}";

    public async Task<NPCStructuredOutput> SendPromptAsync(string playerMessage)
    {
        OllamaRequest reqData = new OllamaRequest
        {
            model = modelName,
            prompt = playerMessage,
            system = systemPrompt,
            stream = false
        };

        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] rawBody = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(rawBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            try
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Ollama Bağlantı Hatası: " + request.error);
                    return GetFallbackResponse("Sunucuya ulaşılamıyor, muhafız derin uykuda.");
                }

                string rawJsonResponse = request.downloadHandler.text;
                OllamaResponse ollamaResp = JsonUtility.FromJson<OllamaResponse>(rawJsonResponse);

                NPCStructuredOutput parsedOutput = JsonUtility.FromJson<NPCStructuredOutput>(ollamaResp.response);
                
                if (parsedOutput == null || string.IsNullOrEmpty(parsedOutput.action))
                {
                    return GetFallbackResponse("Muhafız anlaşılmaz bir şeyler mırıldandı.");
                }

                return parsedOutput;
            }
            catch (Exception ex)
            {
                Debug.LogError("LLM Hata Yönetimi Tetiklendi: " + ex.Message);
                return GetFallbackResponse("Bir aksilik oldu, muhafız seni duyamadı.");
            }
        }
    }

    private NPCStructuredOutput GetFallbackResponse(string fallbackDialogue)
    {
        return new NPCStructuredOutput
        {
            dialogue = fallbackDialogue,
            action = "REJECT",
            confidence = 0.0f
        };
    }
}