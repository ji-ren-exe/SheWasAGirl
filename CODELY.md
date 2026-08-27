

## Codely Structured Memories

### User
- [2026-08-28 00:24:26] GitHub 用户名 ji-ren-exe（显示名 纪潇航）是同一个人。Git 提交中可能出现 "ji-ren-exe <ji-ren-exe@users.noreply.github.com>" 或 "纪潇航 <80156575+ji-ren-exe@users.noreply.github.com>" 两种作者信息，均为同一用户。当前项目远程仓库为 https://github.com/ji-ren-exe/SheWasAGirl.git。

### Feedback
- [2026-08-27 18:53:03] User preference: when AI sprite generation (generate_sprite with huoshan_seedream or frontier-game-design) produces multi-view reference sheets (FRONT/RIGHT/TOP/ISO labels with blue background) instead of single sprites, prefer drawing pixel art sprites programmatically via execute_csharp_script (Texture2D + EncodeToPNG) instead of retrying AI generation — it reliably produces clean, transparent, single-view pixel sprites.
- [2026-08-28 00:02:18] 完成任务后不要自动进行测试验证（不要自动进入 Play 模式、录制 GIF/MP4、截图或运行时诊断），除非用户明确要求"自主测试"。**Why:** 用户明确提出该要求，自动测试会增加不必要的耗时与打扰。**How to apply:** 代码/场景改动后，编译确认无报错即可交付；把运行验证留给用户，或仅在用户要求时执行。
- [2026-08-28 00:07:11] 每完成一项任务后，必须在交付说明中明确列出"需要重点测试的内容"（具体测试点、操作步骤、预期结果、易出问题的边界情况）。**Why:** 用户明确要求；由于不做自动测试，需要用户自行验证，清晰的测试清单能让用户高效确认改动是否正确。**How to apply:** 与"不自动测试"规则配合——交付时给出测试清单而不是自己去跑测试。
- [2026-08-28 01:37:58] execute_csharp_script 中 System.Drawing.Image 会被 Roslyn 解析为 UnityEngine.UIElements.Image 导致编译失败。解决方法：用强制类型转换 `(System.Drawing.Image)(System.Drawing.Image.FromStream(ms))`，同理 Graphics 用 `(System.Drawing.Graphics)(System.Drawing.Graphics.FromImage(bitmap))`。**Why:** Tuanjie 脚本环境默认引用了 UnityEngine 命名空间，Image/Graphics 类型名冲突。**How to apply:** 所有 System.Drawing 类型在 Roslyn 脚本中用全限定名 + cast。
- [2026-08-28 02:48:03] 修改 [SerializeField] 字段的代码默认值后，必须同时更新 Prefab 和场景对象上的序列化值，否则运行时读取的是旧的序列化值而非代码新值。用 execute_csharp_script 直接修改 Prefab/场景组件的序列化属性并 SaveAssets/SaveScene。**Why:** Prefab 序列化值优先级高于代码默认值，只改代码不生效。**How to apply:** 改完代码默认值后，用脚本同步更新 Prefab 和场景中所有同类型对象的序列化值。
- [2026-08-28 02:59:03] 不要在 PlayerController.Update 中每帧调用 SwitchHitbox 动态切换碰撞箱。**Why:** 每帧切换会破坏 Ducking 属性的 `this.collider == this.duckHitbox` 判断逻辑，且在 CheckGround 之前改变 collider 会导致地面检测异常，角色卡住或离地。**How to apply:** collider 只在 Init 和 Ducking setter 中设置一次，不同状态的碰撞箱差异应通过 Inspector 调整而非运行时动态切换。

### Project
- [2026-08-27 18:53:07] Project is a 2D side-scrolling platformer (Celeste-style) named "频率1987 / She Game", built on Tuanjie 1.10.1 (Unity 2022.3.62). Migrated from 2D-Platform-Controller-main (Unity 2021.3.15f1). Code namespaces: Myd.Platform, Myd.Platform.Core, Myd.Common. Player controller uses custom position system (no Rigidbody2D on player), so interactions (pickup, rope climb) use distance-based detection, not Unity trigger callbacks.
- [2026-08-28 00:40:34] Tuanjie TextureImporter 序列化路径是 m_SpriteSheet.m_Sprites（不是 m_SpriteSheet.sprites）。用 SerializedObject 切片 Multiple Sprite 时必须用正确路径，否则 FindProperty 返回 null。
- [2026-08-28 03:22:21] 角色属性已从 Constants 全局参数抽离为 ScriptableObject（CharacterStats）。女儿属性配置在 Assets/Resources/DaughterStats.asset，Player.Reload() 时加载并调用 ApplyToConstants() 同步到 Constants。未来创建母亲角色只需新建 MotherStats.asset 配置不同属性。角色属性包括：移动速度、跳跃、二段跳次数、冲刺、攀爬耐力、墙跳等。

### Reference

