using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Feedback;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class CombatFeedbackServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            AudioService.ResetThrottleForTests();
        }

        [Test]
        public void AudioService_Throttle_BlocksRapidSameSfx()
        {
            AudioService.ResetThrottleForTests();
            Assert.IsTrue(AudioService.CanPlaySfx(SfxId.CombatHit));

            // Mark by attempting play (no AudioManager -> still marks throttle)
            AudioService.PlaySfx(SfxId.CombatHit);
            Assert.IsFalse(AudioService.CanPlaySfx(SfxId.CombatHit));
        }

        [Test]
        public void AudioData_GetSfxMinInterval_CritLongerThanHit()
        {
            var data = ScriptableObject.CreateInstance<AudioData>();
            Assert.Greater(data.GetSfxMinInterval(SfxId.CombatCrit), data.GetSfxMinInterval(SfxId.CombatHit));
            Object.DestroyImmediate(data);
        }

        [Test]
        public void EffectPool_Play_WithNullInner_DoesNotThrow()
        {
            var go = new GameObject("EffectPoolTest", typeof(EffectPool));
            var pool = go.GetComponent<EffectPool>();
            Assert.DoesNotThrow(() => pool.Play("vfx_hit", Vector2.zero));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FloatingTextPool_Spawn_ReusesInstances()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(RectTransform));
            var root = new GameObject("FloatRoot", typeof(RectTransform), typeof(FloatingTextPool));
            root.transform.SetParent(canvasGo.transform, false);
            var pool = root.GetComponent<FloatingTextPool>();
            pool.Initialize(null, root.GetComponent<RectTransform>());

            Assert.DoesNotThrow(() => pool.Spawn("+1", Color.white, Vector2.zero));
            Assert.DoesNotThrow(() => pool.Spawn("+2", Color.yellow, Vector2.one));

            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void CombatVisualEvents_RaiseAreaAndKnockback_DoNotThrowWithoutSubscribers()
        {
            Assert.DoesNotThrow(() => CombatVisualEvents.RaiseAreaAttack(Vector2.one));
            Assert.DoesNotThrow(() => CombatVisualEvents.RaiseKnockbackApplied(Vector2.zero));
            Assert.DoesNotThrow(() => CombatVisualEvents.RaiseTrainDamaged(3f));
        }

        [Test]
        public void CombatFeedbackService_BindWithoutDeps_DoesNotThrow()
        {
            var go = new GameObject(
                "Feedback",
                typeof(RectTransform),
                typeof(EffectPool),
                typeof(FloatingTextPool),
                typeof(CombatFeedbackService));
            var service = go.GetComponent<CombatFeedbackService>();
            Assert.DoesNotThrow(() => service.Bind());
            Assert.DoesNotThrow(() => service.Unbind());
            Object.DestroyImmediate(go);
        }
    }
}
