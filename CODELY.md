

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
- [2026-08-28 13:04:41] 2D 范围判定禁用 Bounds.Contains：玩家 Position 是 Vector2（z=0），触发器/交互物体 z 常为非 0（如 -7.395），triggerSize(Vector2) 转 Bounds 后 z 厚度为 0，Contains 三维判定永远 false。**Why:** DialogueTrigger 的 EnterRange 不触发排查了多轮，根因就是 Z 轴误判。**How to apply:** 所有 2D 范围判定用 Mathf.Abs 比较实现 X/Y 轴距离，明确忽略 Z。

### Project
- [2026-08-27 18:53:07] Project is a 2D side-scrolling platformer (Celeste-style) named "频率1987 / She Game", built on Tuanjie 1.10.1 (Unity 2022.3.62). Migrated from 2D-Platform-Controller-main (Unity 2021.3.15f1). Code namespaces: Myd.Platform, Myd.Platform.Core, Myd.Common. Player controller uses custom position system (no Rigidbody2D on player), so interactions (pickup, rope climb) use distance-based detection, not Unity trigger callbacks.
- [2026-08-28 00:40:34] Tuanjie TextureImporter 序列化路径是 m_SpriteSheet.m_Sprites（不是 m_SpriteSheet.sprites）。用 SerializedObject 切片 Multiple Sprite 时必须用正确路径，否则 FindProperty 返回 null。
- [2026-08-28 03:22:21] 角色属性已从 Constants 全局参数抽离为 ScriptableObject（CharacterStats）。女儿属性配置在 Assets/Resources/DaughterStats.asset，Player.Reload() 时加载并调用 ApplyToConstants() 同步到 Constants。未来创建母亲角色只需新建 MotherStats.asset 配置不同属性。角色属性包括：移动速度、跳跃、二段跳次数、冲刺、攀爬耐力、墙跳等。
- [2026-08-28 13:04:32] 对话系统位于 Assets/ProPlatformer/_Scripts/Dialogue/：DialogueData（ScriptableObject，气泡列表含头像/文本/时长/speakerId，duration<=0 则按空格推进）、DialogueManager（场景单例，气泡UI+打字机效果，bubbleSound 气泡音效先 Stop 再 PlayOneShot 防重叠；按 speakerId 解析气泡跟随目标：0=玩家、1+=场景 DialogueSpeaker）、DialogueTrigger（4种触发模式：GameStart/EnterRange/KeyInRange/Condition）、InteractableObject（靠近按空格/E交互触发对话，selfAsSpeaker 开关可让气泡跟随物品自身）、DialogueSpeaker（挂场景NPC上，编号与气泡 speakerId 对应，气泡出现在 NPC 旁且朝向玩家一侧）。注意：项目 Coroutine 类型冲突需用 UnityEngine.Coroutine；Color 无 orange 常量需 new Color(1f,0.5f,0f)；中文文本用 LegacyRuntime.ttf 可能显示方块，如乱码需换中文字体。开场对话资产在 Assets/ProPlatformer/_Arts/Dialogue/。

- [2026-08-28 12:48:58] 对话触发排查要点：EnterRange 模式判定用的是 Player.Position（角色脚底），触发范围是以触发器中心为基准的矩形（triggerSize），玩家脚底必须真正进入矩形才触发。空格键同时是跳跃键——气泡 duration=0 时需按空格推进，会导致角色跳跃脱离，排查对话是否触发时应先把 duration 改为正数。触发器放在高处（需爬绳到达）时，攀爬中位置经过范围区间理论上可触发，但若耐力不足以到达该高度则永远无法进入范围。
- [2026-08-28 13:20:49] 冲刺残影系统：TrailSnapshot 残影生成时必须用 renderer.transform.root.lossyScale（根对象完整世界缩放含翻转负号），不能用子物体 localScale——角色翻转在 PlayerRenderer 根物体上（scale.x = -Abs * Facing），Sprite 子物体 localScale 恒为正，残影会反向。拖尾参数：生成间隔 0.04s（DashState Update 循环重置 DashTrailTimer）、残影存活 0.45s（PlayerRenderer.Trail 传 duration）、残影池上限 64 个（SceneEffectManager.snapshots）。交互键已统一为 E 键/手柄 JoystickButton2（Xbox X键），空格仅用于跳跃和对话推进；InteractableObject 靠近时在物体头顶显示"按 E 交互"提示条（挂在 DialogueManager Canvas 上）。
- [2026-08-28 13:39:36] 任务系统位于 Assets/ProPlatformer/_Scripts/Quest/：QuestData（ScriptableObject，questId/title/description）、QuestUI（场景单例 QuestUI 对象，左上角纯文字面板无底板：17号暖白标题+13号浅灰描述，切换时淡出旧任务→换文本→淡入新任务 0.4s，ClearQuest() 隐藏）、QuestTrigger（玩家进范围切任务，紫色 Gizmo 框，X/Y 判定规避 Z 轴陷阱）。任务资产在 Assets/ProPlatformer/_Arts/Quest/，场景已部署 QuestTrigger_1（出生点，任务1探索老房子）和 QuestTrigger_2（(0,16)，切换任务2神秘收音机）。用户 UI 审美偏好：清新简约、低存在感、无底板纯文字、小字号柔色。

### Reference
- [2026-08-28 05:34:26] 音效资产位于 Assets/ProPlatformer/_Arts/Audio/（FootstepGravel.mp3、JumpLight.mp3、HeavyLandImpact.mp3），已绑定到 PlayerRenderer.prefab 的序列化字段（footstepClip/jumpClip/heavyLandClip），AudioSource 挂在 PlayerRenderer(Clone) 根对象（Awake 中动态添加，spatialBlend=0）。脚步声用 clip+loop+Play 循环播放（volume=0.3），停止/离地时 Pause() 保留进度、恢复跑步时 UnPause() 断点续播；跳跃声和重落地声（落差>10.7）用 PlayOneShot 一次性播放。音频代码已验证正常，若用户报告无声问题应排查 Windows 音量混合器/空间音效等系统层面原因。SceneEffectManager 的 JumpDust/DashLine 子对象曾丢失导致 MissingReferenceException，已重建并赋值。

