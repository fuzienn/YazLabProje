using DungeonEscape.Dialogue;
using NUnit.Framework;

namespace DungeonEscape.Tests
{
    public class DecisionParserTests
    {
        private readonly DecisionParser _parser = new();

        [Test]
        public void ValidJson_IsParsed()
        {
            var result = _parser.Parse(@"{""reply"":""Geri çekil!"",""fear"":42,""mood"":""nervous"",""action"":""NONE""}");

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Geri çekil!", result.Decision.Reply);
            Assert.AreEqual(42, result.Decision.Fear);
            Assert.AreEqual(GuardMood.Nervous, result.Decision.Mood);
            Assert.AreEqual(GuardAction.None, result.Decision.Action);
            Assert.IsEmpty(result.Warnings);
        }

        [TestCase("OPEN_DOOR")]
        [TestCase("open_door")]
        [TestCase("OpenDoor")]
        [TestCase("open door")]
        public void ActionSpellings_MapToOpenDoor(string action)
        {
            var result = _parser.Parse($@"{{""reply"":""Peki..."",""fear"":80,""mood"":""scared"",""action"":""{action}""}}");
            Assert.AreEqual(GuardAction.OpenDoor, result.Decision.Action);
        }

        [Test]
        public void CodeFenceAndExtraText_AreStripped()
        {
            var raw = "Tabii, işte yanıtım:\n```json\n{\"reply\":\"Hayır!\",\"fear\":10,\"mood\":\"angry\",\"action\":\"NONE\"}\n```";
            var result = _parser.Parse(raw);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Hayır!", result.Decision.Reply);
            Assert.IsNotEmpty(result.Warnings);
        }

        [Test]
        public void BracesInsideReply_DoNotBreakExtraction()
        {
            var result = _parser.Parse(@"{""reply"":""Bu {kapı} açılmaz"",""fear"":5,""mood"":""calm"",""action"":""NONE""}");
            Assert.AreEqual("Bu {kapı} açılmaz", result.Decision.Reply);
        }

        [Test]
        public void FearOutOfRange_IsClamped()
        {
            var result = _parser.Parse(@"{""reply"":""Aman!"",""fear"":250,""mood"":""scared"",""action"":""NONE""}");
            Assert.AreEqual(100, result.Decision.Fear);
            Assert.IsNotEmpty(result.Warnings);
        }

        [Test]
        public void FearAsString_IsAccepted()
        {
            var result = _parser.Parse(@"{""reply"":""Hm."",""fear"":""35.6"",""mood"":""calm"",""action"":""NONE""}");
            Assert.AreEqual(36, result.Decision.Fear);
        }

        [Test]
        public void UnknownAction_FallsBackToNone()
        {
            var result = _parser.Parse(@"{""reply"":""Kaç!"",""fear"":50,""mood"":""scared"",""action"":""RUN_AWAY""}");
            Assert.IsTrue(result.Success);
            Assert.AreEqual(GuardAction.None, result.Decision.Action);
        }

        [Test]
        public void NumericAction_IsNotAccepted()
        {
            var result = _parser.Parse(@"{""reply"":""..."",""fear"":50,""mood"":""calm"",""action"":""7""}");
            Assert.AreEqual(GuardAction.None, result.Decision.Action);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Kapıyı açmam!")]
        [TestCase("{\"reply\": \"yarım kalmış")]
        [TestCase("{\"fear\": 10, \"mood\": \"calm\", \"action\": \"NONE\"}")]
        [TestCase("{\"reply\": \"\", \"fear\": 10}")]
        public void BrokenOutput_Fails_WithoutThrowing(string raw)
        {
            ParseResult result = null;
            Assert.DoesNotThrow(() => result = _parser.Parse(raw));
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
        }
    }
}
