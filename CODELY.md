

## Codely Structured Memories

### User

### Feedback
- [2026-08-27 18:53:03] User preference: when AI sprite generation (generate_sprite with huoshan_seedream or frontier-game-design) produces multi-view reference sheets (FRONT/RIGHT/TOP/ISO labels with blue background) instead of single sprites, prefer drawing pixel art sprites programmatically via execute_csharp_script (Texture2D + EncodeToPNG) instead of retrying AI generation — it reliably produces clean, transparent, single-view pixel sprites.
- [2026-08-28 00:02:18] 完成任务后不要自动进行测试验证（不要自动进入 Play 模式、录制 GIF/MP4、截图或运行时诊断），除非用户明确要求"自主测试"。**Why:** 用户明确提出该要求，自动测试会增加不必要的耗时与打扰。**How to apply:** 代码/场景改动后，编译确认无报错即可交付；把运行验证留给用户，或仅在用户要求时执行。
- [2026-08-28 00:07:11] 每完成一项任务后，必须在交付说明中明确列出"需要重点测试的内容"（具体测试点、操作步骤、预期结果、易出问题的边界情况）。**Why:** 用户明确要求；由于不做自动测试，需要用户自行验证，清晰的测试清单能让用户高效确认改动是否正确。**How to apply:** 与"不自动测试"规则配合——交付时给出测试清单而不是自己去跑测试。

### Project
- [2026-08-27 18:53:07] Project is a 2D side-scrolling platformer (Celeste-style) named "频率1987 / She Game", built on Tuanjie 1.10.1 (Unity 2022.3.62). Migrated from 2D-Platform-Controller-main (Unity 2021.3.15f1). Code namespaces: Myd.Platform, Myd.Platform.Core, Myd.Common. Player controller uses custom position system (no Rigidbody2D on player), so interactions (pickup, rope climb) use distance-based detection, not Unity trigger callbacks.

### Reference

