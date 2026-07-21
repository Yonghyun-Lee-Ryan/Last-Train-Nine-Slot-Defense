using LastTrain.Data;
using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class VisualDatabaseTests
    {
        [Test]
        public void TryGetPassengerVisual_WithMissingId_ReturnsFalse()
        {
            var database = ScriptableObject.CreateInstance<VisualDatabase>();
            Assert.IsFalse(database.TryGetPassengerVisual("missing", out _));
            Object.DestroyImmediate(database);
        }

        [Test]
        public void TryGetProjectileVisual_FallsBackToDefault()
        {
            var database = ScriptableObject.CreateInstance<VisualDatabase>();
            var defaultProjectile = ScriptableObject.CreateInstance<ProjectileVisualSet>();
            SerializedObjectHelper.SetId(defaultProjectile, "projectile_default");

            SerializedObjectHelper.SetProjectileArray(database, new[] { defaultProjectile });
            Assert.IsTrue(database.TryGetProjectileVisual("unknown", out ProjectileVisualSet visual));
            Assert.AreEqual("projectile_default", visual.Id);
            Object.DestroyImmediate(defaultProjectile);
            Object.DestroyImmediate(database);
        }
    }

    public class UiSpriteAnimatorTests
    {
        [Test]
        public void PlayOneShot_AdvancesFramesAndCompletes()
        {
            var go = new GameObject("AnimatorTest", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UiSpriteAnimator));
            var image = go.GetComponent<UnityEngine.UI.Image>();
            var animator = go.GetComponent<UiSpriteAnimator>();
            animator.SetImage(image);

            Sprite[] frames = CreateTestFrames(2);
            var clip = new SpriteAnimationClip(frames, framesPerSecond: 100f, loop: false);
            bool completed = false;
            animator.PlayOneShot(clip, () => completed = true);

            for (int i = 0; i < 5; i++)
            {
                animator.Tick(0.02f);
            }

            Assert.IsTrue(completed);
            Object.DestroyImmediate(go);
            DestroyFrames(frames);
        }

        [Test]
        public void SpriteAnimationClip_FirstFrame_ReturnsFirstSprite()
        {
            Sprite[] frames = CreateTestFrames(3);
            var clip = new SpriteAnimationClip(frames, 8f, true);
            Assert.AreSame(frames[0], clip.FirstFrame);
            DestroyFrames(frames);
        }

        private static Sprite[] CreateTestFrames(int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var tex = new Texture2D(8, 8);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                frames[i] = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
            }

            return frames;
        }

        private static void DestroyFrames(Sprite[] frames)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                Object.DestroyImmediate(frames[i].texture);
                Object.DestroyImmediate(frames[i]);
            }
        }
    }

    internal static class SerializedObjectHelper
    {
#if UNITY_EDITOR
        public static void SetId(Object target, string id)
        {
            var so = new UnityEditor.SerializedObject(target);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetProjectileArray(VisualDatabase database, ProjectileVisualSet[] items)
        {
            var so = new UnityEditor.SerializedObject(database);
            UnityEditor.SerializedProperty array = so.FindProperty("projectiles");
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
#else
        public static void SetId(Object target, string id) { }
        public static void SetProjectileArray(VisualDatabase database, ProjectileVisualSet[] items) { }
#endif
    }
}
