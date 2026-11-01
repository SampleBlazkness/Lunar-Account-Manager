# Lunar 离线账户管理器

管理 Lunar Client 的离线账户：新增、删除、切换当前账户，按玩家名查询正版 UUID，预览皮肤，一键刷新登录有效期。

Lunar Client 的账号数据保存在 `%USERPROFILE%\.lunarclient\settings\game\accounts.json`。本工具直接编辑这个文件，不修改客户端本体。

## 功能

- 账户列表：显示玩家名、UUID、有效期、当前激活账户
- 新增 / 删除账户，编辑玩家名与 UUID
- 「查询 UUID」：按玩家名请求 Mojang 接口，查不到时提示「玩家不存在」
- 皮肤预览：头像与全身图，按 UUID 拉取
- 「设为当前」：切换 `activeAccountLocalId`
- 「刷新 +12 小时」：更新 `accessTokenExpiresAt`
- 「保存到 Lunar」：把当前填写的内容写入文件，写入后自动重读并刷新列表
- 「刷新」：从磁盘重新读取
- 写入前检查 Lunar Client 是否在运行，运行中拒绝写入
- 每次写入自动备份，保留最近 5 份
- 「改动后自动保存」开关（默认关闭）

## 使用

1. 完全退出 Lunar Client（包括托盘图标）
2. 运行 `LunarAccountManager.exe`
3. 输入玩家名 → 「查询 UUID」
4. 需要的话调整有效期
5. 「保存到 Lunar」
6. 重新打开 Lunar Client

改完想还原，去 `accounts.backup-*.json` 找备份。

## 编译

只需要 .NET Framework 4.x（Windows 10/11 自带），不需要 Visual Studio 或 SDK：

```
build.bat
```

产物在 `dist\LunarAccountManager.exe`，约 23 KB。

手动编译：

```
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /optimize+ /out:dist\LunarAccountManager.exe ^
  /reference:System.dll /reference:System.Core.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Web.Extensions.dll ^
  src\*.cs
```

## 说明

- UUID 必须是正版 UUID 才能显示皮肤；没有正版档案的名字会提示「玩家不存在」
- `accessTokenExpiresAt` 建议保持在 24 小时以内，过期后客户端会要求重新登录
- 写入前必须关闭客户端，否则它退出时会把内存里的数据覆写回去

## 许可

MIT

---

# Lunar Offline Account Manager

A small WinForms tool to manage offline accounts in Lunar Client: add, remove and switch accounts, look up premium UUIDs by name, preview skins and refresh the token expiry.

Data file: `%USERPROFILE%\.lunarclient\settings\game\accounts.json`.

Build with the .NET Framework compiler that ships with Windows 10/11 — no SDK required. See `build.bat`.
