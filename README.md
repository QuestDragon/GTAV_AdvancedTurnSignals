# Advanced Turn Signals - Created By QuestDragon
Version: 1.0
## 作成した経緯
今日も多数の方向指示器Modがアップロードされていますが、その多くは点灯と消灯を手動で行うものだったり、方向指示器の音がなかったりするものがほとんどでしたので、スクリプト制作の練習も兼ねて作成してみました。

方向指示器の音に関してはゲーム側に一応用意されているのですが、すごく小さい音かつ、ほんの一部の車両でしか鳴らないので、方向指示器の音や方向指示器レバーの音がなる機能も開発しました。

## 機能
車の方向指示器を操作することができます。左折、右折、ハザードランプの点灯、消灯が可能です。

現実の車で採用されている方向指示器の挙動をできる限り再現しており、ハンドルを切って戻すと自動で方向指示器が消灯する機能が用意されています。（ハンドルの角度はiniファイルにて調整可能）

その他に、方向指示器と逆の方向にハンドルを回した際にも消灯するようになっていたり、方向指示器の点灯状態とハザードランプの点灯状態は独立しているなど、細部の挙動にもこだわっています。

方向指示器に関する効果音を使用できます。scripts\TurnSignalSounds フォルダにサンプルの各種効果音を同梱しています。同種同名ファイルに置き換えることでカスタマイズすることが可能です。（iniファイルにて効果音の有効無効を切り替えられます）

ロード時、通知欄に表示される言語は英語と日本語が選択できます。

## 機能追加、フィードバックについて
制作者は初心者なので何かと至らないところがあると思います。

不具合等を発見しましたら、QuestDragonまでご連絡ください。

また、「こんな機能がほしい！」「ここはこうしてほしい！」という要望がありましたらご相談ください。

こちらもスクリプトModについて勉強したいので、ご意見や要望はいつでもお待ちしております。

## 開発環境
C#を使用しています。

ScriptHookV DotNetを使用しており、バージョンは3.6.0のNightly ビルド 57で開発しています。

## インストール
以下から各種ファイルをダウンロードし、スクリプトMod本体はScriptsフォルダに、前提条件のファイルはGTA5.exeと同じフォルダにコピーしてください。

| [Advanced Turn Signals](https://github.com/QuestDragon/GTAV_AdvancedTurnSignals/releases/latest/download/AdvancedTurnSignals.zip) | [ScriptHookV](http://dev-c.com/gtav/scripthookv/) | [ScriptHookV DotNet 3.6.0 Nightly.57](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases/tag/v3.6.0-nightly.57) |
| ------------- | ------------- | ------------- | 
 
## 各種設定
設定はiniファイルから行います。

基本的にiniファイル内にも説明を記述しているので困ることはないと思いますが、こちらにも記載しておきます。

### General
**Enabled**：本スクリプトの有効化と無効化を切り替えます。「*true*」で有効、「*false*」で無効になります。

**UseSound**：効果音の有効化と無効化を切り替えます。同じく「*true*」で有効、「*false*」で無効になります。ただし、音声ファイルが正しく用意されていない場合、スクリプトModロード時に一時的に無効になります。

### Lang
**Language**： スクリプトModから送信される通知メッセージの言語を指定できます。現状「*en*」で英語、「*ja*」で日本語を指定できます。指定が正しくない場合、スクリプトModロード時に一時的に英語に設定されます。

### Keys
方向指示器のキー設定を変更できます。

**Left**：左折

**Right**：右折

**Hazard**：ハザードランプ

指定する文字列は[こちらのサイト](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?redirectedfrom=MSDN&view=windowsdesktop-7.0)をご確認ください。指定が正しくない場合、スクリプトModロード時に一時的にデフォルト設定が読み込まれます。

### Autooff
方向指示器の自動消灯に関する設定が行なえます。

**Autooff**：有効化と無効化を切り替えます。「*true*」で有効、「*false*」で無効になります。ただし、設定条件が正しくない場合、スクリプトModロード時に一時的に無効になります。

**ReadyAngle**：方向指示器が自動消灯するために回さなければならないハンドルの最低角度です。この角度を超えてハンドルを切ると自動消灯機能が働くようになります。

**OffAngle**：方向指示器が自動消灯するハンドルの角度です。この角度までハンドルを戻すか、方向指示器と反対の方向へこの角度まで相対で回すと方向指示器が自動で消灯します。

#### 角度について
GTA5におけるハンドルの最大角度は40度とのことです。40度までの値であればスクリプトModは自動消灯機能を正常に実行できると思います。

## 使い方
iniファイルにて**Enabled**を*True*にしているとゲームロード時に自動で読み込まれ、有効になります。

乗り物に乗って、iniファイルに設定したキーを押すと方向指示器が作動します。

**UseSound**を*True*にしていて、音声ファイルが正しく用意されている場合は効果音が再生されます。

**Autooff**を*True*にしていて、設定が正しい場合は右左折が終わると自動で方向指示器が消灯します。

## 余談
ハンドルの角度で方向指示器を自動消灯するか判断しているので、ikt氏のManual Transmission Modにも対応させようかと思ったのですが、GTA5のアップデートによって使えなくなっていることが判明し、実装できませんでした…。

ScriptHookV DotNetのリリース版（3.6.0）では動作しない理由は、どうやら不具合でハンドル角度が取得できないようです。開発版のNightlyビルドでは修正されているので、こちらを使用する必要があります。

ポーズメニューを表示しても効果音（方向指示器の音）が停止しない他、GTA5をバックグラウンドに移しても効果音はなり続けます。この理由は、効果音はGTA5側というよりPC側から流しているため、このようになっています。

なお、一応ポーズメニューを表示したときに効果音を一時停止するコードは組んであるのですが、ポーズメニューにした際にそもそもスクリプトModの動作も一時停止してしまうため、PC側から流れている効果音をスクリプトMod側で制御できないのかもしれません…。

## 免責事項
本スクリプトModを使用したことにより生じた被害に関して、私QuestDragonは一切の責任を負いかねます。自己責任でご使用ください。

2次配布は禁止です。

予告なく配布を停止することがあります。予めご了承ください。

改造はご自由にしていただいて構いませんが、配布の際はクレジット表記していただけると助かります。

「一から自作した」というのではなく、「QuestDragonのスクリプトの〇〇を△△にした」のように表記していただければと思います。

## 制作者
QuestDragon
