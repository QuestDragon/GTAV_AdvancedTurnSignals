using System.Threading.Tasks;
using System.Threading;
using GTA;
using System;
using NAudio.Wave;
using GTA.UI;


namespace AdvancedTurnSignals
{
    public enum IndicatorState { Off, Left, Right, Hazard }

    public class SignalController
    {
        public IndicatorState currentState = IndicatorState.Off;
        public IndicatorState TurnSignalState = IndicatorState.Off; // ハザード用の状態記憶用
        private CancellationTokenSource tokenSrc;
        private SoundManager SoundHost;

        private Vehicle PrevCurrentVehicle;
        public bool Off_OK = false;
        private bool digital_OK = false;

        // readonly = 再代入不可。Kotlinのvalと同様。
        private bool EnableSound;
        private readonly bool EnableController;
        private readonly bool Headlight;
        private readonly bool EnableAutoOff;
        private readonly float AutoOff_Duration;
        private readonly float AutoOff_Angle;
        private readonly float AutoOffReady_Angle;

        private readonly bool EnabledAutoOn; //1回限りの代入
        private readonly bool EnabledAutoOnSound; //1回限りの代入
        private readonly float AutoOn_Angle;
        private readonly float AutoOn_Speed;

        public bool AutoOffInterlock;
        public bool ActivateIsRunning;
        private Task IndicateHandlerStatus;

        // インスタンス作成時処理
        public SignalController()
        {
            SoundHost = new SoundManager(); //Init
            // GetSettings
            AutoOff_Duration = Utils.GetSettings(FloatSettingsItem.KBCoffTimeValue);
            AutoOff_Angle = Utils.GetSettings(FloatSettingsItem.OffAngleValue);
            AutoOffReady_Angle = Utils.GetSettings(FloatSettingsItem.ReadyAngleValue);
            EnableSound = Utils.GetSettings(BoolSettingsItem.UseSound);
            EnableController = Utils.GetSettings(BoolSettingsItem.UseControllerButton);
            EnableAutoOff = Utils.GetSettings(BoolSettingsItem.AutoOffEnabled);
            Headlight = Utils.GetSettings(BoolSettingsItem.UseHeadlight);

            EnabledAutoOn = Utils.GetSettings(BoolSettingsItem.AutoOnEnabled);
            EnabledAutoOnSound = Utils.GetSettings(BoolSettingsItem.AutoOnSoundEnabled);
            AutoOn_Angle = Utils.GetSettings(FloatSettingsItem.OnAngleValue);
            AutoOn_Speed = Utils.GetSettings(FloatSettingsItem.OnSpeedValue);

            AutoOffInterlock = false;
            ActivateIsRunning = false;
        }

        /// <summary>
        /// 方向指示器の操作
        /// </summary>
        /// <param name="cv">対象車両</param>
        /// <param name="newState">作動箇所</param>
        /// <param name="MuteSound">ウインカーレバーを強制的に鳴らさない</param>
        public async void Activate(Vehicle cv, IndicatorState newState, bool MuteSound = false)
        {
            ActivateIsRunning = true; //Tickのサウンド管理停止用

            bool UseSound = EnableSound;
            if (MuteSound) UseSound = false;

            switch (newState)
            {
                case IndicatorState.Off: //強制停止
                    await Deactivate(true);
                    break;
                case IndicatorState.Left:
                    await ActivateHelper(cv, newState, Headlight, UseSound); // 終わるまで待つ！
                    break;
                case IndicatorState.Right:
                    await ActivateHelper(cv, newState, Headlight, UseSound);
                    break;
                case IndicatorState.Hazard:
                    if (EnableSound && !Utils.IsBike(cv)) await SoundHost.PlaySound(SoundType.Hazard); //再生完了まで待つ（ゲームはフリーズしない）
                    if (currentState == newState) //ハザード状態ならハザード解除
                    {
                        ResetLights(cv, false);
                        currentState = IndicatorState.Off;
                        Activate(cv, TurnSignalState, true); //再帰 (ウインカーレバーが鳴ってしまうのでサウンドはMute。）
                        return;
                    }
                    else //ハザード
                    {
                        TurnSignalState = currentState; //CurrentStateをTurnSignalStateへ移動
                        currentState = newState; //ハザードのみActivateHelperを使わないので手動で代入
                        if (Headlight)
                        {
                            // そもそもヘッドライトが欠損している場合(ウインカーが動いていない、非同期処理も動いていない状態で）
                            TaskStatus IH_Status = TaskStatus.RanToCompletion; //非同期処理の状態 ※デフォルトは処理完了状態
                            if (IndicateHandlerStatus != null)
                            {
                                IH_Status = IndicateHandlerStatus.Status;
                                // Notification.Show("IH_Status: " + IH_Status);
                            }

                            if (IH_Status != TaskStatus.Running && cv.IsLeftHeadLightBroken || cv.IsRightHeadLightBroken)
                            {
                                currentState = IndicatorState.Off;
                                return; //実行不可
                            }

                            ResetLights(cv, false);
                            tokenSrc?.Cancel(); //前回の実行を停止
                            tokenSrc = new CancellationTokenSource();
                            IndicateHandlerStatus = Task.Run(() => { IndicatorHandler(cv, newState, tokenSrc.Token); });
                        }
                        else
                        {
                            cv.IsLeftIndicatorLightOn = true;
                            cv.IsRightIndicatorLightOn = true;
                        }
                    }
                    break;
            }
            ActivateIsRunning = false;
        }

        /// <summary>
        /// Activateメソッドのヘルパー関数
        /// </summary>
        /// <param name="v">対象車両</param>
        /// <param name="s">作動箇所</param>
        /// <param name="h">ヘッドライトモード</param>
        /// <param name="p">サウンド再生</param>
        private async Task ActivateHelper(Vehicle v, IndicatorState s, bool h, bool p)
        {
            if (currentState == s) //既に指定された動作箇所はONの場合
            {
                await Deactivate(true);
                ResetLights(v, false);
                if (p && v.ClassType != VehicleClass.Motorcycles) await SoundHost.PlaySound(SoundType.Outro); //再生完了まで待つ（ゲームはフリーズしない）
                return;
            }
            ResetAutoOffBools(); //AutoOff準備
            if (p && v.ClassType != VehicleClass.Motorcycles) await SoundHost.PlaySound(SoundType.Intro); //サウンド再生
            if (currentState == IndicatorState.Hazard) //ハザード状態
            {
                // Notification.Show("Current is Hazard!");
                // ハザード中は切り替えないが状態は記憶
                // 三項演算子：TurnSignalStateは引数と同じ？ならTurnSignalStateへOffを代入。：違えばTurnSignalStateへ引数の値を代入。
                TurnSignalState = (TurnSignalState == s) ? IndicatorState.Off : s;
            }
            else
            {
                TurnSignalState = IndicatorState.Off; // 既定値に戻す
                if (h)
                {
                    // そもそもヘッドライトが欠損している場合(ウインカーが動いていない、非同期処理も動いていない状態で）
                    TaskStatus IH_Status = TaskStatus.RanToCompletion; //非同期処理の状態 ※デフォルトは処理完了状態
                    if (IndicateHandlerStatus != null)
                    {
                        IH_Status = IndicateHandlerStatus.Status;
                        // Notification.Show("IH_Status: " + IH_Status);
                    }

                    if (currentState == IndicatorState.Off && IH_Status != TaskStatus.Running && v.IsLeftHeadLightBroken || v.IsRightHeadLightBroken)
                    {
                        return; //実行不可
                    }

                    // 問題なければヘッドライトモードをバックグラウンド実行
                    tokenSrc?.Cancel(); //前回の実行を停止
                    tokenSrc = new CancellationTokenSource();
                    IndicateHandlerStatus = Task.Run(() => { IndicatorHandler(v, s, tokenSrc.Token); });
                }
                else
                {
                    if (s == IndicatorState.Left)
                    {
                        v.IsLeftIndicatorLightOn = true;
                        v.IsRightIndicatorLightOn = false;
                    }
                    if (s == IndicatorState.Right)
                    {
                        v.IsLeftIndicatorLightOn = false;
                        v.IsRightIndicatorLightOn = true;
                    }

                }
                currentState = s;
            }
        }

        /// <summary>
        /// SoundManagerクラスへStop指示を出します。（ゲームポーズ用）
        /// </summary>
        public void SoundMute()
        {
            if (SoundHost == null) return;
            SoundHost.PlayPause();
        }

        /// <summary>
        /// AutoOff関連のBoolを全てFalseにします。
        /// </summary>
        private void ResetAutoOffBools()
        {
            Off_OK = false;
            digital_OK = false;
        }

        /// <summary>
        /// 方向指示器の解除（全て停止）
        /// </summary>
        /// <param name="OfftoTSstate">TurnSignalStateもOffにするか</param>
        public async Task Deactivate(bool OfftoTSstate = false)
        {
            Random rnd = new Random();
            tokenSrc?.Cancel(); // 非同期処理キャンセル（IndicatorHandler）
            currentState = IndicatorState.Off;
            if (OfftoTSstate) TurnSignalState = IndicatorState.Off; //引数がTrueならTurnSignalStateもOff。

            ResetAutoOffBools();
            // ResetLights();
            if (EnableSound) await SoundHost.Stop(from_memo: "DeActivate");
            // Notification.Show("Deactivated!: " + rnd.Next());
        }

        /// <summary>
        /// [未実装!!!]方向指示器の処理を行います。
        /// </summary>
        /// <param name="cv">対象車両</param>
        /// <param name="state">作動箇所</param>
        private void IndicatorHandler(Vehicle cv, IndicatorState state)
        {
            throw new NotImplementedException(); //[For Developer] 未実装なので使おうとしたらスクリプトを落とす。…というか現状は使わない？
        }

        /// <summary>
        /// ヘッドライトに対して方向指示器の処理を行います。 ※非同期
        /// </summary>
        /// <param name="cv">対象車両</param>
        /// <param name="state">作動箇所</param>
        /// <param name="token">キャンセル用のトークン</param>
        /// <returns>なし</returns>
        private async Task IndicatorHandler(Vehicle cv, IndicatorState state, CancellationToken token)
        {
            const int DelayTime = 250;
            ResetLights(cv, false);

            // キャンセル指示が出ていない間
            while (!token.IsCancellationRequested && currentState != IndicatorState.Off)
            {

                switch (state)
                {
                    case IndicatorState.Off: //強制停止
                        return;
                    // break;
                    case IndicatorState.Left:
                        //ゲームポーズはスキップ
                        if (Utils.game_pause)
                        {
                            try
                            {
                                await Task.Delay(100, token);
                            }
                            catch (TaskCanceledException)
                            {
                                break; // キャンセル指示が来たらwhileを抜ける
                            }
                            continue;
                        }
                        cv.IsLeftHeadLightBroken = !cv.IsLeftHeadLightBroken; //反転
                        try
                        {
                            await Task.Delay(DelayTime, token);
                        }
                        catch (TaskCanceledException)
                        {
                            break; // キャンセル指示が来たらwhileを抜ける
                        }
                        break;
                    case IndicatorState.Right:
                        //ゲームポーズはスキップ
                        if (Utils.game_pause)
                        {
                            try
                            {
                                await Task.Delay(100, token);
                            }
                            catch (TaskCanceledException)
                            {
                                break; // キャンセル指示が来たらwhileを抜ける
                            }
                            continue;
                        }
                        cv.IsRightHeadLightBroken = !cv.IsRightHeadLightBroken; //反転
                        try
                        {
                            await Task.Delay(DelayTime, token);
                        }
                        catch (TaskCanceledException)
                        {
                            break; // キャンセル指示が来たらwhileを抜ける
                        }
                        break;
                    case IndicatorState.Hazard:
                        //ゲームポーズはスキップ
                        if (Utils.game_pause)
                        {
                            try
                            {
                                await Task.Delay(100, token);
                            }
                            catch (TaskCanceledException)
                            {
                                break; // キャンセル指示が来たらwhileを抜ける
                            }
                            continue;
                        }
                        cv.IsLeftHeadLightBroken = !cv.IsLeftHeadLightBroken; //反転
                        cv.IsRightHeadLightBroken = !cv.IsRightHeadLightBroken; //反転
                        try
                        {
                            await Task.Delay(DelayTime, token);
                        }
                        catch (TaskCanceledException)
                        {
                            break; // キャンセル指示が来たらwhileを抜ける
                        }
                        break;
                }

                try
                {
                    await Task.Delay(DelayTime, token);
                }
                catch (TaskCanceledException)
                {
                    break; // キャンセル指示が来たらwhileを抜ける
                }
            }

            // キャンセル処理
            ResetLights(cv, false);
            return;
        }

        /// <summary>
        /// キーボード操作時にAutoOffを作動させます。
        /// </summary>
        /// <param name="ct">キャンセル用トークン。</param>
        /// <returns>なし</returns>
        public async Task digital_AutoOff(CancellationToken ct)
        {
            digital_OK = false; //再び待機
            // Notification.Show($"Duration: {AutoOff_duration}");
            try
            {
                await Task.Delay((int)AutoOff_Duration, ct); //指定時間経過後に
            }
            catch (TaskCanceledException)
            {
                return; //キャンセル指示が出たら終了
            }

            if (Math.Abs(Game.Player.Character.CurrentVehicle.SteeringAngle) < AutoOff_Angle) //自動消灯角度まで戻っている場合
            {
                // Notification.Show("Digital OK!");
                Off_OK = true;
                digital_OK = true;
            }
            else if (Off_OK) //一度アングルを超えているなら
            {
                // Notification.Show($"Digital NG! {Math.Abs(Game.Player.Character.CurrentVehicle.SteeringAngle)} / {off_angle}");
                digital_OK = true;
            }
        }

        //Steering Angle = マイナスが右回し、プラスが左まわし。

        /// <summary>
        /// ハンドル角度がAutoOffできる角度に達しているかを判定します。
        /// </summary>
        /// <param name="cv">対象車両</param>
        /// <param name="state">作動箇所</param>
        /// <returns>AutoOff可能か</returns>
        private bool CheckOffAngle(Vehicle cv, IndicatorState state)
        {
            if (Off_OK) //すでにTrueの場合は結果を変えない
            {
                return true;
            }
            else if (state == IndicatorState.Left && cv.SteeringAngle > AutoOffReady_Angle) //自動消灯できる角度まで切っている
            {
                return true;
            }
            else if (state == IndicatorState.Right && cv.SteeringAngle < -AutoOffReady_Angle) //自動消灯できる角度まで切っている
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 車両のインジケーター状態を取得します。 ※ヘッドライト式には非対応
        /// </summary>
        /// <param name="cv">対象車両</param>
        /// <returns>現在のインジケーター状態</returns>
        private IndicatorState GetVehicleIndicatorState(Vehicle cv)
        {
            if (cv == null) return IndicatorState.Off;

            if (cv.IsLeftIndicatorLightOn && cv.IsRightIndicatorLightOn)
            {
                return IndicatorState.Hazard;
            }
            else if (cv.IsLeftIndicatorLightOn) return IndicatorState.Left;
            else if (cv.IsRightIndicatorLightOn) return IndicatorState.Right;
            else return IndicatorState.Off; //いずれも該当しない場合（そんなはずはないが）

        }

        /// <summary>
        /// Main.csのOnTickにて使用。ゲームの状態を監視する。
        /// </summary>
        public async void Update()
        {
            if (ActivateIsRunning) return; // Activateが実行中は停止

            // Tickごとに必要な更新処理（自動OFFなど）を行う
            Vehicle cv = Utils.GetCurrentVehicle();

            // 車両に乗っていない場合
            if (cv == null)
            {
                // 作動中に車両から降りた場合
                if (currentState != IndicatorState.Off)
                {
                    ResetLights(PrevCurrentVehicle, true); //下りる前の車両に対してHeadlightをResetLight。 ※通常のウインカーはそのまま
                    await Deactivate(true); //方向指示器動作の停止
                    PrevCurrentVehicle = null;
                }
                return;
            }

            if (PrevCurrentVehicle == null || cv != PrevCurrentVehicle) //車両を乗り換えている場合
            {
                PrevCurrentVehicle = cv; //車両情報更新
                if (EnableSound) SoundHost.sounds_load(cv.DisplayName); // サウンド情報ロード
            }

            if (!Headlight) //通常動作時、現在の車両のインジケーター状態を取得する
            {
                currentState = GetVehicleIndicatorState(cv);
            }

            if (EnableController && MainScript.inputManager.ActiveKeys.Count == 0) //ボタン設定を使用する場合、キーボード入力が無ければ
            {
                // コントローラー操作の監視 ※キーボードの監視はInputManager側で対応している。（イベント登録はMain.csで適用済み）
                MainScript.inputManager.controller(cv);
            }

            //AutoOn処理
            if (EnabledAutoOn && currentState == IndicatorState.Off && cv.ClassType != VehicleClass.Motorcycles) //自動点灯が有効であり、ウインカーが作動していない、バイクでない場合
            {
                //ウインカー無効＆OnAngleを超えている＆OnSpeed以下である場合
                if (cv.SteeringAngle > AutoOn_Angle && cv.Speed > 0 && cv.Speed * 2 < AutoOn_Speed)
                {
                    Activate(cv, IndicatorState.Left,!EnabledAutoOnSound);
                }
                else if (cv.SteeringAngle < -AutoOn_Angle && cv.Speed > 0 && cv.Speed * 2 < AutoOn_Speed)
                {
                    Activate(cv, IndicatorState.Right, !EnabledAutoOnSound);
                }
            }


            // AutoOff処理
            //Steering Angle = マイナスが右回し、プラスが左まわし。
            if (EnableAutoOff && cv.ClassType != VehicleClass.Motorcycles) //自動消灯オン+車に乗っている+バイクでない
            {
                bool KeyboardComp = MainScript.inputManager.KeyboardCompatibility;
                Off_OK = CheckOffAngle(cv, currentState); //自動消灯できる角度まで切っているか
                switch (currentState)
                {
                    case IndicatorState.Left:
                        if (cv.SteeringAngle < -AutoOff_Angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            AutoOffHelper(cv, currentState);
                        }
                        else if (KeyboardComp && Off_OK && digital_OK)
                        {
                            AutoOffHelper(cv, currentState);
                        }
                        else if (!KeyboardComp && Off_OK && cv.SteeringAngle < AutoOff_Angle) //自動消灯条件を満たした場合
                        {
                            AutoOffHelper(cv, currentState);
                        }

                        break;
                    case IndicatorState.Right:

                        if (cv.SteeringAngle > AutoOff_Angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            AutoOffHelper(cv, currentState);
                        }
                        else if (KeyboardComp && Off_OK && digital_OK)
                        {
                            AutoOffHelper(cv, currentState);
                        }
                        else if (!KeyboardComp && Off_OK && cv.SteeringAngle > -AutoOff_Angle)  //自動消灯条件を満たした場合
                        {
                            AutoOffHelper(cv, currentState);
                        }
                        break;
                }
            }


            // AutoOffとActivate処理の機能が働いていない状態限定（ブッキングしてクラッシュするため） 
            if (!AutoOffInterlock)
            {
                // 作動中 (ヘッドライト式もIndicatorStateで判断しているのでOffになればヘッドライト動作も停止する)
                if (currentState != IndicatorState.Off)
                {
                    if (EnableSound && SoundHost.GetSoundState() != PlaybackState.Playing)  //サウンド使用ON ＆ 再生されていない場合
                    {
                        // ここは動く。Resume機能搭載！安心。
                        bool PlaySoundResult = await SoundHost.PlaySound();
                        if (!PlaySoundResult)
                        {
                            // Falseが返ってきたら、オーディオファイルに問題がある、ということ。EnableSoundを無効にする。
                            EnableSound = false;
                        }
                    }
                }
                else //非作動
                {
                    if (EnableSound && SoundHost.GetSoundState() != PlaybackState.Stopped)  //サウンド使用ON ＆ 止まっていない場合
                    {
                        await SoundHost.Stop(from_memo: "トマレー！");
                    }
                }
            }

        }

        /// <summary>
        /// AutoOff作動時の補助を行います。
        /// </summary>
        /// <param name="cv">CurrentVehicle。</param>
        /// <param name="cs">CurrentState。</param>
        private async void AutoOffHelper(Vehicle cv, IndicatorState cs)
        {
            if (AutoOffInterlock) return;
            AutoOffInterlock = true;
            if (EnableSound && !Utils.IsBike(cv)) await SoundHost.PlaySound(SoundType.Outro); //再生終了まで待つ。

            if (cs == IndicatorState.Hazard) //ハザード動作時はTurnSignalStateをOffに。
            {
                TurnSignalState = IndicatorState.Off;
                ResetAutoOffBools();
            }
            else //通常動作時は方向指示器終了
            {
                await Deactivate(true);
                ResetLights(cv, false);
            }
            AutoOffInterlock = false;
        }

        /// <summary>
        /// 方向指示器の状態をリセットします。（Stateは変更されない）
        /// </summary>
        /// <param name="tv">対象車両</param>
        /// <param name="OnlyHeadlight">ヘッドライトモードでのみ動作させるか</param>
        private void ResetLights(Vehicle tv, bool OnlyHeadlight)
        {
            if (tv == null || !tv.Exists()) return;
            if (Headlight)
            {
                tv.IsLeftHeadLightBroken = false;
                tv.IsRightHeadLightBroken = false;
            }
            else if (!OnlyHeadlight)
            {
                tv.IsLeftIndicatorLightOn = false;
                tv.IsRightIndicatorLightOn = false;
            }
        }
    }
}