using System;
using System.Reflection;
using LastTrain.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Unit 31: Audio Mixer(Master/BGM/SFX)와 AudioData 애셋 생성.
    /// AudioMixerController는 내부 API이므로 리플렉션으로만 접근한다.
    /// </summary>
    public static class Unit31AudioAssetsBuilder
    {
        private const string MixerFolder = "Assets/Resources/Audio";
        private const string MixerPath = "Assets/Resources/Audio/GameAudioMixer.mixer";
        private const string AudioDataPath = "Assets/Resources/Audio/AudioData.asset";

        [MenuItem("Tools/막차 생존/개발 단위 31 오디오 믹서·AudioData 생성")]
        public static void Build()
        {
            EnsureFolders();
            AudioMixer mixer = LoadOrCreateMixer();
            EnsureChildGroups(mixer);
            AudioData data = LoadOrCreateAudioData(mixer);
            EditorUtility.SetDirty(data);
            if (mixer != null)
            {
                EditorUtility.SetDirty(mixer);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "완료",
                "GameAudioMixer(Master/BGM/SFX)와 AudioData를 생성·연결했습니다.\n" +
                "Mixer Inspector에서 MasterVolume / BgmVolume / SfxVolume을 Expose 하세요.",
                "확인");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(MixerFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");
            }
        }

        private static AudioMixer LoadOrCreateMixer()
        {
            AudioMixer existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null)
            {
                return existing;
            }

            Type controllerType = FindType("UnityEditor.Audio.AudioMixerController");
            if (controllerType == null)
            {
                Debug.LogError("[Unit31] AudioMixerController 타입을 찾지 못했습니다.");
                return null;
            }

            MethodInfo create = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.Static);
            if (create == null)
            {
                Debug.LogError("[Unit31] CreateMixerControllerAtPath를 찾지 못했습니다.");
                return null;
            }

            object created = create.Invoke(null, new object[] { MixerPath });
            return created as AudioMixer;
        }

        private static void EnsureChildGroups(AudioMixer mixer)
        {
            if (mixer == null)
            {
                return;
            }

            Type controllerType = FindType("UnityEditor.Audio.AudioMixerController");
            if (controllerType == null || !controllerType.IsInstanceOfType(mixer))
            {
                return;
            }

            PropertyInfo masterProp = controllerType.GetProperty(
                "masterGroup",
                BindingFlags.Public | BindingFlags.Instance);
            object master = masterProp?.GetValue(mixer);
            if (master == null)
            {
                return;
            }

            if (!HasChildNamed(master, "BGM"))
            {
                CreateGroup(mixer, controllerType, master, "BGM");
            }

            if (!HasChildNamed(master, "SFX"))
            {
                CreateGroup(mixer, controllerType, master, "SFX");
            }
        }

        private static void CreateGroup(AudioMixer mixer, Type controllerType, object parent, string name)
        {
            MethodInfo createGroup = controllerType.GetMethod(
                "CreateNewGroup",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(UnityEngine.Object[]) },
                null);
            if (createGroup == null)
            {
                return;
            }

            createGroup.Invoke(mixer, new object[] { name, new UnityEngine.Object[] { parent as UnityEngine.Object } });
        }

        private static bool HasChildNamed(object parentGroup, string name)
        {
            PropertyInfo childrenProp = parentGroup.GetType().GetProperty(
                "children",
                BindingFlags.Public | BindingFlags.Instance);
            if (childrenProp == null)
            {
                return false;
            }

            if (childrenProp.GetValue(parentGroup) is not Array children)
            {
                return false;
            }

            for (int i = 0; i < children.Length; i++)
            {
                object child = children.GetValue(i);
                if (child == null)
                {
                    continue;
                }

                PropertyInfo nameProp = child.GetType().GetProperty("name");
                string childName = nameProp?.GetValue(child) as string;
                if (childName == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static AudioData LoadOrCreateAudioData(AudioMixer mixer)
        {
            AudioData data = AssetDatabase.LoadAssetAtPath<AudioData>(AudioDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AudioData>();
                AssetDatabase.CreateAsset(data, AudioDataPath);
            }

            var so = new SerializedObject(data);
            so.FindProperty("mixer").objectReferenceValue = mixer;
            so.FindProperty("masterVolumeParam").stringValue = "MasterVolume";
            so.FindProperty("bgmVolumeParam").stringValue = "BgmVolume";
            so.FindProperty("sfxVolumeParam").stringValue = "SfxVolume";
            so.FindProperty("defaultSfxMinInterval").floatValue = 0f;
            so.FindProperty("combatHitMinInterval").floatValue = 0.04f;
            so.FindProperty("combatCritMinInterval").floatValue = 0.08f;
            so.FindProperty("enemyDeathMinInterval").floatValue = 0.06f;
            so.FindProperty("coinMinInterval").floatValue = 0.05f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static Type FindType(string fullName)
        {
            Type direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
