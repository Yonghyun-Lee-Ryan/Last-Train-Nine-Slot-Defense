using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tests.EditMode
{
    public class Unit44ResultPresentationTests
    {
        [Test]
        public void GetCauseLine_DefeatHpZero_ExplainsTrainDestroyed()
        {
            RunResult result = CreateResult(
                isVictory: false,
                RunEndReason.Defeat,
                remainingHp: 0,
                StationType.Normal);

            Assert.AreEqual("게임 오버", RunResultFormatter.GetTitle(result));
            Assert.AreEqual("객차 내구도가 0이 되었습니다.", RunResultFormatter.GetCauseLine(result));
            Assert.AreEqual(RunResultFormatter.GetCauseLine(result), RunResultFormatter.GetOverlayMessage(result));
        }

        [Test]
        public void GetCauseLine_DefeatOnBoss_ExplainsBossFailure()
        {
            RunResult result = CreateResult(
                isVictory: false,
                RunEndReason.Defeat,
                remainingHp: 0,
                StationType.Boss);

            Assert.AreEqual("보스전에서 객차가 파괴되었습니다.", RunResultFormatter.GetCauseLine(result));
        }

        [Test]
        public void GetCauseLine_Victory_ExplainsArrival()
        {
            RunResult result = CreateResult(
                isVictory: true,
                RunEndReason.Victory,
                remainingHp: 40,
                StationType.Boss);

            Assert.AreEqual("도착 성공!", RunResultFormatter.GetTitle(result));
            Assert.AreEqual("최종 역에 도착했습니다.", RunResultFormatter.GetCauseLine(result));
        }

        [Test]
        public void GetCauseLine_Abandoned_ExplainsQuit()
        {
            RunResult result = CreateResult(
                isVictory: false,
                RunEndReason.Abandoned,
                remainingHp: 50,
                StationType.Normal);

            Assert.AreEqual("회차를 중단했습니다.", RunResultFormatter.GetCauseLine(result));
        }

        [Test]
        public void BuildRewardSummary_ShowsFragmentsAndDiscoveries()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            RunResult result = new RunResult(
                runId: "u44-reward",
                lineId: "line_default",
                isVictory: false,
                endReason: RunEndReason.Defeat,
                reachedStationIndex: 2,
                completedStationCount: 1,
                enemiesKilled: 4,
                bossesKilled: 0,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 0,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 12,
                totalCoinsSpent: 0,
                passengersSummoned: 1,
                passengersSold: 0,
                abilityCardsSelected: 0,
                discoveredPassengerIds: new[] { "passenger_office_worker" },
                discoveredEnemyIds: new[] { "enemy_normal" },
                reachedStationType: StationType.Normal);

            MetaApplyResult apply = MetaProgressionService.TryApplyRunResult(meta, result);
            Assert.IsTrue(apply.Applied);
            Assert.Greater(apply.Breakdown.TotalTickets, 0);

            string summary = RunResultFormatter.BuildRewardSummary(apply);
            Assert.IsTrue(summary.Contains("승차권 조각 +" + apply.Breakdown.TotalTickets), summary);
            Assert.IsTrue(summary.Contains("신규 발견"), summary);
            Assert.IsTrue(summary.Contains("passenger_office_worker") || summary.Contains("enemy_normal"), summary);

            var lines = RunResultFormatter.CollectRevealLines(apply);
            Assert.Greater(lines.Count, 0);
            Assert.AreEqual($"승차권 조각 +{apply.Breakdown.TotalTickets}", lines[0]);
        }

        [Test]
        public void BuildRewardSummary_DuplicateRun_DoesNotGrantAgain()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            RunResult result = CreateResult(
                isVictory: true,
                RunEndReason.Victory,
                remainingHp: 20,
                StationType.Normal,
                runId: "u44-dup");

            MetaApplyResult first = MetaProgressionService.TryApplyRunResult(meta, result);
            int fragments = meta.ticketFragments;
            MetaApplyResult second = MetaProgressionService.TryApplyRunResult(meta, result);

            Assert.IsTrue(first.Applied);
            Assert.IsTrue(second.WasDuplicate);
            Assert.IsFalse(second.Applied);
            Assert.AreEqual(fragments, meta.ticketFragments);
            Assert.IsTrue(RunResultFormatter.BuildRewardSummary(second).Contains("이미 보상을 받은 회차"));
        }

        [Test]
        public void BuildRewardSummary_DedupesDiscoveryNames_AndLabelsTicketBreakdown()
        {
            var apply = new MetaApplyResult
            {
                Applied = true,
                TicketFragmentsAfter = 12,
                AccountLevelAfter = 2,
                AccountXpAfter = 40,
                Breakdown = new MetaRewardBreakdown
                {
                    StationTickets = 100,
                    KillTickets = 59,
                    BossTickets = 50
                }
            };
            apply.Breakdown.NewEnemyDiscoveries.Add("enemy_drunk_manager");
            apply.Breakdown.NewEnemyDiscoveries.Add("enemy_drunk_manager");
            apply.Breakdown.NewBossDiscoveries.Add("enemy_drunk_manager");

            string summary = RunResultFormatter.BuildRewardSummary(apply);
            Assert.IsTrue(summary.Contains("조각 내역: 역 +100 · 처치 +59 · 보스 +50"), summary);
            Assert.IsFalse(summary.Contains("역 100 / 처치"), summary);

            int first = summary.IndexOf("enemy_drunk_manager");
            Assert.GreaterOrEqual(first, 0);
            Assert.AreEqual(-1, summary.IndexOf("enemy_drunk_manager", first + 1), summary);

            var lines = RunResultFormatter.CollectRevealLines(apply);
            int discoveryCount = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("신규 발견"))
                {
                    discoveryCount++;
                }
            }

            Assert.AreEqual(1, discoveryCount);
        }

        [Test]
        public void ApplyContent_PutsStatsInScroll_WithoutHeaderOverflow()
        {
            var canvasGo = new GameObject("ResultLayoutCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                Text title = CreateResultText(canvasGo.transform, "TitleLabel", "도착 성공!");
                Text message = CreateResultText(canvasGo.transform, "MessageLabel", "최종 역에 도착했습니다.");
                Text stats = CreateResultText(
                    canvasGo.transform,
                    "StatsLabel",
                    "도달 역: 10\n완료 역: 10\n처치 수: 59\n신규 발견: a, b, c");
                stats.alignment = TextAnchor.MiddleCenter;
                stats.verticalOverflow = VerticalWrapMode.Overflow;

                ResultUiLayout.ApplyContent(title, message, stats);

                Assert.AreEqual(VerticalWrapMode.Truncate, title.verticalOverflow);
                Assert.AreEqual(VerticalWrapMode.Truncate, message.verticalOverflow);
                Assert.AreEqual(TextAnchor.UpperLeft, stats.alignment);
                Assert.GreaterOrEqual(stats.lineSpacing, 1.1f);
                RectTransform scroll = stats.transform.parent.parent as RectTransform;
                Assert.AreEqual("StatsScroll", scroll.name);
                Assert.GreaterOrEqual(scroll.offsetMin.y, 540f);
                Assert.AreEqual("Viewport", stats.transform.parent.name);
                Assert.Less(
                    message.rectTransform.anchoredPosition.y,
                    title.rectTransform.anchoredPosition.y - title.rectTransform.sizeDelta.y);
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void AchievementToast_SitsAboveResultTitleBand()
        {
            GameObject leftover = GameObject.Find("AchievementToast");
            if (leftover != null)
            {
                Object.DestroyImmediate(leftover);
            }

            var host = new GameObject("ToastHost", typeof(AchievementToastController));
            try
            {
                host.GetComponent<AchievementToastController>().ShowMessage("신규 발견: 테스트");
                Transform box = GameObject.Find("AchievementToast")?.transform.Find("Box");
                Assert.IsNotNull(box);
                RectTransform rect = box.GetComponent<RectTransform>();
                Assert.GreaterOrEqual(rect.anchorMin.y, 0.92f);
            }
            finally
            {
                GameObject toast = GameObject.Find("AchievementToast");
                if (toast != null)
                {
                    Object.DestroyImmediate(toast);
                }

                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void StationProgress_RestoreBossId_MarksBossType()
        {
            var station = new StationProgress();
            station.Initialize(1);
            station.RestoreFromSave(4, "station_boss_final", 0, 3);
            Assert.AreEqual(StationType.Boss, station.CurrentStationType);
        }

        [Test]
        public void ResultUnlockPresenter_Play_ShowsFirstRevealLine()
        {
            var go = new UnityEngine.GameObject("ResultUnlockTest");
            try
            {
                var presenter = go.AddComponent<ResultUnlockPresenter>();
                presenter.Play(new[] { "승차권 조각 +3", "신규 발견: enemy_fast" });
                var toast = go.GetComponent<AchievementToastController>();
                Assert.IsNotNull(toast);
                Assert.IsTrue(toast.IsShowing);
                Assert.AreEqual(1, presenter.RemainingCount);
                Assert.IsTrue(presenter.IsPlaying);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                GameObject leftoverToast = GameObject.Find("AchievementToast");
                if (leftoverToast != null)
                {
                    Object.DestroyImmediate(leftoverToast);
                }
            }
        }

        private static RunResult CreateResult(
            bool isVictory,
            RunEndReason reason,
            int remainingHp,
            StationType stationType,
            string runId = "u44-run")
        {
            return new RunResult(
                runId,
                "line_default",
                isVictory,
                reason,
                reachedStationIndex: 3,
                completedStationCount: 2,
                enemiesKilled: 5,
                bossesKilled: stationType == StationType.Boss ? 0 : 1,
                mergeCount: 1,
                highestPassengerStar: 2,
                remainingTrainHp: remainingHp,
                trainMaxHp: 100,
                finalCoins: 20,
                totalCoinsEarned: 40,
                totalCoinsSpent: 10,
                passengersSummoned: 2,
                passengersSold: 0,
                abilityCardsSelected: 1,
                reachedStationType: stationType);
        }

        private static Text CreateResultText(Transform parent, string name, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            return text;
        }
    }
}
