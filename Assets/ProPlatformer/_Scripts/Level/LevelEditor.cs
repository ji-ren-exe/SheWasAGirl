#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Myd.Platform.EditorTools
{
    /// <summary>
    /// Level 出生点可视化编辑：Scene 视图中拖动绿点把手直接改 StartPosition（支持 Undo/Shift 对齐）。
    /// </summary>
    [CustomEditor(typeof(Level))]
    public class LevelEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var level = (Level)target;

            // 只在选中时显示可拖动把手
            Vector3 current = level.StartPosition;
            float size = 0.45f * HandleUtility.GetHandleSize(current);

            Handles.color = new Color(0.2f, 1f, 0.3f, 0.95f);
            EditorGUI.BeginChangeCheck();
            var fmh_25_17_639235661509083309 = Quaternion.identity; Vector3 moved = Handles.FreeMoveHandle(
                current,
                size,
                Vector3.zero,
                Handles.DotHandleCap);
            // 标签提示
            Handles.Label(moved + Vector3.up * 0.7f, "出生点\n(拖动绿点)");

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(level, "移动出生点");
                level.StartPosition = moved;
                EditorUtility.SetDirty(level);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var level = (Level)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"出生点: ({level.StartPosition.x:0.##}, {level.StartPosition.y:0.##})\n" +
                "Scene 视图中拖动绿色圆点把手可移动出生点（箭头底横线 = 角色脚底）。",
                MessageType.Info);
        }
    }
}
#endif
