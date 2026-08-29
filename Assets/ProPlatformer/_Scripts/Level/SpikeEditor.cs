#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Myd.Platform.EditorTools
{
    /// <summary>
    /// 尖刺编辑器：Inspector 设置数量/间距，一键生成一排尖刺（本对象为排首）
    /// </summary>
    [CustomEditor(typeof(Spike))]
    public class SpikeEditor : UnityEditor.Editor
    {
        // 生成参数（编辑器会话内记忆）
        private static int count = 5;
        private static float spacing = 1.28f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var spike = (Spike)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("生成一排尖刺", EditorStyles.boldLabel);

            count = EditorGUILayout.IntField("数量", count);
            spacing = EditorGUILayout.FloatField("间距（世界单位）", spacing);
            count = Mathf.Max(1, count);
            spacing = Mathf.Max(0.1f, spacing);

            if (GUILayout.Button("向右生成一排"))
            {
                GenerateRow(spike, Vector2.right);
            }
            if (GUILayout.Button("向左生成一排"))
            {
                GenerateRow(spike, Vector2.left);
            }

            EditorGUILayout.HelpBox(
                "以当前选中的尖刺为起点，按数量和间距复制出一排新尖刺。\n" +
                "间距 1.28 = 一格瓦片宽度（推荐）。",
                MessageType.Info);
        }

        private void GenerateRow(Spike origin, Vector2 dir)
        {
            // 找到排首对象的 Prefab 源（保持贴图与参数一致）
            Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(origin.gameObject);
            bool isPrefabInstance = prefabSource != null;

            Undo.IncrementCurrentGroup();
            for (int i = 1; i < count; i++) // i=0 是本物体自身
            {
                Vector3 pos = origin.transform.position + (Vector3)(dir * spacing * i);

                GameObject go;
                if (isPrefabInstance)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, origin.gameObject.scene);
                }
                else
                {
                    go = Instantiate(origin.gameObject, origin.transform.parent);
                    // 去掉复制体上可能的场景唯一名后缀
                    go.name = origin.gameObject.name;
                }

                go.transform.position = pos;
                go.transform.SetParent(origin.transform.parent, true);
                Undo.RegisterCreatedObjectUndo(go, "生成尖刺");
            }

            // 选中新排的最后一个，方便继续接排
            Selection.activeGameObject = origin.transform.parent != null
                ? origin.transform.parent.GetChild(origin.transform.parent.childCount - 1).gameObject
                : origin.gameObject;
        }
    }
}
#endif
