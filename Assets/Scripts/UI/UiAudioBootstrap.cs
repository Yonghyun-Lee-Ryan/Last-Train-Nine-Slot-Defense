using LastTrain.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>씬의 Button 클릭에 UI SFX를 붙인다. BGM은 AudioManager가 담당한다.</summary>
    public static class UiAudioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            for (int i = 0; i < buttons.Length; i++)
            {
                AttachClickSfx(buttons[i]);
            }
        }

        public static void AttachClickSfx(Button button)
        {
            if (button == null || button.GetComponent<UiClickSfx>() != null)
            {
                return;
            }

            button.gameObject.AddComponent<UiClickSfx>();
        }
    }

    public sealed class UiClickSfx : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            GameAudio.PlaySfx(SfxId.UiClick);
        }
    }
}
