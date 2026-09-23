# GF02-NET SDK 与传输交接

输入提交：`ef8c18cf64302ba8439c45cd96b8db9bb7044aeb`；分支：`codex/gf02-net`。这里只交传输代码及安装建议；Packages、锁文件和 Unity 导入编译由 GF02-INT 操作。

## 选定版本与来源

- 选 `com.openai.unity` **8.8.9**，从 OpenUPM scoped registry 安装并在 `Packages/manifest.json` 写精确版本，不使用浮动 `#upm` Git 分支。该 [发行版](https://github.com/RageAgainstThePixel/com.openai.unity/releases) 对应上游 8.8.9，包的 [package.json](https://github.com/RageAgainstThePixel/com.openai.unity/blob/upm/package.json) 声明 Unity 2021.3、MIT 与下列精确直接依赖。当前工程是 Unity 2021.3.27f1c2，版本声明相容；尚未在本工程导入编译，不能宣称实测兼容。
- 直接依赖：`com.utilities.audio` 3.0.3、`com.utilities.encoder.wav` 3.0.2、`com.utilities.rest` 5.1.1、`com.utilities.websockets` 2.0.0。上游 README 还列出 `com.utilities.async`、`com.utilities.extensions` 等传递依赖；OpenUPM 应按这些包的 manifest 解析并将**实际精确版本**写入 `packages-lock.json`。若包解析未带入 async/extensions，INT 需显式安装对应版本并记录锁文件，不用缺失依赖的 Git URL 接入。
- [SDK](https://github.com/RageAgainstThePixel/com.openai.unity) 与 [audio](https://github.com/RageAgainstThePixel/com.utilities.audio)、[encoder.wav](https://github.com/RageAgainstThePixel/com.utilities.encoder.wav)、[rest](https://github.com/RageAgainstThePixel/com.utilities.rest)、[websockets](https://github.com/RageAgainstThePixel/com.utilities.websockets)、[async](https://github.com/RageAgainstThePixel/com.utilities.async)、[extensions](https://github.com/RageAgainstThePixel/com.utilities.extensions) 仓库均标 MIT。发布包时保留各包许可证文件。工程锁文件已有 `com.unity.nuget.newtonsoft-json` 3.2.1；SDK 源码使用 Newtonsoft.Json，INT 应核对解析后的程序集引用，不再复制第二份 Newtonsoft DLL。

给 INT 的最小 manifest 建议（合入现有键，勿整份覆盖）：

```json
"scopedRegistries": [
  {
    "name": "OpenUPM",
    "url": "https://package.openupm.com",
    "scopes": ["com.openai", "com.utilities"]
  }
],
"dependencies": {
  "com.openai.unity": "8.8.9"
}
```

INT 在唯一 Editor 中解析、保存 `Packages/manifest.json` 与 `Packages/packages-lock.json`，核对上述四个直接版本与 async/extensions 的锁定版本。未安装包时传输文件引用的 `OpenAI` 类型必然无法编译；这是明确依赖，不能保留旧 HTTP 冒充 SDK。

## URI 与配置

现有 `CommanderSettings.EndpointUrl` 先把 BaseUrl 归一成完整 chat completions URL。SDK 的 [OpenAISettingsInfo](https://github.com/RageAgainstThePixel/com.openai.unity/blob/main/OpenAI/Packages/com.openai.unity/Runtime/Authentication/OpenAISettingsInfo.cs) 再拼 `/{apiVersion}/`，[ChatEndpoint](https://raw.githubusercontent.com/RageAgainstThePixel/com.openai.unity/upm/Runtime/Chat/ChatEndpoint.cs) 再拼 `chat/completions`。传输把 EndpointUrl 最后两段去掉，剩余最后一段作为 apiVersion，更早的路径作为 domain：

| 设置 BaseUrl 或完整 Endpoint | SDK domain | SDK apiVersion | 实际请求 |
| --- | --- | --- | --- |
| `https://host` 或 `https://host/v1` | `https://host` | `v1` | `https://host/v1/chat/completions` |
| `https://host/prefix/v1/chat/completions` | `https://host/prefix` | `v1` | 原完整 Endpoint |
| `https://host/proxy` | `https://host` | `proxy` | `https://host/proxy/chat/completions` |

模型 ID 原样传给 `ChatRequest`，Key 只从进程内 `CommanderSettings.TryGetApiKey` 取出并传入 `OpenAIAuthentication`；不进入资源、PlayerPrefs、日志或提交。超时以 `Stopwatch` 计时，不受 `Time.timeScale` 影响。SDK 包按非流式 `GetCompletionAsync` 调用，未添加强制 JSON mode、工具或供应商参数。

限制：SDK 的此设置类型强制插入一个非空 apiVersion，因此裸 `https://host/chat/completions` 无法被它精确表达；此输入当前被传输拒绝，`SendAsync` 返回 false。若目标供应商只提供裸根路径，需另行决定 SDK 可维护的 URI 扩展，不能静默改写请求地址。带查询参数、userinfo、fragment 的 Endpoint 也不在现有配置模型允许范围内。不同供应商的真实可用性仍须最终人工联网校验。

## 保留接口与结果

- `ICommanderTransport.Busy`、`SendAsync(IReadOnlyList<CommanderMessage>, Action<CommanderTransportResult>)`、`Cancel()` 保留；`CommanderHttpClient.GetOrCreate()` 与 `TestConnection(Action<CommanderTransportResult>)` 保留。SDK 类型只在传输文件中。无共享契约签名改动。
- Busy、空/无效消息、无模型/Key、不能映射的 Endpoint：`SendAsync` 返回 false，不发请求、也不调用回调。接受的请求最多一次终态，回调在 Unity 主线程。SDK 迟到结果在取消、超时或设置变化后丢弃。
- `Success` 的 `Content` 是 SDK 第一选择的原始文本，供 CHAT 的既有 JSON 解析器继续处理。空文本、截断或过滤完成、过长内容归 `InvalidResponse`。`Timeout`、`NetworkError`、`HttpError`、`InvalidConfiguration`、`Cancelled` 使用现有枚举；HTTP 状态若 SDK 异常公开提供则写入 `HttpStatus`。异常正文不进入 `ErrorText`。CHAT 是唯一 fallback 决策方。
- `TestConnection` 发独立的最小非流式 chat 请求，只通过调用者回调返回结果，不访问 `CommanderSession`、聊天记录或支援。共享传输忙时它返回 false，不抢占会话。

给 INT 的共享文件最小补丁：`Assets/NativeGame/GameUI/GameFlowSettingsAdapter.cs` 中把 `CancelTest = transport.Cancel` 改成 `CancelTest = transport.CancelTestConnection`。新增方法只在当前请求为测试连接时取消；这样设置面板关掉或一次 Busy 检测失败不会取消局内正在运行的聊天。其他配置与会话文件无需 NET 修改。

## 尚待 INT 与最终人工确认

当前没有打开 Unity、安装包、编译、运行自动测试或使用真实 Key。INT 需完成包解析与编译，遇到 SDK API/程序集差异时把具体编译错误回传 NET；最终人工清单验证可用服务、失败、超时、取消及配置变化，不把“已配置”当成健康连接。
