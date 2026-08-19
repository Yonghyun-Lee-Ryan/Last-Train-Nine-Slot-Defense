using LastTrain.Endless;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 51: Endless 마일스톤 10단 트랙.</summary>
    public static class Unit51EndlessMilestoneAssetsBuilder
    {
        private const string DataPath = "Assets/Data/Endless/EndlessMilestoneTrack.asset";
        private const string ResourcesPath = "Assets/Resources/Endless/EndlessMilestoneTrack.asset";

        [MenuItem("Tools/막차 생존/개발 단위 51 Endless 마일스톤 생성")]
        public static void BuildFromMenu()
        {
            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit51EndlessMilestoneAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit51EndlessMilestoneAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit51EndlessMilestoneAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal(bool showDialog)
        {
            EnsureFolder("Assets/Data", "Endless");
            EnsureFolder("Assets/Resources", "Endless");

            EndlessMilestoneTrack track = LoadOrCreate(DataPath);
            WriteSteps(track);
            CopyToResources(track);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", "Endless 마일스톤 10단 생성", "확인");
            }
        }

        private static void WriteSteps(EndlessMilestoneTrack track)
        {
            var so = new SerializedObject(track);
            SerializedProperty steps = so.FindProperty("steps");
            (string id, int station, int score, int tickets)[] table =
            {
                ("ms_station_5", 5, 0, 20),
                ("ms_station_10", 10, 0, 30),
                ("ms_station_15", 15, 0, 40),
                ("ms_station_20", 20, 0, 50),
                ("ms_station_30", 30, 0, 80),
                ("ms_score_500", 0, 500, 20),
                ("ms_score_1000", 0, 1000, 40),
                ("ms_score_2000", 0, 2000, 60),
                ("ms_score_4000", 0, 4000, 80),
                ("ms_score_8000", 0, 8000, 100),
            };

            steps.arraySize = table.Length;
            for (int i = 0; i < table.Length; i++)
            {
                SerializedProperty item = steps.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("id").stringValue = table[i].id;
                item.FindPropertyRelative("requiredStation").intValue = table[i].station;
                item.FindPropertyRelative("requiredScore").intValue = table[i].score;
                item.FindPropertyRelative("ticketFragments").intValue = table[i].tickets;
                item.FindPropertyRelative("accountXp").intValue = 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(track);
        }

        private static EndlessMilestoneTrack LoadOrCreate(string path)
        {
            EndlessMilestoneTrack existing = AssetDatabase.LoadAssetAtPath<EndlessMilestoneTrack>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<EndlessMilestoneTrack>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void CopyToResources(EndlessMilestoneTrack source)
        {
            AssetDatabase.CopyAsset(DataPath, ResourcesPath);
            EndlessMilestoneTrack copy = AssetDatabase.LoadAssetAtPath<EndlessMilestoneTrack>(ResourcesPath);
            if (copy != null)
            {
                WriteSteps(copy);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
