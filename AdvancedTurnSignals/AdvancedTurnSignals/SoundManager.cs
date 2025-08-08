using GTA.UI;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AdvancedTurnSignals
{
    public enum SoundType
    {
        None = -1,
        Intro = 0,
        Outro = 1,
        Signal = 2,
        Hazard = 3
    }

    internal class SoundManager
    {
        //サウンド関連
        private WaveOut SoundSystem;
        private WaveFileReader MainReader;
        private NaudioLoop MainLoop;
        XElement xml = null; //XML File
        private List<string> AudioFilePathes = new List<string>(); //XMLに指定されているオーディオファイル名一覧
        private List<string> TurnSignalSounds = new List<string>(); //有効なオーディオファイル名一覧
        private string[] defaultsounds = new string[4]; //要素数は4だがカウント(添え字）は0スタートである点に注意。
        private string[] selectedsounds = new string[4]; //要素数は4だがカウント(添え字）は0スタートである点に注意。
        private bool reload_hint_showed = false; //リロードすると音声ファイルの再読み込みが行われるヒントを表示したかどうか
        private bool PlaySoundInterlock = false;
        private bool SoundStopInterlock = false;

        /// <summary>
        /// 再読み込みのヒント通知を表示したかどうかを確認し、Falseの場合は表示します。
        /// </summary>
        private void CheckAndShowReloadTips()
        {
            if (!reload_hint_showed) //ヒントを表示していない場合
            {
                Utils.ShowNotification(NotificationIcon.LsCustoms, NotificationClass.information, $"{Lang.GetLangData(LangKeys.tips)}~s~ - {Lang.GetLangData(LangKeys.reload_tips)}");
                reload_hint_showed = true;
            }
        }

        public SoundManager()
        {
            init();
        }

        /// <summary>
        /// SoundPlayerの初期化
        /// </summary>
        /// <returns>実行結果</returns>
        private bool init()
        {

            /*
            0: TURN_LEVER_INTRO
            1: TURN_LEVER_OUTRO
            2: INDICATOR_SOUND 
            3: HAZARD_BUTTON
            */

            defaultsounds = new string[4]; //Init
            selectedsounds = new string[4];
            reload_hint_showed = false;
            PlaySoundInterlock = false;
            SoundStopInterlock = false;

            bool audiofail = false;
            NotificationIcon icon;
            //xmlファイルを指定する
            xml = XElement.Load(@"scripts\AdvancedTurnSignals\TurnSignalSoundSetup.xml");

            //タグ内のサウンド情報を取得する
            IEnumerable<string> data = from item in xml.Elements("DefaultSettings").Elements("TURN_LEVER_INTRO")
                                       select item.Value;
            defaultsounds[0] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)
            data = from item in xml.Elements("DefaultSettings").Elements("TURN_LEVER_OUTRO")
                   select item.Value;
            defaultsounds[1] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)
            data = from item in xml.Elements("DefaultSettings").Elements("INDICATOR_SOUND")
                   select item.Value;
            defaultsounds[2] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)
            data = from item in xml.Elements("DefaultSettings").Elements("HAZARD_BUTTON")
                   select item.Value;
            defaultsounds[3] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)

            // XMLに指定されている各オーディオファイル名をListへ
            foreach (string s in defaultsounds)
            {
                AudioFilePathes.Add($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{s}.wav");
            }

            try
            {
                // TurnSignalSoundsフォルダに存在するWAVE音声ファイル一覧
                TurnSignalSounds = Directory.GetFiles(@"scripts\AdvancedTurnSignals\TurnSignalSounds").Where(x => Path.GetExtension(x) == ".wav").ToList();
            }
            catch (Exception e) //取得失敗時
            {
                Console.WriteLine("SoundManager >>> init: Failed! " + e.Message);
                audiofail = true;
            }

            if (!audiofail)
            {
                foreach (var audiofile in AudioFilePathes)
                {
                    if (!TurnSignalSounds.Contains(audiofile)) //XMLに指定されたファイル名がフォルダに存在しない場合
                    {
                        string af = audiofile.Replace(@"scripts\AdvancedTurnSignals\TurnSignalSounds\", ""); //パス部分の削除
                        string message = Lang.GetLangData(LangKeys.audio_warn);
                        message = message.Replace("{0}", af); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Utils.ShowNotification(icon, NotificationClass.warning, message);
                        CheckAndShowReloadTips();
                    }
                }
            }
            else //読み込みに失敗している場合
            {
                icon = NotificationIcon.LesterDeathwish; //骸骨のアイコン
                Utils.ShowNotification(icon, NotificationClass.error, Lang.GetLangData(LangKeys.audio_err));
                return false;
            }

            return true;
        }

        public void sounds_load(string vehicle_name)
        {
            //SoundSetup→Vehiclesのタグ内の情報を取得する
            IEnumerable<XElement> infos = from item in xml.Elements("SoundSetup").Elements("Vehicles")
                                          select item;

            bool exist_custom_settings = false;
            //SoundSetup→Vehicles分ループして、存在チェック
            foreach (XElement info in infos)
            {
                if (info.Element("Vehicle").Value == vehicle_name)
                {
                    exist_custom_settings = true;
                    break;
                }
            }

            if (exist_custom_settings)
            {
                //「vehicle」がvehicle_nameであるサウンドの「ファイル名」を取得する。
                IEnumerable<string> data = from item in xml.Elements("SoundSetup")
                                           where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                                           select item.Element("Sounds").Element("TURN_LEVER_INTRO").Value;
                selectedsounds[0] = data.ElementAt(0);
                data = from item in xml.Elements("SoundSetup")
                       where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                       select item.Element("Sounds").Element("TURN_LEVER_OUTRO").Value;
                selectedsounds[1] = data.ElementAt(0);
                data = from item in xml.Elements("SoundSetup")
                       where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                       select item.Element("Sounds").Element("INDICATOR_SOUND").Value;
                selectedsounds[2] = data.ElementAt(0);
                data = from item in xml.Elements("SoundSetup")
                       where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                       select item.Element("Sounds").Element("HAZARD_BUTTON").Value;
                selectedsounds[3] = data.ElementAt(0);

                NotificationIcon WarningIcon = NotificationIcon.Blocked; //禁止アイコン
                for (int i = 0; i < selectedsounds.Length; i++) //lengthは4。添え字は0スタート。4に満たない間ということは0スタートで3まで（0,1,2,3）要素数の総数と一致。ちょうどいいね。
                {
                    // if (selectedsounds[i] == "") continue; //スキップ

                    // XMLに指定された音声ファイルがTurnSignalSoundsフォルダに存在しない場合
                    if (!TurnSignalSounds.Contains($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[i]}.wav"))
                    {
                        if (Utils.GetSettings(BoolSettingsItem.DefaultSoundEnabled)) // デフォルトサウンドを使用する設定の場合
                        {
                            string message = Lang.GetLangData(LangKeys.default_warn);
                            message = message.Replace("{0}", $"{selectedsounds[i]}.wav"); //特定部分を変数の値に置き換える

                            Utils.ShowNotification(WarningIcon, NotificationClass.warning, message);

                            // デフォルトサウンドに一時的に置き換え
                            selectedsounds[i] = defaultsounds[i];
                        }
                        else //InterSoundがFalseの場合は警告のみ
                        {
                            string message = Lang.GetLangData(LangKeys.audio_warn);
                            message = message.Replace("{0}", $"{selectedsounds[i]}.wav"); //特定部分を変数の値に置き換える

                            Utils.ShowNotification(WarningIcon, NotificationClass.warning, message);

                        }
                        CheckAndShowReloadTips();
                    }
                }
            }
            else //存在しない場合はデフォルト設定を使用
            {
                for (int i = 0; i < selectedsounds.Length; i++) //lengthは4。添え字は0スタート。4に満たない間ということは0スタートで3まで（0,1,2,3）要素数の総数と一致。ちょうどいいね。
                {
                    selectedsounds[i] = defaultsounds[i];
                }
            }
        }

        /// <summary>
        /// 一時的に1回だけ再生します。
        /// </summary>
        /// <param name="type">再生するサウンドタイプ。</param>
        /// <returns>なし</returns>
        public async Task PlaySound(SoundType type)
        {
            // if (selectedsounds[(int)type] == "") return; //指定されていない場合はオフ
            string FilePath = $@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[(int)type]}.wav";
            if (!File.Exists(FilePath))
            {
                Utils.ShowNotification(NotificationIcon.Blocked, NotificationClass.warning, Lang.GetLangData(LangKeys.audio_warn));
                return;
            }

            // Notification.Show("InstantPlay:" + type);
            WaveOut InstantSS = new WaveOut();
            WaveFileReader reader = new WaveFileReader(FilePath);
            InstantSS.Init(reader);
            TimeSpan duration = reader.TotalTime;
            InstantSS.Play();
            await Task.Delay(duration); // 再生時間だけ待機

            // 使用後の後始末
            InstantSS.Stop();
            InstantSS.Dispose();
            reader.Dispose();
        }

        /// <summary>
        /// 現在の再生状態を取得します。
        /// </summary>
        /// <returns>再生状態</returns>
        public PlaybackState GetSoundState()
        {
            if (SoundSystem == null) return PlaybackState.Stopped;
            return SoundSystem.PlaybackState;
        }

        /// <summary>
        /// ウインカー音を再生します。
        /// </summary>
        /// <returns>実行結果</returns>
        public async Task<bool> PlaySound()
        {
            if (PlaySoundInterlock)
            {
                return true;
            }

            string FilePath = $@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[2]}.wav";
            if (!File.Exists(FilePath))
            {
                Utils.ShowNotification(NotificationIcon.Blocked, NotificationClass.error, Lang.GetLangData(LangKeys.audio_err));
                return false; //EnableSoundをFalseにするよう要求
            }

            PlaySoundInterlock = true;

            if (SoundSystem != null && SoundSystem.PlaybackState == PlaybackState.Paused)
            {
                // 一時停止している場合は再開（PlayPauseと同様）
                SoundSystem?.Resume();
                return true;
            }

            await Stop(false, "PlaySoundより"); //Init ※再生状態だった場合に備えて
            if (SoundSystem == null)
            {
                MainReader = new WaveFileReader(FilePath);
                MainLoop = new NaudioLoop(MainReader);
                SoundSystem = new WaveOut();
                SoundSystem.Init(MainLoop);
            }
            SoundSystem.Play();

            return true;
        }

        /// <summary>
        /// サウンド再生/一時停止
        /// </summary>
        public void PlayPause()
        {
            if (SoundSystem == null) return;
            if (SoundSystem.PlaybackState == PlaybackState.Paused)
            {
                SoundSystem?.Resume();
                PlaySoundInterlock = true;
            }
            else if (SoundSystem.PlaybackState == PlaybackState.Playing)
            {
                SoundSystem?.Pause();
                PlaySoundInterlock = false; //再開できるようにインターロック解除
            }
        }

        /// <summary>
        /// サウンド停止
        /// </summary>
        /// <param name="InterlockOff">PlaySoundのインターロックを解除するか</param>
        public async Task Stop(bool InterlockOff = true, string from_memo = "")
        {
            if (SoundStopInterlock)
            {
                // Notification.Show("SoundStop Interlocked!: " + from_memo);
                return;
            }
            SoundStopInterlock = true;
            // Notification.Show("SoundStop executed!: " + from_memo);


            if (InterlockOff) PlaySoundInterlock = false;
            if (SoundSystem == null)
            {
                SoundStopInterlock = false;
                return;
            }

            // フリーズ回避のため別スレッド処理 ※Awaitなので同期処理と挙動は変わらない。ただゲームのフリーズ対策になる。
            await Task.Run(() =>
            {
                if (SoundSystem.PlaybackState != PlaybackState.Stopped)
                {
                    SoundSystem?.Stop();
                    SoundSystem?.Dispose();
                    SoundSystem = null; //各所でNullチェックが使われているので、Null代入は必須となるだろう…。
                }

                MainLoop?.Dispose();
                MainLoop = null;

                MainReader?.Dispose();
                MainReader = null;
            });

            SoundStopInterlock = false;
        }
    }
}