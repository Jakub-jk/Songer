using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using TagLib;
using Un4seen.Bass;
using WPFSoundVisualizationLib;

namespace Songer.Classes
{
    public class PlayerEngine : IWaveformPlayer, ISpectrumPlayer, ISoundPlayer, INotifyPropertyChanged
    {
        private const int waveformCompressedPointCount = 2000;

        private const int repeatThreshold = 200;

        private static PlayerEngine instance;

        private readonly DispatcherTimer positionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);

        private readonly int fftDataSize = 2048;

        private readonly int maxFFT = -2147483645;

        private readonly BackgroundWorker waveformGenerateWorker = new BackgroundWorker();

        private readonly SYNCPROC endTrackSyncProc;

        private readonly SYNCPROC repeatSyncProc;

        private int sampleFrequency = 44100;

        private int activeStreamHandle;

        private TagLib.File fileTag;

        private bool canPlay;

        private bool canPause;

        private bool isPlaying;

        private bool canStop;

        private double channelLength;

        private double currentChannelPosition;

        private float[] fullLevelData;

        private float[] waveformData;

        private bool inChannelSet;

        private bool inChannelTimerUpdate;

        private int repeatSyncId;

        private string pendingWaveformPath;

        private TimeSpan repeatStart;

        private TimeSpan repeatStop;

        private bool inRepeatSet;

        public int ActiveStreamHandle
        {
            get
            {
                return this.activeStreamHandle;
            }
            protected set
            {
                int num = this.activeStreamHandle;
                this.activeStreamHandle = value;
                if (num != this.activeStreamHandle)
                {
                    this.NotifyPropertyChanged("ActiveStreamHandle");
                }
            }
        }

        public bool CanPause
        {
            get
            {
                return this.canPause;
            }
            protected set
            {
                bool flag = this.canPause;
                this.canPause = value;
                if (flag != this.canPause)
                {
                    this.NotifyPropertyChanged("CanPause");
                }
            }
        }

        public bool CanPlay
        {
            get
            {
                return this.canPlay;
            }
            protected set
            {
                bool flag = this.canPlay;
                this.canPlay = value;
                if (flag != this.canPlay)
                {
                    this.NotifyPropertyChanged("CanPlay");
                }
            }
        }

        public bool CanStop
        {
            get
            {
                return this.canStop;
            }
            protected set
            {
                bool flag = this.canStop;
                this.canStop = value;
                if (flag != this.canStop)
                {
                    this.NotifyPropertyChanged("CanStop");
                }
            }
        }

        public double ChannelLength
        {
            get
            {
                return JustDecompileGenerated_get_ChannelLength();
            }
            set
            {
                JustDecompileGenerated_set_ChannelLength(value);
            }
        }

        public double JustDecompileGenerated_get_ChannelLength()
        {
            return this.channelLength;
        }

        protected void JustDecompileGenerated_set_ChannelLength(double value)
        {
            double num = this.channelLength;
            this.channelLength = value;
            if (num != this.channelLength)
            {
                this.NotifyPropertyChanged("ChannelLength");
            }
        }

        public double ChannelPosition
        {
            get
            {
                return this.currentChannelPosition;
            }
            set
            {
                if (!this.inChannelSet)
                {
                    this.inChannelSet = true;
                    double num = this.currentChannelPosition;
                    double num1 = Math.Max(0, Math.Min(value, this.ChannelLength));
                    if (!this.inChannelTimerUpdate)
                    {
                        Bass.BASS_ChannelSetPosition(this.ActiveStreamHandle, Bass.BASS_ChannelSeconds2Bytes(this.ActiveStreamHandle, num1));
                    }
                    this.currentChannelPosition = num1;
                    if (num != this.currentChannelPosition)
                    {
                        this.NotifyPropertyChanged("ChannelPosition");
                    }
                    this.inChannelSet = false;
                }
            }
        }

        public int FileStreamHandle
        {
            get
            {
                return this.activeStreamHandle;
            }
            protected set
            {
                int num = this.activeStreamHandle;
                this.activeStreamHandle = value;
                if (num != this.activeStreamHandle)
                {
                    this.NotifyPropertyChanged("FileStreamHandle");
                }
            }
        }

        public TagLib.File FileTag
        {
            get
            {
                return this.fileTag;
            }
            set
            {
                TagLib.File file = this.fileTag;
                this.fileTag = value;
                if (file != this.fileTag)
                {
                    this.NotifyPropertyChanged("FileTag");
                }
            }
        }

        public static PlayerEngine Instance
        {
            get
            {
                if (PlayerEngine.instance == null)
                {
                    PlayerEngine.instance = new PlayerEngine();
                }
                return PlayerEngine.instance;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return JustDecompileGenerated_get_IsPlaying();
            }
            set
            {
                JustDecompileGenerated_set_IsPlaying(value);
            }
        }

        public bool JustDecompileGenerated_get_IsPlaying()
        {
            return this.isPlaying;
        }

        protected void JustDecompileGenerated_set_IsPlaying(bool value)
        {
            bool flag = this.isPlaying;
            this.isPlaying = value;
            if (flag != this.isPlaying)
            {
                this.NotifyPropertyChanged("IsPlaying");
            }
            this.positionTimer.IsEnabled = value;
        }

        public TimeSpan SelectionBegin
        {
            get
            {
                return this.repeatStart;
            }
            set
            {
                if (!this.inRepeatSet)
                {
                    this.inRepeatSet = true;
                    TimeSpan timeSpan = this.repeatStart;
                    this.repeatStart = value;
                    if (timeSpan != this.repeatStart)
                    {
                        this.NotifyPropertyChanged("SelectionBegin");
                    }
                    this.SetRepeatRange(value, this.SelectionEnd);
                    this.inRepeatSet = false;
                }
            }
        }

        public TimeSpan SelectionEnd
        {
            get
            {
                return this.repeatStop;
            }
            set
            {
                if (!this.inChannelSet)
                {
                    this.inRepeatSet = true;
                    TimeSpan timeSpan = this.repeatStop;
                    this.repeatStop = value;
                    if (timeSpan != this.repeatStop)
                    {
                        this.NotifyPropertyChanged("SelectionEnd");
                    }
                    this.SetRepeatRange(this.SelectionBegin, value);
                    this.inRepeatSet = false;
                }
            }
        }

        public float[] WaveformData
        {
            get
            {
                return JustDecompileGenerated_get_WaveformData();
            }
            set
            {
                JustDecompileGenerated_set_WaveformData(value);
            }
        }

        public float[] JustDecompileGenerated_get_WaveformData()
        {
            return this.waveformData;
        }

        protected void JustDecompileGenerated_set_WaveformData(float[] value)
        {
            float[] singleArray = this.waveformData;
            this.waveformData = value;
            if (singleArray != this.waveformData)
            {
                this.NotifyPropertyChanged("WaveformData");
            }
        }

        private PlayerEngine()
        {
            //this.Initialize();
            this.endTrackSyncProc = new SYNCPROC(this.EndTrack);
            this.repeatSyncProc = new SYNCPROC(this.RepeatCallback);
            //this.waveformGenerateWorker.DoWork += new DoWorkEventHandler(this.waveformGenerateWorker_DoWork);
            this.waveformGenerateWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.waveformGenerateWorker_RunWorkerCompleted);
            this.waveformGenerateWorker.WorkerSupportsCancellation = true;
        }

        private void ClearRepeatRange()
        {
            if (this.repeatSyncId != 0)
            {
                Bass.BASS_ChannelRemoveSync(this.ActiveStreamHandle, this.repeatSyncId);
                this.repeatSyncId = 0;
            }
        }

        private void EndTrack(int handle, int channel, int data, IntPtr user)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => this.Stop()), new object[0]);
        }

        private void GenerateWaveformData(string path)
        {
            if (this.waveformGenerateWorker.IsBusy)
            {
                this.pendingWaveformPath = path;
                this.waveformGenerateWorker.CancelAsync();
                return;
            }
            if (!this.waveformGenerateWorker.IsBusy)
            {
                this.waveformGenerateWorker.RunWorkerAsync(new PlayerEngine.WaveformGenerationParams(2000, path));
            }
        }

        public bool GetFFTData(float[] fftDataBuffer)
        {
            return Bass.BASS_ChannelGetData(this.ActiveStreamHandle, fftDataBuffer, this.maxFFT) > 0;
        }

        public int GetFFTFrequencyIndex(int frequency)
        {
            return Utils.FFTFrequency2Index(frequency, this.fftDataSize, this.sampleFrequency);
        }

        public TimeSpan BytesToTimeSpan(long bytes)
        {
            return TimeSpan.FromSeconds(Bass.BASS_ChannelBytes2Seconds(activeStreamHandle, bytes));
        }

        public void Initialize()
        {
            this.positionTimer.Interval = TimeSpan.FromMilliseconds(50);
            this.positionTimer.Tick += new EventHandler(this.positionTimer_Tick);
            this.IsPlaying = false;
            Window mainWindow = Application.Current.MainWindow;
            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_SPEAKERS, (new WindowInteropHelper(mainWindow)).Handle))
            {
                Bass.BASS_PluginLoad("bass_aac.dll");
                Bass.BASS_PluginLoad("basswma.dll");
                return;
            }
            MessageBox.Show(mainWindow, "Bass initialization error!");
            mainWindow.Close();
        }

        private void NotifyPropertyChanged(string info)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public bool OpenFile(string path)
        {
            this.Stop();
            if (this.ActiveStreamHandle != 0)
            {
                this.ClearRepeatRange();
                this.ChannelPosition = 0;
                Bass.BASS_StreamFree(this.ActiveStreamHandle);
            }
            if (System.IO.File.Exists(path))
            {
                int num = Bass.BASS_StreamCreateFile(path, (long)0, (long)0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_SAMPLE_OVER_POS | BASSFlag.BASS_MIXER_PAUSE | BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_MUSIC_FLOAT | BASSFlag.BASS_MUSIC_PRESCAN);
                int num1 = num;
                this.ActiveStreamHandle = num;
                this.FileStreamHandle = num1;
                this.ChannelLength = Bass.BASS_ChannelBytes2Seconds(this.FileStreamHandle, Bass.BASS_ChannelGetLength(this.FileStreamHandle, BASSMode.BASS_POS_BYTES));
                this.FileTag = TagLib.File.Create(path);
                this.GenerateWaveformData(path);
                if (this.ActiveStreamHandle != 0)
                {
                    BASS_CHANNELINFO bASSCHANNELINFO = new BASS_CHANNELINFO();
                    Bass.BASS_ChannelGetInfo(this.ActiveStreamHandle, bASSCHANNELINFO);
                    this.sampleFrequency = bASSCHANNELINFO.freq;
                    if (Bass.BASS_ChannelSetSync(this.ActiveStreamHandle, BASSSync.BASS_SYNC_END, (long)0, this.endTrackSyncProc, IntPtr.Zero) == 0)
                    {
                        throw new ArgumentException("Error establishing End Sync on file stream.", "path");
                    }
                    this.CanPlay = true;
                    return true;
                }
                this.ActiveStreamHandle = 0;
                this.FileTag = null;
                this.CanPlay = false;
            }
            return false;
        }

        public void Pause()
        {
            if (this.IsPlaying && this.CanPause)
            {
                Bass.BASS_ChannelPause(this.ActiveStreamHandle);
                this.IsPlaying = false;
                this.CanPlay = true;
                this.CanPause = false;
            }
        }

        public void Play()
        {
            if (this.CanPlay)
            {
                this.PlayCurrentStream();
                this.IsPlaying = true;
                this.CanPause = true;
                this.CanPlay = false;
                this.CanStop = true;
            }
        }

        private void PlayCurrentStream()
        {
            if (this.ActiveStreamHandle != 0)
            {
                Bass.BASS_ChannelPlay(this.ActiveStreamHandle, false);
            }
        }

        private void positionTimer_Tick(object sender, EventArgs e)
        {
            if (this.ActiveStreamHandle == 0)
            {
                this.ChannelPosition = 0;
                return;
            }
            this.inChannelTimerUpdate = true;
            this.ChannelPosition = Bass.BASS_ChannelBytes2Seconds(this.ActiveStreamHandle, Bass.BASS_ChannelGetPosition(this.ActiveStreamHandle, BASSMode.BASS_POS_BYTES));
            this.inChannelTimerUpdate = false;
        }

        private void RepeatCallback(int handle, int channel, int data, IntPtr user)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => this.ChannelPosition = this.SelectionBegin.TotalSeconds), new object[0]);
        }

        private void SetRepeatRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (this.repeatSyncId != 0)
            {
                Bass.BASS_ChannelRemoveSync(this.ActiveStreamHandle, this.repeatSyncId);
            }
            if ((endTime - startTime) <= TimeSpan.FromMilliseconds(200))
            {
                this.ClearRepeatRange();
                return;
            }
            long num = Bass.BASS_ChannelGetLength(this.ActiveStreamHandle);
            long totalSeconds = (long)(endTime.TotalSeconds / this.ChannelLength * (double)num);
            this.repeatSyncId = Bass.BASS_ChannelSetSync(this.ActiveStreamHandle, BASSSync.BASS_SYNC_POS, totalSeconds, this.repeatSyncProc, IntPtr.Zero);
            this.ChannelPosition = this.SelectionBegin.TotalSeconds;
        }

        public void Stop()
        {
            this.ChannelPosition = this.SelectionBegin.TotalSeconds;
            if (this.ActiveStreamHandle != 0)
            {
                Bass.BASS_ChannelStop(this.ActiveStreamHandle);
                Bass.BASS_ChannelSetPosition(this.ActiveStreamHandle, this.ChannelPosition);
            }
            this.IsPlaying = false;
            this.CanStop = false;
            this.CanPlay = true;
            this.CanPause = false;
        }

        /*private void waveformGenerateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BassEngine.WaveformGenerationParams argument = e.Argument as BassEngine.WaveformGenerationParams;
            int num = Bass.BASS_StreamCreateFile(argument.Path, (long)0, (long)0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_OVER_POS | BASSFlag.BASS_MIXER_PAUSE | BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_MUSIC_FLOAT | BASSFlag.BASS_MUSIC_DECODE | BASSFlag.BASS_MUSIC_PRESCAN);
            int num1 = (int)Bass.BASS_ChannelSeconds2Bytes(num, 0.02);
            long num2 = Bass.BASS_ChannelGetLength(num, BASSMode.BASS_POS_BYTES);
            int num3 = (int)((double)num2 / (double)num1);
            int num4 = num3 * 2;
            float[] singleArray = new float[num4];
            float[] singleArray1 = new float[2];
            int num5 = Math.Min(argument.Points, num3);
            float[] singleArray2 = new float[num5 * 2];
            List<int> nums = new List<int>();
            for (int i = 1; i <= num5; i++)
            {
                nums.Add((int)Math.Round((double)num4 * ((double)i / (double)num5), 0));
            }
            float single = float.MinValue;
            float single1 = float.MinValue;
            int num6 = 0;
            int num7 = 0;
            while (num7 < num4)
            {
                Bass.BASS_ChannelGetLevel(num, singleArray1, 1f);
                singleArray[num7] = singleArray1[0];
                singleArray[num7 + 1] = singleArray1[1];
                if (singleArray1[0] > single)
                {
                    single = singleArray1[0];
                }
                if (singleArray1[1] > single1)
                {
                    single1 = singleArray1[1];
                }
                if (num7 > nums[num6])
                {
                    singleArray2[num6 * 2] = single;
                    singleArray2[num6 * 2 + 1] = single1;
                    single = float.MinValue;
                    single1 = float.MinValue;
                    num6++;
                }
                if (num7 % 3000 == 0)
                {
                    float[] singleArray3 = (float[])singleArray2.Clone();
                    Application.Current.Dispatcher.Invoke(new Action(() => this.WaveformData = singleArray3), new object[0]);
                }
                if (!this.waveformGenerateWorker.CancellationPending)
                {
                    num7 = num7 + 2;
                }
                else
                {
                    e.Cancel = true;
                    break;
                }
            }
            float[] singleArray4 = (float[])singleArray2.Clone();
            Application.Current.Dispatcher.Invoke(new Action(() => {
                this.fullLevelData = singleArray;
                this.WaveformData = singleArray4;
            }), new object[0]);
            Bass.BASS_StreamFree(num);
        }*/

        private void waveformGenerateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled && !this.waveformGenerateWorker.IsBusy)
            {
                this.waveformGenerateWorker.RunWorkerAsync(new PlayerEngine.WaveformGenerationParams(2000, this.pendingWaveformPath));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private class WaveformGenerationParams
        {
            public string Path
            {
                get;
                protected set;
            }

            public int Points
            {
                get;
                protected set;
            }

            public WaveformGenerationParams(int points, string path)
            {
                this.Points = points;
                this.Path = path;
            }
        }
    }
}