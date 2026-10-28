using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonEscape.Dialogue;
using NUnit.Framework;

namespace DungeonEscape.Tests
{
    public class DialogueSessionTests
    {
        /// <summary>Sirayla onceden verilen yanitlari donduren istemci; gonderilen mesajlari kaydeder.</summary>
        private class ScriptedClient : ILLMClient
        {
            private readonly Queue<LLMResult> _responses = new();
            public readonly List<IReadOnlyList<ChatMessage>> Requests = new();

            public ScriptedClient Then(string content)
            {
                _responses.Enqueue(LLMResult.Ok(content, 0.1f));
                return this;
            }

            public ScriptedClient ThenFail(LLMErrorKind kind)
            {
                _responses.Enqueue(LLMResult.Fail(kind, $"{kind} hatasi"));
                return this;
            }

            public Task<HealthStatus> CheckHealthAsync(CancellationToken cancellation) =>
                Task.FromResult(new HealthStatus(true, true, "ok"));

            public Task<LLMResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string schema, CancellationToken cancellation)
            {
                Requests.Add(new List<ChatMessage>(messages));
                return Task.FromResult(_responses.Dequeue());
            }
        }

        private static string Json(int fear, string action, string reply = "Hm.") =>
            $@"{{""reply"":""{reply}"",""fear"":{fear},""mood"":""nervous"",""action"":""{action}""}}";

        private static TurnResult Send(DialogueSession session, string text) =>
            session.SendAsync(text, CancellationToken.None).GetAwaiter().GetResult();

        [Test]
        public void ValidTurn_UpdatesFear_AndHistory()
        {
            var client = new ScriptedClient().Then(Json(40, "NONE")).Then(Json(40, "NONE"));
            var session = new DialogueSession(client, new DecisionParser());

            var turn = Send(session, "Kapıyı aç!");
            Assert.IsTrue(turn.Success);
            Assert.AreEqual(40, turn.FearLevel);

            Send(session, "Hâlâ bekliyorum.");
            var second = client.Requests[1];
            Assert.AreEqual("system", second[0].role);
            StringAssert.Contains("Şu anki korkun: 40", second[0].content);
            Assert.IsTrue(second.Count > 3, "Onceki tur gecmise eklenmeli");
        }

        [Test]
        public void BrokenOutput_IsRepairedOnce()
        {
            var client = new ScriptedClient().Then("Kapıyı açmam, defol!").Then(Json(20, "NONE"));
            var session = new DialogueSession(client, new DecisionParser());

            var turn = Send(session, "Aç şunu");
            Assert.IsTrue(turn.Success);
            Assert.IsTrue(turn.Repaired);
            Assert.AreEqual(2, client.Requests.Count);
        }

        [Test]
        public void BrokenTwice_FailsTurn_AndLeavesStateUntouched()
        {
            var client = new ScriptedClient().Then("bozuk").Then("yine bozuk");
            var session = new DialogueSession(client, new DecisionParser());
            int before = session.Fear.Level;

            var turn = Send(session, "Aç şunu");
            Assert.IsFalse(turn.Success);
            Assert.IsNotNull(turn.UserFacingError);
            Assert.AreEqual(before, session.Fear.Level);
            Assert.AreEqual(DialogueOutcome.Continue, turn.Outcome);
        }

        [TestCase(LLMErrorKind.ServerUnreachable)]
        [TestCase(LLMErrorKind.Timeout)]
        [TestCase(LLMErrorKind.ModelMissing)]
        [TestCase(LLMErrorKind.EmptyResponse)]
        public void ClientErrors_ProduceUserMessage_NoStateChange(LLMErrorKind kind)
        {
            var session = new DialogueSession(new ScriptedClient().ThenFail(kind), new DecisionParser());
            var turn = Send(session, "Merhaba");

            Assert.IsFalse(turn.Success);
            Assert.AreEqual(kind, turn.ErrorKind);
            Assert.IsNotEmpty(turn.UserFacingError);
            Assert.AreEqual(DialogueOutcome.Continue, turn.Outcome);
        }

        [Test]
        public void SingleThreat_CannotOpenDoor_ButSustainedIntimidationCan()
        {
            // Model ilk tehditte hemen teslim olmak istese de korku tur basina en fazla 35 artar
            var client = new ScriptedClient()
                .Then(Json(90, "OPEN_DOOR"))
                .Then(Json(90, "OPEN_DOOR"));
            var session = new DialogueSession(client, new DecisionParser());

            var first = Send(session, "Aç yoksa ölürsün!");
            Assert.AreEqual(50, first.FearLevel);
            Assert.AreEqual(DialogueOutcome.Continue, first.Outcome);
            Assert.IsNotNull(first.RuleNote);

            var second = Send(session, "Arkadaşlarının hâlini gördün.");
            Assert.AreEqual(85, second.FearLevel);
            Assert.AreEqual(DialogueOutcome.DoorOpens, second.Outcome);
        }

        [Test]
        public void EmptyInput_IsRejectedWithoutCallingModel()
        {
            var client = new ScriptedClient();
            var session = new DialogueSession(client, new DecisionParser());
            var turn = Send(session, "   ");
            Assert.IsFalse(turn.Success);
            Assert.IsEmpty(client.Requests);
        }
    }
}
