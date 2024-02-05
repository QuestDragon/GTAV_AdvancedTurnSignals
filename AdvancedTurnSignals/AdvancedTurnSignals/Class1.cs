using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using System.Runtime.InteropServices;
using System.Windows.Forms; // KeyEventArgs用
using GTA.UI;
using GTA.Math;
using System.Media;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace AdvancedTurnSignals
{
    public class Class1 : Script //Scriptは継承。継承すると継承元の情報を利用できる。(この場合はSHV.NETの情報。なお、スクリプト作成には必須。）
    {
        //iniファイルの読み込みに必要な処理。Pythonのimport文、C#のUsing文の派生版みたいなもんよ
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
        private static Keys left; //キー設定を用意
        private static Keys right; //キー設定を用意
        private static Keys hazard; //キー設定を用意
        private static Keys escape = Keys.Escape; //キー設定を用意
        private static Keys pause1; //キー設定を用意
        private static Keys pause2; //キー設定を用意
        private static string lang; //言語
        private static bool enabled = true; //有効または無効
        private static bool autooff = true; //自動消灯
        private static string raw_ready_angle = "15"; //自動消灯に最低限必要な角度
        private static string raw_off_angle = "10"; //自動消灯する角度
        private static Single ready_angle = 15; //自動消灯に最低限必要な角度(コード内で使用)
        private static Single off_angle = 10; //自動消灯する角度(コード内で使用)
        private static Single? toggle_angle = null; //ウインカーと反対側にハンドルを切った場合に使用する。
        public static bool use_sound = true;

        //最大角度：バニラで-+40
        //ReadyAngleは現実の最大角度240度のうち90度設定と合うよう、40度のうち15度と設定。
        //OffAngleは現実の最大角度240度のうち60度設定と合うよう、40度のうち10度と設定。

        static bool turn_left = false; //ハザード用
        static bool turn_right = false;
        static bool is_hazard = false; //ハザード状態か
        static bool off_ready = false; //自動消灯アングルまでハンドルが切られているか

        //サウンド関連
        private System.Media.SoundPlayer player = null;
        private string[] audiofiles;
        private List<string> requireAudio = new List<string> { @"scripts\TurnSignalSounds\HAZARD_BUTTON.wav", @"scripts\TurnSignalSounds\INDICATOR_SOUND.wav", @"scripts\TurnSignalSounds\TURN_LEVER_INTRO.wav", @"scripts\TurnSignalSounds\TURN_LEVER_OUTRO.wav" };

        public Class1()
        {
            ScriptSettings ini = ScriptSettings.Load(@"scripts\AdvancedTurnSignals.ini");
            // iniのデータを読み込む (セクション、キー、デフォルト値)
            left = ini.GetValue<Keys>("Keys", "Left", Keys.J);
            right = ini.GetValue<Keys>("Keys", "Right", Keys.K);
            hazard = ini.GetValue<Keys>("Keys", "Hazard", Keys.I);
            pause1 = ini.GetValue<Keys>("Keys", "PrimaryPause", Keys.P);
            pause2 = ini.GetValue<Keys>("Keys", "SecondaryPause", Keys.None);
            lang = ini.GetValue<string>("Lang", "Language", "en");
            enabled = ini.GetValue<bool>("General", "Enabled", true);
            use_sound = ini.GetValue<bool>("General", "UseSound", true);
            autooff = ini.GetValue<bool>("Autooff", "Autooff", true);
            raw_ready_angle = ini.GetValue<string>("Autooff", "ReadyAngle", "15");
            raw_off_angle = ini.GetValue<string>("Autooff", "OffAngle", "10");

            if (enabled)
            {
                KeyDown += keyDown;
                Tick += onTick;

                bool ignore_lang = false;
                if (lang != "en" && lang != "ja") //言語が日本語または英語でない場合
                {
                    lang = "en"; //英語表記を使用
                    ignore_lang = true; //言語設定を無視しているフラグを立てる
                }

                if (lang == "en")
                {
                    Notification.Show("Advanced Turn Signals: ~g~has loaded!");
                    if (ignore_lang)
                    {
                        Notification.Show("Advanced Turn Signals: ~o~The language specification is ~h~incorrect, ~h~~o~so it will be displayed in English.");
                    }
                }
                else
                {
                    Notification.Show("Advanced Turn Signals: ~g~スクリプトの読み込みに成功しました！");
                }

                if (autooff)
                {
                    float res = 0; //数値変換結果

                    if (!float.TryParse(raw_ready_angle, out res)) //数値でない場合
                    {
                        if (lang == "en")
                        {
                            Notification.Show($"Advanced Turn Signals: ~y~{raw_ready_angle} ~r~in ReadyAngle is ~h~not a number. ~h~~n~~o~The default number ~b~(15) ~o~will be used.");
                        }
                        else
                        {
                            Notification.Show($"Advanced Turn Signals: ~r~ReadyAngle の~y~{raw_ready_angle} ~r~は~h~数値ではありません。  ~h~~n~~o~デフォルトの数値~b~（15）~o~が使用されます。");
                        }
                        raw_ready_angle = "15";
                    }

                    if (!float.TryParse(raw_off_angle, out res)) //数値でない場合
                    {
                        if (lang == "en")
                        {
                            Notification.Show($"Advanced Turn Signals: ~y~{raw_off_angle} ~r~in OffAngle is ~h~not a number. ~h~~n~~o~The default number ~b~(10) ~o~will be used.");
                        }
                        else
                        {
                            Notification.Show($"Advanced Turn Signals: ~r~OffAngle の~y~{raw_off_angle} ~r~は~h~数値ではありません。  ~h~~n~~o~デフォルトの数値~b~（10）~o~が使用されます。");
                        }
                        raw_off_angle = "10";
                    }

                    //使用できる形に変換
                    ready_angle = float.Parse(raw_ready_angle);
                    off_angle = float.Parse(raw_off_angle);
                    if (ready_angle > 40 || off_angle > 40) //GTAVの角度40度を超えた設定の場合
                    {
                        if (lang == "en")
                        {
                            Notification.Show($"Advanced Turn Signals: ~o~There are settings where the steering wheel angle exceeds GTA5's maximum steering angle of ~h~40 ~h~~o~degrees.~n~The script will continue to work, ~y~but the turn signal ~h~may not turn off automatically.");
                        }
                        else
                        {
                            Notification.Show($"Advanced Turn Signals: ~o~ハンドル角度がGTA5の最大ハンドル角度である~h~40度~h~を超えている設定があります。~n~スクリプトは動作を続行しますが、~y~ウインカーが~h~自動消灯しない可能性があります。");
                        }

                    }
                    if (ready_angle < off_angle) //ReadyAngleよりもOffAngleのほうが数値が高い場合
                    {
                        if (lang == "en")
                        {
                            Notification.Show($"Advanced Turn Signals: ~r~OffAngle value ~y~{off_angle} ~r~cannot be higher than ReadyAngle value ~b~{ready_angle}~r~. ~n~~o~Auto-off feature has been ~h~disabled.");
                        }
                        else
                        {
                            Notification.Show($"Advanced Turn Signals: ~r~OffAngleの値 ~y~{off_angle} ~r~はReadyAngleの値 ~b~{ready_angle} ~r~より高くすることはできません。~n~~o~自動消灯機能は~h~無効化されました。");
                        }
                        autooff = false;
                    }
                }
                if (use_sound)
                {
                    bool audiofail = false;
                    try
                    {
                        audiofiles = Directory.GetFiles(@"scripts\TurnSignalSounds").Where(x => Path.GetExtension(x) == ".wav").ToArray();
                    }
                    catch (Exception e)
                    {
                        audiofail = true;
                    }

                    if (!audiofail)
                    {
                        foreach (var audiofile in audiofiles)
                        {
                            if (!requireAudio.Contains(audiofile))
                            {
                                audiofail = true;
                                break;
                            }
                        }
                    }

                    if (audiofail)
                    {
                        if (lang == "en")
                        {
                            Notification.Show($"Advanced Turn Signals: ~r~A required audio file is missing. ~n~~o~UseSound feature has been ~h~disabled.");
                        }
                        else
                        {
                            Notification.Show($"Advanced Turn Signals: ~r~必要なオーディオファイルが不足しています。~n~~o~サウンド再生機能は~h~無効化されました。");
                        }
                        use_sound = false;

                    }

                }
            }
#if DEBUG
            Notification.Show("Advanced turn signals is READY!");
#endif

        }

        /*
        private int soundtime(string file)
        {
            AudioFileReader audioStream;
            int bytePerSec;
            int musicLength_s;

            // ファイル名の拡張子によって、異なるストリームを生成
            audioStream = new AudioFileReader(file);

            // 1秒あたりのバイト数を計算
            bytePerSec = (audioStream.WaveFormat.BitsPerSample / 8) * audioStream.WaveFormat.SampleRate * audioStream.WaveFormat.Channels;
            // 音楽の長さ (秒)を計算
            musicLength_s = (int)audioStream.Length / bytePerSec;

            return musicLength_s;
        }
                */

        private void signal_manual(string mode = "turn")
        {
            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }

            //読み込む
            if (mode == "turn")
            {
                player = new SoundPlayer(@"scripts\TurnSignalSounds\TURN_LEVER_INTRO.wav");
            }
            else if(mode == "hazard")
            {
                player = new SoundPlayer(@"scripts\TurnSignalSounds\HAZARD_BUTTON.wav");
            }
            
            //再生する
            player.PlaySync();

            //一度破棄
            player.Stop();
            player.Dispose();
            player = null;

            if(Game.Player.Character.CurrentVehicle.IsLeftIndicatorLightOn | Game.Player.Character.CurrentVehicle.IsRightIndicatorLightOn)
            {
                player = new SoundPlayer(@"scripts\TurnSignalSounds\INDICATOR_SOUND.wav");
                player.PlayLooping();
            }
        }

        private void signal_autooff()
        {
            if (player != null) //再生中なら一度破棄
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
            //読み込む
            player = new SoundPlayer(@"scripts\TurnSignalSounds\TURN_LEVER_OUTRO.wav");
            //再生する
            player.PlaySync();
            //用済みなので破棄
            player.Stop();
            player.Dispose();
            //nullを入れてしまうとTick側の再生処理が反応してしまうのでDisposeまで

            Task.Delay(1000);
            if (Game.Player.Character.CurrentVehicle.IsLeftIndicatorLightOn | Game.Player.Character.CurrentVehicle.IsRightIndicatorLightOn)
            {
                player = new SoundPlayer(@"scripts\TurnSignalSounds\INDICATOR_SOUND.wav");
                player.PlayLooping();
            }

        }

        private async void keyDown(object sender, KeyEventArgs e)
        {

            Vehicle cv = Game.Player.Character.CurrentVehicle;

            if (enabled && cv != null && new Keys[] { left,right,hazard }.Contains(e.KeyCode)) //有効かつ車に乗っているかつ設定したキーバインドが押されている
            {
                if (e.KeyCode == left)
                {
                    if (use_sound)
                    {
                        // ウインカー再生
                        Task t = Task.Run(() => { signal_manual(); });
                    }

                    if (!is_hazard)
                    {
                        cv.IsRightIndicatorLightOn = false; //消灯
                        cv.IsLeftIndicatorLightOn = !cv.IsLeftIndicatorLightOn; //ON/OFF 切り替え
                        turn_left = cv.IsLeftIndicatorLightOn;
                        if (cv.IsLeftIndicatorLightOn && autooff) //ウインカー有効時
                        {
                            toggle_angle = cv.SteeringAngle;
                            off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか

                        }
                    }
                    else
                    {
                        turn_left = !turn_left;
                    }
                }
                else if(e.KeyCode == right)
                {
                    if (use_sound)
                    {
                        // ウインカー再生
                        Task t = Task.Run(() => { signal_manual(); });
                    }
                    if (!is_hazard)
                    {
                        cv.IsLeftIndicatorLightOn = false; //消灯
                        cv.IsRightIndicatorLightOn = !cv.IsRightIndicatorLightOn; //ON/OFF 切り替え

                        turn_right = cv.IsRightIndicatorLightOn;
                        if (cv.IsRightIndicatorLightOn && autooff)
                        {
                            toggle_angle = cv.SteeringAngle;
                            off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか
                        }                    }
                    else
                    {
                        turn_right = !turn_right;
                    }
                }
                else if(e.KeyCode == hazard)
                {
                    if (cv.IsLeftIndicatorLightOn && cv.IsRightIndicatorLightOn) //ハザード状態の場合のみ
                    {
                        cv.IsLeftIndicatorLightOn = turn_left; //前回の状態に戻す
                        cv.IsRightIndicatorLightOn = turn_right;
                        is_hazard = false;
                    }
                    else
                    {
                        turn_left = cv.IsLeftIndicatorLightOn; //状態を保存
                        turn_right = cv.IsRightIndicatorLightOn;

                        cv.IsRightIndicatorLightOn = true; //点灯
                        cv.IsLeftIndicatorLightOn = true; //点灯
                        is_hazard= true;
                    }
                    if (use_sound)
                    {
                        Task t = Task.Run(() => { signal_manual("hazard"); });
                    }

                }
            }

            if (use_sound && new Keys[] { pause1,pause2,escape }.Contains(e.KeyCode))
            {
                await Task.Delay(1000);
                if (Game.IsPaused) //ポーズ中の場合
                {
                    if (player != null)
                    {
                        player.Stop();
                        player.Dispose();
                        player = null;
                    }
                }

            }

        }

        //Steering Angle = マイナスが右回し、プラスが左まわし。

        private bool is_off_angle(string LR)
        {
            Vehicle cv = Game.Player.Character.CurrentVehicle;

            if (off_ready) //すでにTrueの場合は結果を変えない
            {
                return true;
            }
            else if (LR == "L" && cv.SteeringAngle > ready_angle) //自動消灯できる角度まで切っている
            {
                return true;
            }
            else if (LR == "R" && cv.SteeringAngle < -ready_angle) //自動消灯できる角度まで切っている
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void onTick(object sender, EventArgs e)
        {
            Vehicle cv = Game.Player.Character.CurrentVehicle;
            if(cv == null && player!= null)  //非乗車状態でサウンドが鳴っている場合
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
            else if(cv != null && player == null && cv.IsLeftIndicatorLightOn | cv.IsRightIndicatorLightOn) //車両乗車時にウインカーが有効の場合
            {
                player = new SoundPlayer(@"scripts\TurnSignalSounds\INDICATOR_SOUND.wav"); //サウンド再生(loop)
                player.PlayLooping();

            }

            if (autooff && cv != null) //自動消灯オン+車に乗っている
            {
                if (!is_hazard) //ハザード状態でない
                {
                    if (cv.IsLeftIndicatorLightOn) //Left indicator
                    {
                        off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか
                        if (cv.SteeringAngle < toggle_angle - off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            cv.IsLeftIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }


                        }
                        else if (off_ready && cv.SteeringAngle < off_angle) //自動消灯条件を満たした場合
                        {
                            cv.IsLeftIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }
                    else if (cv.IsRightIndicatorLightOn) //Right indicator
                    {
                        off_ready = is_off_angle("R"); //自動消灯できる角度まで切っているか

                        if (cv.SteeringAngle > toggle_angle + off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            cv.IsRightIndicatorLightOn = false;
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }

                        }
                        else if (off_ready && cv.SteeringAngle > -off_angle)  //自動消灯条件を満たした場合
                        {
                            cv.IsRightIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }
                }
                else //ハザード状態の場合
                {
                    if (turn_left) //Left indicator
                    {
                        off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか

                        if (cv.SteeringAngle < toggle_angle - off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            turn_left = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (off_ready && cv.SteeringAngle < off_angle) //自動消灯条件を満たした場合
                        {
                            turn_left = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }
                    else if (turn_right) //Right indicator
                    {
                        off_ready = is_off_angle("R"); //自動消灯できる角度まで切っているか
                        if (cv.SteeringAngle > toggle_angle + off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            turn_right = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (off_ready && cv.SteeringAngle > -off_angle) //自動消灯条件を満たした場合
                        {
                            turn_right = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }

                }
            }

        }
    }

}
