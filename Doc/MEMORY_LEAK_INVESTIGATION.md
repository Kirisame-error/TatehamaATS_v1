# メモリリーク調査 協力依頼ガイド

特定の環境でATSプロセスが約20GBまでメモリを消費する事例が報告されています。
通常は0.5GB程度です。原因特定のため、本ビルドには軽量な常時メモリロガーを
組み込んでいます。再現環境の方は以下にご協力ください。

## 1. 取得されるデータ（自動）

ATS起動と同時に `logs/memory_YYYYMMDD_HHmmss_<PID>.log` が作成され、
10秒ごとに以下が追記されます。プログラム本体の動作には影響しません。

| 列 | 内容 |
| --- | --- |
| time | 取得時刻(JST) |
| WS | WorkingSet(物理メモリ占有量) |
| Priv | PrivateBytes |
| Paged | ページプール |
| Virtual | 仮想メモリ |
| GCHeap | .NET管理ヒープサイズ |
| GCTotalCommitted | GCがOSから確保した総量 |
| Gen0/1/2 | 各世代のGC回数 |
| Handles | カーネルハンドル数 |
| Threads | スレッド数 |
| GDI | GDIオブジェクト数 (画像系リーク検出に重要) |
| USER | USERオブジェクト数 |

`WS >= 1GB` を超えると `*** WARN level=1 ***`、`>= 4GB` で `level=2` の
マーカが行に付きます。

## 2. ご協力いただきたい手順

1. ATS を通常通り起動して、症状が出るまで普段の運転をしてください。
2. メモリ使用量が異常に増えた、または ATS が落ちた／重くなった、と感じた段階で
   タスクマネージャを開き、`TatehamaATS_v1.exe` の **メモリ列の値を一緒に控えて**
   おいてください。
3. ATS を終了してください。クラッシュ時は `logs/crash_*.log` も併せて残ります。
4. 以下のファイルを開発者まで送付してください。
   - `logs/memory_*.log` 全部
   - `logs/inspection_error*.log`（既存のもの）
   - 可能ならクラッシュ時の `logs/crash_*.log`
   - タスクマネージャで観測した最大メモリ値とそのときの操作内容
   - OS（Windows 10/11、ビルド番号）、ディスプレイの解像度・倍率(125%等)・
     モニタ枚数、TrainCrew のバージョン
   - 該当ウィンドウ(LED表示器・列番設定器・告知装置)をどれを表示していたか

## 3. もし可能なら：フルメモリダンプ取得

メモリが10GBを超えた状態でフルダンプが取れると、解析が一気に進みます。
取得は **任意** ですが、可能な方は以下の手順を試してください。

### 方法A: タスクマネージャ（最も簡単）

1. タスクマネージャを開き、「詳細」タブで `TatehamaATS_v1.exe` を右クリック
2. **「ダンプ ファイルの作成」**
3. 出力された `.DMP` ファイル（数GB～十数GB）をクラウドストレージで共有

### 方法B: procdump

1. https://learn.microsoft.com/sysinternals/downloads/procdump から取得
2. 管理者PowerShell から実行：
   ```
   procdump -ma TatehamaATS_v1.exe C:\temp\ats_full.dmp
   ```

### 方法C: dotnet-dump (.NET推奨)

1. PowerShellで以下を実行（.NET SDKが必要）：
   ```
   dotnet tool install -g dotnet-dump
   dotnet-dump ps
   dotnet-dump collect -p <PID> -o C:\temp\ats.dmp
   ```

## 4. 現在疑っている箇所

開発者側で先行調査済みの主な疑い箇所は以下です。ダンプ・ログでの確認対象。

- `RetsubanWindow/LCDLogic.cs` `LCDDrawing()`
  - 10ms周期(`ClockTimer.Interval = 10`)で `Bitmap` を多数生成し、
    `LCD.BackgroundImage` に代入。旧 BackgroundImage が `Dispose` されていない。
- `ATSDisplay/LEDWindow.cs` `DisplayImage()`
  - 20ms周期 (`ControlLED.StartDisplayUpdateLoop`) で3枚のBitmapを生成し、
    `pictureBox.BackgroundImage` に代入。旧 BackgroundImage と中間Bitmap
    (`codeAImage` 等、`croppedImage`、`enlargedImage`)が `Dispose` されない。
- `KokuchiWindow/KokuchiWindow.cs` `timer1_Tick` (50ms周期)
  - 旧BackgroundImage は Dispose しているが、毎tick `GetImageByPos`/
    `EnlargePixelArt` で中間Bitmapを多数生成、それらは Dispose されない。

これらはマネージドオブジェクトとしては小さくGCが走りにくく、
**Bitmap内部のアンマネージドGDIメモリ**だけが滞留して肥大化しうるパターンです。
ディスプレイの解像度・DPIスケール・モニタ枚数で1枚あたりのバイト数が変わるため、
**特定環境でのみ肥大化する**症状とも整合します。

ログ上 `GDI` 列が時間と共に増え続けていれば、上記のいずれかが真因と確定できます。

ご協力ありがとうございます。
