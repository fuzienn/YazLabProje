using System;

// Ollama API'sine gönderilecek istek gövdesi
[Serializable]
public class OllamaRequest
{
    public string model;
    public string prompt;
    public string system;
    public bool stream = false;
}

// Ollama'dan dönen ham HTTP yanıtı
[Serializable]
public class OllamaResponse
{
    public string response;
}

// Modelin ürettiği ve oyunu yöneteceğimiz Yapılandırılmış (Structured) JSON verisi
[Serializable]
public class NPCStructuredOutput
{
    public string dialogue;  // NPC nin oyuncuya söyleyeceği söz
    public string action;    // "OPEN_DOOR", "REJECT", "ATTACK" vb.
    public float confidence; // Modelin karar güven oranı
}