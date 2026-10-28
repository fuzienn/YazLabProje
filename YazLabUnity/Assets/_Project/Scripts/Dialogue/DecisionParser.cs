using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DungeonEscape.Dialogue
{
    /// <summary>
    /// Model ciktisini dogrular. Sema zorlansa da kucuk modeller kod blogu, fazladan metin veya
    /// alan disi deger uretebilir; bunlar onarilir ve uyari olarak kaydedilir. Onarilamayan cikti Fail doner.
    /// </summary>
    public class DecisionParser : IDecisionParser
    {
        public ParseResult Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return ParseResult.Fail("Boş çıktı");

            var warnings = new List<string>();
            var json = ExtractJsonObject(raw);
            if (json == null) return ParseResult.Fail("Çıktıda JSON nesnesi yok");
            if (json.Length != raw.Trim().Length) warnings.Add("JSON dışındaki metin atıldı");

            JObject obj;
            try
            {
                obj = JObject.Parse(json);
            }
            catch (JsonException e)
            {
                return ParseResult.Fail($"Geçersiz JSON: {e.Message}");
            }

            var reply = ((string)obj["reply"])?.Trim();
            if (string.IsNullOrEmpty(reply)) return ParseResult.Fail("'reply' alanı yok veya boş");

            int fear = ReadFear(obj["fear"], warnings);
            var mood = ReadEnum(obj["mood"], GuardMood.Calm, "mood", warnings);
            var action = ReadEnum(obj["action"], GuardAction.None, "action", warnings);

            return ParseResult.Ok(new GuardDecision(reply, fear, mood, action), warnings);
        }

        /// <summary>Ilk '{' ile ona denk gelen '}' arasini alir (kod bloklari ve on/son metin atilir).</summary>
        private static string ExtractJsonObject(string raw)
        {
            int start = raw.IndexOf('{');
            if (start < 0) return null;

            int depth = 0;
            bool inString = false;
            for (int i = start; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c == '"' && raw[i - 1] != '\\') inString = !inString;
                if (inString) continue;
                if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return raw.Substring(start, i - start + 1);
            }
            return null;
        }

        private static int ReadFear(JToken token, List<string> warnings)
        {
            if (token == null || !double.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                warnings.Add("'fear' okunamadı, 0 kabul edildi");
                return 0;
            }
            int fear = (int)Math.Round(value);
            if (fear is >= 0 and <= 100) return fear;
            warnings.Add($"'fear' {fear} aralık dışı, sınırlandı");
            return Math.Clamp(fear, 0, 100);
        }

        /// <summary>"OPEN_DOOR", "open door", "OpenDoor" gibi yazimlari ayni enum'a esler.</summary>
        private static T ReadEnum<T>(JToken token, T fallback, string field, List<string> warnings) where T : struct, Enum
        {
            var text = token?.ToString().Replace("_", "").Replace(" ", "");
            // Sayisal metinler ("7") tanimsiz enum degeri uretmesin diye IsDefined ile dogrulanir
            if (!string.IsNullOrEmpty(text) && !char.IsDigit(text[0]) && Enum.TryParse(text, true, out T value)
                && Enum.IsDefined(typeof(T), value)) return value;
            warnings.Add($"'{field}' geçersiz ('{token}'), {fallback} kabul edildi");
            return fallback;
        }
    }
}
