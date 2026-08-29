using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 场景级蹬墙跳开关：挂到场景任意对象（如 Game）即禁用本场景的蹬墙跳。
    /// 普通跳/二段跳不受影响。
    /// </summary>
    public class WallJumpDisabler : MonoBehaviour
    {
        private void OnEnable()
        {
            NormalState.DisableWallJumpThisScene = true;
        }

        private void OnDisable()
        {
            NormalState.DisableWallJumpThisScene = false;
        }

        private void OnDestroy()
        {
            NormalState.DisableWallJumpThisScene = false;
        }
    }
}
