#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonEscape.ML;
using NUnit.Framework;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonEscape.Tests
{
    /// <summary>
    /// Egitilmis gizli yaklasma politikasini, gizlilik gozetmeden dogrudan kosan taban politikayla
    /// ayni arenalarda karsilastirir. Sonuc Docs/test_results/rl_evaluation.md dosyasina yazilir.
    /// </summary>
    [Explicit("Egitilmis model gerektirir; adiyla calistirin")]
    [Category("Benchmark")]
    public class RatRLEvaluation
    {
        private const string ScenePath = "Assets/_Project/Scenes/RatTraining.unity";
        private const string ModelPath = "Assets/_Project/ML/Models/RatStealth.onnx";
        private const int Episodes = 600;
        private const float TimeScale = 5f;

        private class Tally
        {
            public int Success, Caught, Timeout;
            public readonly List<int> SuccessSteps = new();
            public int Total => Success + Caught + Timeout;
        }

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator TrainedPolicy_VersusDirectRunBaseline()
        {
            var model = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
            Assert.IsNotNull(model, $"Model bulunamadi: {ModelPath}");

            var trained = new Tally();
            yield return Run(BehaviorType.InferenceOnly, model, trained);
            var baseline = new Tally();
            yield return Run(BehaviorType.HeuristicOnly, null, baseline);

            Write(trained, baseline);
            Assert.Greater(Rate(trained.Success, trained), Rate(baseline.Success, baseline),
                "Egitilmis politika tabandan daha basarili olmali");
        }

        private static IEnumerator Run(BehaviorType type, ModelAsset model, Tally tally)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            foreach (var behavior in Object.FindObjectsByType<BehaviorParameters>())
            {
                behavior.Model = model;
                behavior.InferenceDevice = InferenceDevice.Burst;
                behavior.BehaviorType = type;
            }
            yield return null;

            foreach (var agent in Object.FindObjectsByType<RatStealthAgent>())
            {
                agent.EpisodeFinished += (outcome, steps) =>
                {
                    if (tally.Total >= Episodes) return;
                    switch (outcome)
                    {
                        case StealthOutcome.Success:
                            tally.Success++;
                            tally.SuccessSteps.Add(steps);
                            break;
                        case StealthOutcome.Caught:
                            tally.Caught++;
                            break;
                        default:
                            tally.Timeout++;
                            break;
                    }
                };
            }

            Time.timeScale = TimeScale;
            try
            {
                while (tally.Total < Episodes) yield return null;
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        private static float Rate(int count, Tally tally) => tally.Total == 0 ? 0f : (float)count / tally.Total;

        private static void Write(Tally trained, Tally baseline)
        {
            string Row(string name, Tally t) =>
                $"| {name} | {t.Total} | %{100 * Rate(t.Success, t):F0} | %{100 * Rate(t.Caught, t):F0} | %{100 * Rate(t.Timeout, t):F0} | " +
                $"{(t.SuccessSteps.Count > 0 ? t.SuccessSteps.Average().ToString("F0") : "-")} |";

            var md = new StringBuilder();
            md.AppendLine("# Pekistirmeli Ogrenme Degerlendirmesi");
            md.AppendLine();
            md.AppendLine("Ayni 12 arenada, her bolumde rastgele sutun, muhafiz konumu ve baslangic noktasi.");
            md.AppendLine();
            md.AppendLine("| Politika | Bolum | Basari | Yakalanma | Zaman asimi | Basarili bolum ort. adim |");
            md.AppendLine("|---|---|---|---|---|---|");
            md.AppendLine(Row("Egitilmis PPO (RatStealth.onnx)", trained));
            md.AppendLine(Row("Taban: dogrudan hedefe kos", baseline));

            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/test_results"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "rl_evaluation.md"), md.ToString());
            Debug.Log(md.ToString());
        }
    }
}
#endif
