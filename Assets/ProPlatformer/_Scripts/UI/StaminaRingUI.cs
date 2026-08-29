using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 环状耐力条：绿色圆环跟随角色，抓墙消耗时缩短，耐力满时隐藏
    /// </summary>
    public class StaminaRingUI : MonoBehaviour
    {
        //环直径为原先 0.75 半径版本的 1/4 => 半径 0.1875；线宽约为半径的 40%，视觉更粗
        [SerializeField] private int segmentCount = 48;
        [SerializeField] private float radius = 0.1875f;
        [SerializeField] private float lineWidth = 0.075f;
        //置于角色后上方：x 为背向偏移（按朝向取反），y 为向上偏移
        [SerializeField] private float backOffset = 0.45f;
        [SerializeField] private float upOffset = 0.75f;
        [SerializeField] private Color fullColor = new Color(0.2f, 0.95f, 0.35f, 1f);
        [SerializeField] private Color lowColor = new Color(0.95f, 0.75f, 0.15f, 1f);

        private LineRenderer line;

        private void Awake()
        {
            line = gameObject.GetComponent<LineRenderer>();
            if (line == null)
                line = gameObject.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.loop = false;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.widthMultiplier = lineWidth;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sortingLayerName = "EffectForgeground";
            line.sortingOrder = 100;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.positionCount = 0;
        }

        private void LateUpdate()
        {
            var player = Player.Current;
            if (player == null)
            {
                line.positionCount = 0;
                return;
            }

            float percent = player.StaminaPercent;
            // 独立 Transform（非角色子物体）：直接用世界坐标跟随，避免父级翻转/零缩放影响
            // 朝右时环偏左（背向），朝左时偏右
            float backDir = -(int)player.Facing;
            transform.position = player.Position + new Vector2(backDir * backOffset, upOffset);
            // 中和任何继承缩放，保持环原始大小
            transform.localScale = Vector3.one;

            // 耐力满时不显示，避免遮挡角色
            if (percent >= 0.999f)
            {
                line.positionCount = 0;
                return;
            }

            //每帧同步线宽，便于在 Inspector 中实时调整粗细
            line.widthMultiplier = lineWidth;

            int points = Mathf.Max(2, Mathf.CeilToInt(segmentCount * percent) + 1);
            line.positionCount = points;

            // 从正上方开始顺时针绘制剩余耐力
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / segmentCount;
                float angle = Mathf.PI * 0.5f - t * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            Color c = Color.Lerp(lowColor, fullColor, percent);
            line.startColor = c;
            line.endColor = c;
        }
    }
}
