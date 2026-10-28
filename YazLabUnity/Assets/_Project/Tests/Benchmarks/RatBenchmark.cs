using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DungeonEscape.Companion;
using DungeonEscape.Core;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonEscape.Tests
{
    /// <summary>
    /// Farenin tekrarlanabilir performans olcumu: her seed'de farkli muhafiz yerlesimi ve farkli
    /// baslangic noktasi. Sonuclar Docs/test_results altina CSV ve Markdown olarak yazilir.
    /// Uzun surdugu icin yalnizca adiyla calistirilir.
    /// </summary>
    [Explicit("Uzun olcum; adiyla calistirin")]
    [Category("Benchmark")]
    public class RatBenchmark
    {
        private const int Runs = 50;
        private const float TimeScale = 3f;
        private const float RunTimeout = 60f;
        // Hucre zemini (Level 01): x 6-11, y 1-4
        private static readonly Rect CellArea = new(6.4f, 1.4f, 4.2f, 2.2f);

        private struct RunRecord
        {
            public int Seed;
            public Vector2 RatStart;
            public Vector2 PlayerStart;
            public RatMissionReport Report;
            public string Guards;
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator RunFiftySeeds()
        {
            GameEvents.Reset();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;

            var rat = Object.FindAnyObjectByType<RatAgent>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var spawner = Object.FindAnyObjectByType<GuardSpawner>();
            player.SetInput(new FakePlayerInput());

            RatMissionReport? report = null;
            rat.MissionFinished += r => report = r;
            var records = new List<RunRecord>();
            Time.timeScale = TimeScale;

            try
            {
                for (int seed = 1; seed <= Runs; seed++)
                {
                    var random = new System.Random(seed * 7919);
                    player.Keys.RemoveKey(KeyIds.Cell);
                    spawner.Spawn(seed);
                    yield return null;

                    var playerStart = RandomPoint(random);
                    player.GetComponent<Rigidbody2D>().position = playerStart;
                    player.transform.position = playerStart;
                    var ratStart = RandomPoint(random);
                    rat.Navigator.Teleport(ratStart);
                    yield return null;

                    report = null;
                    rat.Command();
                    float waited = 0f;
                    while (report == null && waited < RunTimeout)
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }

                    records.Add(new RunRecord
                    {
                        Seed = seed,
                        RatStart = ratStart,
                        PlayerStart = playerStart,
                        Report = report ?? new RatMissionReport(false, waited, 0f, 0, "Test zaman asimi"),
                        Guards = string.Join(" ", spawner.Spawned.Select(g => ((Vector2)g.transform.position).ToString("F1", CultureInfo.InvariantCulture)))
                    });

                    // Basarisiz gorevden sonra fare oyuncuya donup bosta beklemeli
                    float settle = 0f;
                    while (!rat.CanAcceptCommand && !player.Keys.HasKey(KeyIds.Cell) && settle < 20f)
                    {
                        settle += Time.deltaTime;
                        yield return null;
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }

            WriteResults(records);
            float successRate = records.Count(r => r.Report.Success) / (float)records.Count;
            Debug.Log($"[Benchmark] Basari orani: {successRate:P0}");
            Assert.GreaterOrEqual(successRate, 0.9f, "Fare kosularin en az %90'inda basarili olmali");
        }

        private static Vector2 RandomPoint(System.Random random) => new(
            CellArea.x + (float)random.NextDouble() * CellArea.width,
            CellArea.y + (float)random.NextDouble() * CellArea.height);

        private static void WriteResults(List<RunRecord> records)
        {
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/test_results"));
            Directory.CreateDirectory(folder);
            var inv = CultureInfo.InvariantCulture;

            var csv = new StringBuilder("seed,success,duration_s,path_m,hides,rat_start,player_start,guards,fail_reason\n");
            foreach (var r in records)
                csv.AppendLine(string.Join(",", r.Seed, r.Report.Success ? 1 : 0,
                    r.Report.Duration.ToString("F2", inv), r.Report.TargetPathLength.ToString("F2", inv), r.Report.HideCount,
                    $"\"{r.RatStart.ToString("F1", inv)}\"", $"\"{r.PlayerStart.ToString("F1", inv)}\"",
                    $"\"{r.Guards}\"", $"\"{r.Report.FailReason}\""));
            File.WriteAllText(Path.Combine(folder, "rat_runs.csv"), csv.ToString());

            var ok = records.Where(r => r.Report.Success).ToList();
            var durations = ok.Select(r => r.Report.Duration).OrderBy(d => d).ToList();
            string Stat(IEnumerable<float> values, string format) => values.Any() ? values.Average().ToString(format, inv) : "-";

            var md = new StringBuilder();
            md.AppendLine("# Fare Gorevi Olcumu");
            md.AppendLine();
            md.AppendLine($"- Kosu sayisi: {records.Count} (seed 1-{records.Count}, her kosuda farkli muhafiz yerlesimi ve baslangic noktasi)");
            md.AppendLine($"- Basari orani: {ok.Count}/{records.Count} (%{100f * ok.Count / records.Count:F0})");
            if (durations.Count > 0)
            {
                md.AppendLine($"- Tamamlama suresi (oyun ici sn): ortalama {Stat(durations, "F2")}, medyan {durations[durations.Count / 2].ToString("F2", inv)}, en kisa {durations.First().ToString("F2", inv)}, en uzun {durations.Last().ToString("F2", inv)}");
                md.AppendLine($"- Hedef muhafiza ortalama yol uzunlugu: {Stat(ok.Select(r => r.Report.TargetPathLength), "F2")} m");
                md.AppendLine($"- Saklanma gereken kosu: {ok.Count(r => r.Report.HideCount > 0)} (toplam {ok.Sum(r => r.Report.HideCount)} saklanma)");
            }
            var failures = records.Where(r => !r.Report.Success).ToList();
            md.AppendLine($"- Basarisiz kosular: {(failures.Count == 0 ? "yok" : string.Join(", ", failures.Select(f => $"seed {f.Seed} ({f.Report.FailReason})")))}");
            File.WriteAllText(Path.Combine(folder, "rat_summary.md"), md.ToString());
        }
    }
}
