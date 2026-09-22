# 工具与依赖冻结 0.1.0

| 项目 | 冻结版本 / 现场来源 | 许可证记录 |
|---|---|---|
| Unity | 2021.3.27f1c2 (3b3d9646cb47)，ProjectSettings/ProjectVersion.txt 与实际引擎日志 | Unity 已安装工具的许可；未重新分发引擎 |
| C# | 9.0，Directory.Build.props；Unity 使用此版本支持的语法 | 语言设置，无新增依赖 |
| 逻辑 TFM | netstandard2.1 | .NET targeting pack 随 SDK 安装 |
| .NET SDK / runtime | 8.0.413 / 8.0.19，osx-arm64；global.json 禁止 rollForward | .NET SDK 自带 LICENSE / ThirdPartyNotices；MIT 及其第三方 notices，未重新分发 SDK |
| JSON | com.unity.nuget.newtonsoft-json 3.2.1，内含 Newtonsoft.Json 13.0.2 | Unity 包包装 LICENSE.md 为 Unity Companion License；Newtonsoft.Json 及适配来源按包 Third Party Notices.md 为 MIT，具体声明已复制到 THIRD_PARTY_NOTICES.txt |
| Schema | Echo ContractJson `echo-draft07-subset/0.1.0` | 项目自有代码，无新增第三方 Schema 库；不是完整 Draft-07 验证器 |
| 测试 | 自有控制台/Editor smoke，未添加测试依赖；项目原有 Unity Test Framework 1.1.33 | 未重新分发 NUnit 或测试框架；本任务不依赖其 API |

Newtonsoft 版本依据本机包 package.json 的 “Currently synced to version 13.0.2”，并在两个宿主实际检查 DLL ProductVersion。Unity 保持 packages-lock.json 现有锁定 3.2.1；它当前是传递依赖，未来删去相关父包时应由共享依赖负责人提升为直接依赖，不能悄悄换库。P1-01 无需修改 manifest/lock。

新自有源代码不擅自给整个仓库追加开源许可证。本文件是依赖来源记录，不改变已有代码、素材或供应商的许可。实际资源的来源和许可仍需 P1-07 清点，本任务不声明原型美术已获可分发授权。
