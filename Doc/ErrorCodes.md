# TatehamaATS エラーコード一覧

## エラーコードの読み方

エラーコードは **系統番号（1桁）+ 種別コード（2桁16進数）** で構成されます。

```
例: 7EE
    │ └─ 種別コード（EE = 通信部カウンタ異常）
    └─── 系統番号（7 = 通信部）
```

実装上は `ATSCommonException.Place.ToString() + ToCode()` で生成されます。

---

## 系統コード一覧

| 系統番号 | 系統名 | 主な担当クラス |
|---------|--------|-------------|
| 3 | ATS本体系（表示・記録・スピーカー） | `LEDWindow`, `InspectionRecord`, `ConsoleSpeaker` |
| 5 | 継電部（TrainCrew WebSocket 連携） | `Relay` |
| 7 | 通信部（地上サーバー SignalR 連携） | `Network` |
| 8 | LED制御部（詳細制御） | `ControlLED` |

---

## リセット条件の説明

| リセット条件 | 意味 |
|------------|------|
| `ExceptionReset` | 故障復旧のみで復旧可能 |
| `RetsubanReset` | 列番等の再設定が必要 |
| `NetworkReset` | 信号接続のリセットが必要 |
| `StopDetection` | 停車検知が必要 |
| `StopDetection_RelayReset` | 停車検知 + TC接続リセットが必要 |
| `StopDetection_NetworkReset` | 停車検知 + 信号接続リセットが必要 |
| `StopDetection_MasconEB` | 停車検知 + マスコン非常が必要 |
| `StopDetection_MasconEB_ATSReset` | 停車検知 + マスコン非常 + ATS復帰が必要 |
| `PowerReset` | 電源再投入が必要 |

---

## エラーコード詳細

### 3系（ATS本体系）

#### `3B0` — LED制御部未定義故障 / LED表示番号異常

**例外クラス:** `LEDControlException` / `LEDDisplayNumberAbnormal`

**発生条件:**
- `LEDWindow.DisplayImage()` 内で GDI+ による画像合成・PictureBox への設定処理中に例外が発生した場合
- `GetPictureBoxByIndex()` に L1/L2/L3 に対応する 1〜3 以外のインデックスが渡された場合
- `GetImageByNumber()` で LED スプライトシート（8列×32行）の範囲外番号を指定した場合
- `GetImageByCodeNumber()` で故障コード表示用スプライトシート（4列×4行）の範囲外番号を指定した場合
- `InspectionRecord.StartDisplayUpdateLoop()` の 20ms 定周期ループ内で予期しない例外が発生した場合

**リセット条件:** `StopDetection`

---

#### `39F` — C# 系異常

**例外クラス:** `CsharpException`

**発生条件:**
- `InspectionRecord.AddException()` が CLR 例外を受け取りコード `39F` として故障辞書へ登録する際
- `ConsoleSpeaker` のコンストラクタで `AudioManager.AddAudio()` が失敗した場合（音声ファイル欠落・DirectX/オーディオドライバの初期化エラーなど）

**リセット条件:** `StopDetection_MasconEB`

---

#### `395` — 継電部・検査記録部伝送異常

**例外クラス:** `RelayIOConnectionException`

**発生条件:**
- `InspectionRecord` の監視ループで、`RelayUpdatedTime`（Relay からの最終更新時刻）から現在時刻（JST）までの経過が **2秒以上** かつ **ゲームプレイ中**（`GameScreen.MainGame` または `MainGame_Pause`）を検知した場合
- `Relay.ReceiveMessages()` が一定時間 `RelayUpdatedTime` を更新しない状態に相当

**リセット条件:** `StopDetection`

---

#### `397` — 通信部・検査記録部伝送異常

**例外クラス:** `NetworkIOConnectionException`

**発生条件:**
- `InspectionRecord` の監視ループで、`NetworkUpdatedTime`（Network からの最終更新時刻）から現在時刻（JST）までの経過が **5秒以上** を検知した場合
- ゲームプレイ中かどうかを問わず発生する（`395` と異なる点）

**リセット条件:** `StopDetection`

---

### 5系（継電部・TrainCrew WebSocket）

#### `5CF` — 継電部未定義故障

**例外クラス:** `RelayException`

**発生条件:**
- `Relay.Command` プロパティの setter で `null` が渡された場合、または `IsValidCommand()` による許可リスト検証（`"DataRequest"`, `"SetEmergencyLight"`, `"SetSignalPhase"`, `"SetRoute"`, `"DeleteRoute"` 等）に失敗した場合
- `Relay.Request` プロパティの setter で `null` が渡された場合、または `IsValidRequest()` によるコマンド種別ごとの引数形式検証（引数数・値の許可リスト）に失敗した場合
- `SendSingleCommand()` でコマンド・リクエストの検証に失敗した場合

**リセット条件:** `PowerReset`

---

#### `5CC` — 継電部接続異常（初回）

**例外クラス:** `RelayFirstConnectException`

**発生条件:**
- `Relay.SendMessages()` で WebSocket 接続後、最初の `DataRequest` コマンドの JSON をポート 50300 の TrainCrew ローカルサーバーへ送信する際に例外が発生した場合
- 送信は `ClientWebSocket.SendAsync()` による UTF-8 エンコードされた JSON テキストフレームで行われる

**リセット条件:** `StopDetection_RelayReset`

---

#### `5CD` — 継電部接続異常

**例外クラス:** `RelayConnectException`

**発生条件:**
- `TryConnectWebSocket()` の無限リトライループ中に `ClientWebSocket` の接続で一般例外が発生した場合（1秒待機後リトライ）
- `SendMessages(string, string[])` でコマンド種別（信号現示設定・進路設定など）の JSON 送信時に例外が発生した場合
- `SendSingleCommand()` 内で `SendMessages()` からの例外が伝播した場合

**リセット条件:** `StopDetection_RelayReset`

---

#### `5CA` — ほか情報異常

**例外クラス:** `RelayOtherInfoAbnormal`

**発生条件:**
- `ReceiveMessages()` で WebSocket 受信データを UTF-8 デコードした結果、`HasInvalidChars()`（制御文字または U+FFFD 混入チェック）が **20回連続**で true を返した場合（ノイズ・破損パケット）
- 受信 JSON の `type` フィールドが `"TrainCrewStateData"` / `"RecvBeaconStateData"` / `"APIMessage"` のいずれにも該当しない未知のタイプだった場合

**リセット条件:** `ExceptionReset`

---

#### `5C9` — 車両情報異常

**例外クラス:** `RelayCarInfoAbnormal`

**発生条件:**
- `ReceiveMessages()` で受信した JSON を `JsonConvert.DeserializeObject<TrainCrewStateData>()` でデシリアライズした結果が `null` になった場合（速度・制動・ゲーム画面状態などの列車情報が取得不能）

**リセット条件:** `StopDetection`

---

#### `5CH` — TrainCrew状態取得異常

**例外クラス:** `RelayGetStateException`

**発生条件:**
- `SendDataUpdate()`（Network.cs）で `TrainCrewInput.GetTrainState()` 呼び出し中に例外が発生した場合

**リセット条件:** `StopDetection_RelayReset`

---

#### `5C2` — 地上子情報異常

**例外クラス:** `TransponderInfoAbnormal`

**発生条件:**
- `ReceiveMessages()` で受信した JSON を `JsonConvert.DeserializeObject<RecvBeaconStateData>()` でデシリアライズした結果が `null` になった場合（列車無線トランスポンダのビーコンデータが取得不能）

**リセット条件:** `StopDetection_MasconEB_ATSReset`

---

### 7系（通信部・地上サーバー SignalR）

#### `7EE` — 通信部カウンタ異常

**例外クラス:** `NetworkCountaException`

**発生条件:**
- `StartUpdateLoop()` で `Task` として実行中の `UpdateLoop()` が例外で終了し、外側のループが再起動を試みた場合
- `UpdateLoop()` の 100ms 定周期ループ内で `SendData_to_Server()` が例外で終了した場合（接続喪失などにより SignalR `InvokeAsync` が失敗し UpdateLoop が終了する）
- `SendDataUpdate()` で、送信データオブジェクトへのフィールド代入（TcData の null 参照、文字列の無効文字チェックなど）中に例外が発生した場合（`TrainCrewInput.GetTrainState()` 呼び出しに起因する例外は `5CH` として通報される）

**リセット条件:** `StopDetection_NetworkReset`

---

#### `7EC` — 地上接続失敗（未接続）

**例外クラス:** `NetworkNonConnectException`

**発生条件:**
- `UpdateLoop()` の各イテレーションで `connected == false`（SignalR の `HubConnection` が接続確立前または切断後の状態）を検知した場合
- 接続確立まで 100ms ごとに繰り返し通知される（ループは継続）

**リセット条件:** `NetworkReset`

---

#### `7ED` — 地上接続失敗

**例外クラス:** `NetworkConnectException`

**発生条件（複数）:**
- `Connect()` 開始直後に無条件で発火（接続試行開始の通知として使用）
- `Connect()` で `HubConnection.StartAsync()` が `InvalidOperationException` を throw した場合（接続オブジェクトが Disposed 状態や不正な状態）→ 接続オブジェクトを破棄して再初期化
- `Connect()` で `StartAsync()` がその他の例外（`WebSocketException`, `HttpRequestException` など）を throw した場合
- `SendData_to_Server()` で `InvokeAsync` 実行中に `WebSocketError.ConnectionClosedPrematurely` を捕捉後、`TryReconnectOnceAsync()` による再接続も失敗した場合
- `SendData_to_Server()` で `InvokeAsync` が `InvalidOperationException` を throw した場合（`HubConnection` が Disposed 状態）
- `DriverGetsOff()` で上記と同様のパターン

**リセット条件:** `NetworkReset`

---

#### `7E4` — データ異常

**例外クラス:** `NetworkDataException`

**発生条件:**
- `SendData_to_Server()` で `HubConnection.InvokeAsync<DataFromServer>()` が WebSocket 切断・HTTP エラー・JSON 以外の例外（`TaskCanceledException`, `HttpRequestException`, `JsonSerializationException` など）を throw した場合
- `DriverGetsOff()` で同様の例外が発生した場合

**リセット条件:** `StopDetection`

---

#### `7E8` — 地上認証失敗

**例外クラス:** `NetworkAuthorizeException`

**発生条件（複数）:**
- `InteractiveAuthenticateAsync()` で OpenIddict の `AuthenticateInteractivelyAsync()` が 90秒の `CancellationToken` タイムアウトで `OperationCanceledException` を throw した場合（ユーザーがブラウザで認証を完了しない）
- 同メソッドで `ProtocolException.Error == ServerError`（OpenIddict サーバー側の内部エラー）が返された場合
- 同メソッドでその他の例外（`HttpRequestException`, `JsonException` など）が発生した場合
- `TryReconnectOnceAsync()` で、アクセストークンの有効期限切れかつリフレッシュトークンの更新も `InvalidToken` / `InvalidGrant` / `ExpiredToken` で失敗した場合

**リセット条件:** `StopDetection_NetworkReset`

---

#### `787` — 地上認証拒否

**例外クラス:** `NetworkAccessDenied`

**発生条件:**
- `InteractiveAuthenticateAsync()` で `ProtocolException.Error == UnauthorizedClient` が返された場合（サーバーにユーザーが未登録、または「入鋏」ロールが未付与）
- `Connect()` で `HubConnection.StartAsync()` が `HttpRequestException` かつ `StatusCode == 403 Forbidden` を throw した場合（SignalR ハブへのアクセス権限なし）

**リセット条件:** `StopDetection_NetworkReset`

---

### 8系（LED制御部）

#### `883` — LED制御部初期化失敗

**例外クラス:** `LEDControlInitialzingFailure`

**発生条件:**
- `ControlLED` のコンストラクタで `LEDWindow()` の WinForms Form 生成または GDI+ リソースの初期化が失敗した場合

**リセット条件:** `PowerReset`

---

#### `8B0` — LED制御部未定義故障

**例外クラス:** `LEDControlException`

**発生条件:**
- `ControlLED.StartDisplayUpdateLoop()` の 20ms 定周期ループ内で `UpdateDisplay()` が予期しない一般例外を throw した場合

**リセット条件:** `StopDetection`

---

#### `8B2` — LED表示内容異常

**例外クラス:** `LEDDisplayStringAbnormal`

**発生条件:**
- `ControlLED.ConvertToLEDNumber()` で、入力文字列が16進数・10進数のいずれでもなく、かつ定義済み文字列マッピング（`"普通"`, `"急行"`, `"無表示"` 等 40種以上）のいずれにも合致しない場合

**リセット条件:** `StopDetection`

---

## 付録: 定義のみのエラーコード（throw 箇所未確認）

以下の例外クラスは実装されているが、現時点でコード中に `throw` 箇所が確認されていないものです。

| エラーコード | 例外クラス | 説明 | リセット条件 |
|------------|----------|------|------------|
| `*91` | `CarAbnormal` | 編成両数異常 | `PowerReset` |
| `*9C` | `DBTrackDataChengeAbnormal` | 車上DB閉塞データ編集異常 | `StopDetection_MasconEB` |
| `*BE` | `LEDControlCountaException` | LED制御部カウンタ異常 | `PowerReset` |
| `*FE` | `InspectionRecordCountaException` | 検査記録部カウンタ異常 | `PowerReset` |
| `*FE` | `InspectionRecordException` | 検査記録部未定義故障 | `PowerReset` |
| `*CE` | `RelayCountaException` | 継電部カウンタ異常 | `PowerReset` |
| `*84` | `RelayInitialzingFailure` | 継電部初期化失敗 | `PowerReset` |
| `*9B` | `OnCarDBDataGetException` | 車上DB閉塞データ取得失敗 | `RetsubanReset` |
| `*9D` | `OnCarDBTrackDataAbnormal` | 車上DB閉塞データ異常 | `RetsubanReset` |
| `*90` | `RetsubanAbnormal` | 列番異常 | `RetsubanReset` |
| `*A1` | `TCSideATSDataAbnormalException` | 車両側ATS情報異常 | `PowerReset` |
| `*80` | `TestingRecordInitializingFailure` | 検査記録部初期化失敗 | `PowerReset` |
| `*C0` | `TGAbnormalException` | TG異常 | `PowerReset` |
| `*C8` | `RelayCarTypeAbnormal` | 車種異常 | `StopDetection` |
| `*DF` | `TransferException` | 伝送部未定義故障 | `PowerReset` |
| `*85` | `TransferInitialzingFailure` | 伝送部初期化失敗 | `StopDetection` |
| `*E3` | `NetworkDataExpired` | データ有効期限切れ | `StopDetection_NetworkReset` |
| `*E5` | `NetworkTimeOutException` | 連続タイムアウト | `StopDetection_NetworkReset` |
| `*EF` | `NetworkException` | 通信部未定義故障 | `PowerReset` |
| `*F0` | `ATSCommonException`（基底） | 未定義故障 | `PowerReset` |

> `*` は系統番号（place 値）が未確定であることを示します。

---

## 非常ブレーキ出力

各例外は `ToBrake()` メソッドで非常ブレーキ出力を返します。

| `OutputBrake` | 対象クラス |
|--------------|----------|
| `OutputBrake.EB`（非常ブレーキあり） | 大部分の例外クラス |
| `OutputBrake.None`（非常ブレーキなし） | `LEDControlCountaException`, `LEDControlException`, `LEDControlInitialzingFailure`, `LEDDisplayNumberAbnormal`, `LEDDisplayStringAbnormal`, `TransferException` |
