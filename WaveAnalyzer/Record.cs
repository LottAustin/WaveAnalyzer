using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading;
using System.Media;

namespace WaveAnalyer
{
    public unsafe partial class RecordDialog : Form
    {
        // DLL calls
        [DllImport("winmm.dll")]
        public static extern int waveInAddBuffer(IntPtr hWaveIn, ref WAVEHDR lpWaveHdr, uint cWaveHdrSize);
        [DllImport("winmm.dll")]
        public static extern int waveInPrepareHeader(IntPtr hWaveIn, ref WAVEHDR lpWaveHdr, uint Size);
        [DllImport("winmm.dll")]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveHdr, uint Size);
        [DllImport("winmm.dll")]
        public static extern int waveOutWrite(IntPtr hWaveOut, ref WAVEHDR lpWaveHdr, uint Size);
        [DllImport("winmm.dll")]
        public static extern int waveInStart(IntPtr hWaveIn);
        [DllImport("winmm.dll", EntryPoint = "waveInOpen", SetLastError = true)]
        public static extern int waveInOpen(ref IntPtr t, uint id, ref WAVEFORMAT pwfx, IntPtr dwCallback, int dwInstance, int fdwOpen);
        [DllImport("winmm.dll", EntryPoint = "waveOutOpen", SetLastError = true)]
        public static extern int waveOutOpen(ref IntPtr t, uint id, ref WAVEFORMAT pwfx, IntPtr dwCallback, int dwInstance, int fdwOpen);
        [DllImport("winmm.dll", EntryPoint = "waveInUnprepareHeader", SetLastError = true)]
        public static extern int waveInUnprepareHeader(IntPtr hwi,ref WAVEHDR pwh, uint cbwh);
        [DllImport("winmm.dll",EntryPoint="waveInClose",SetLastError=true)]
        public static extern uint waveInClose(IntPtr hwnd);
        [DllImport("winmm.dll",EntryPoint="waveInReset",SetLastError=true)]
        static extern uint waveInReset(IntPtr hwi);
        [DllImport("winmm.dll", EntryPoint = "waveOutReset", SetLastError = true)]
        static extern uint waveOutReset(IntPtr hwi);
        [DllImport("winmm.dll", EntryPoint = "waveInStop", SetLastError = true)]
        static extern uint waveInStop(IntPtr hwi);

        // win32 play back and recording varialbes
        uint INP_BUFFER_SIZE = 16384;
        public const int CALLBACK_FUNCTION = 0x0030000;
        const int CALLBACK_WINDOW = 0x0010000;
        bool endRecording = false;
        public static WAVEHDR pWaveHdr1, pWaveHdr2;
        public delegate void AudioRecordingDelegate(IntPtr deviceHandle, uint message, IntPtr instance, ref WAVEHDR wavehdr, IntPtr reserved2);
        public delegate void AudioPlayBackDelegate(IntPtr deviceHandle, uint message, IntPtr instance, ref WAVEHDR wavehdr, IntPtr reserved2);
        private static uint sampleLength = 0;
        private byte[] samples = new byte[0];
        private AudioRecordingDelegate waveIn;
        private AudioPlayBackDelegate waveOut;
        public static IntPtr handle;
        public static IntPtr handle2;
        private GCHandle headerPin;
        private GCHandle bufferPin;
        private byte[] buffer;
        private uint bufferLength;
        private Form1 form;

        // C# playback variables
        SoundPlayer player;

        // Header data passed into a WaveReader object
        byte[] chunkID = new byte[] { 0x52, 0x49, 0x46, 0x46 };
        byte[] riffType = new byte[] { 0x57, 0x41, 0x56, 0x45 };
        byte[] fmtID = new byte[] { 0x66, 0x6d, 0x74, 0x20 };
        int fmtSize = 16;
        int fmtCode = 1;
        int channels = 1;
        int sampleRate = 11025;
        int fmtAvgBPS = 11025;
        int fmtBlockAlign = 1;
        int bitDepth = 8;
        byte[] dataID = new byte[] { 0x64, 0x61, 0x74, 0x61 };

        /*  SetupBuffer
         *
         *  Setups header and buffer used for recording
         * 
         *  Return void
         */
        private void setupBuffer()
        {

            pWaveHdr1 = new WAVEHDR();
            pWaveHdr1.lpData = bufferPin.AddrOfPinnedObject();
            pWaveHdr1.dwBufferLength = INP_BUFFER_SIZE;
            pWaveHdr1.dwBytesRecorded = 0;
            pWaveHdr1.dwUser = IntPtr.Zero;
            pWaveHdr1.dwFlags = 0;
            pWaveHdr1.dwLoops = 1;
            pWaveHdr1.lpNext = IntPtr.Zero;
            pWaveHdr1.reserved = (System.IntPtr)null;

            int i = waveInPrepareHeader(handle, ref pWaveHdr1, Convert.ToUInt32(Marshal.SizeOf(pWaveHdr1)));
            if (i != 0)
            {
                this.Text = "Error: waveInPrepare " + i.ToString();
                return;
            }
 
             i = waveInAddBuffer(handle, ref pWaveHdr1, Convert.ToUInt32(Marshal.SizeOf(pWaveHdr1)));
            if (i != 0)
            {
                this.Text = "Error: waveInAddrBuffer";
                return;
            }

        }

        /*  SetupWaveIn
         *
         *  Setups and starts recording, globals used to pass data into a WaveReader object
         * 
         *  Return void
         */
        private void setupWaveIn()
        {
            waveIn = this.callbackWaveIn;
            handle = new IntPtr();
            WAVEFORMAT format;
            fmtCode = format.wFormatTag = 1;
            channels = format.nChannels = 1;
            format.nSamplesPerSec = 11025;
            sampleRate = (int)format.nSamplesPerSec;
            bitDepth = format.wBitsPerSample = 8;
            fmtBlockAlign = format.nBlockAlign = 1;// Convert.ToUInt16(format.nChannels * format.wBitsPerSample);
            format.nAvgBytesPerSec = 11025;
            fmtAvgBPS = (int)format.nAvgBytesPerSec;//format.nSamplesPerSec * format.nBlockAlign;
            bufferLength = 16384;// format.nAvgBytesPerSec / 800;
            format.cbSize = 0;

            buffer = new byte[bufferLength];
            bufferPin = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            //4294967295 = WAVE_MAPPER aka unsigned int (-1)
            int i = waveInOpen(ref handle, 4294967295, ref format, Marshal.GetFunctionPointerForDelegate(waveIn), 0, CALLBACK_FUNCTION);           
            if (i != 0)
            {
                this.Text = "Error: waveInOpen";
                return;
            }

            setupBuffer();
            i = waveInStart(handle);
            if (i != 0)
            {
                this.Text = "Error: waveInStart" + i;
                return;
            }
            SystemSounds.Beep.Play();
        }

        /*
         * callBackWaveOut
         *
         * Function mimicks a win32 callback function, mostaly a dummy function, 
         * rebuilds pWaveHdr when called and correct message is recieved
         * 
         * Parameter: 
         *      IntPtr deviceHandle
         *      uint message
         *      IntPtr instance
         *      WAVEHDR wavehdr
         *      IntPtr reserved2
         *
         * return void
         */
        public void callbackWaveOut(IntPtr deviceHandle, uint message, IntPtr instance, ref WAVEHDR wavehdr, IntPtr reserved2)
        {
            if (message == 0x3BB) // WOM_OPEN
            {
                headerPin = GCHandle.Alloc(samples, GCHandleType.Pinned);
                pWaveHdr1.lpData = headerPin.AddrOfPinnedObject();
                pWaveHdr1.dwBufferLength = (uint)samples.Length;
                pWaveHdr1.dwBytesRecorded = 0;
                pWaveHdr1.dwUser = IntPtr.Zero;
                pWaveHdr1.dwFlags = 0x00000004 | 0x00000008;
                pWaveHdr1.dwLoops = 1;
                pWaveHdr1.lpNext = IntPtr.Zero;
                pWaveHdr1.reserved = IntPtr.Zero;
            }
        }

        /*
         * callBackWaveIn
         *
         * Function mimicks a win32 callback function, handles recording
         * 
         * Parameter: 
         *      IntPtr deviceHandle
         *      uint message
         *      IntPtr instance
         *      WAVEHDR wavehdr
         *      IntPtr reserved2
         *
         * return void
         */
        public void callbackWaveIn(IntPtr deviceHandle, uint message, IntPtr instance, ref WAVEHDR wavehdr, IntPtr reserved2)
        {
            int i = 0;
     
            if (message == 0x3BF)//WIM_CLOSE
            {
 
            }else if (message == 958)//WIM_OPEN
            {
                return;
            }else if (message == 0x3C0){//WIM_DATA
              {
                    if (wavehdr.dwBytesRecorded > 0)
                    {
                        byte[] temp = new byte[sampleLength + wavehdr.dwBytesRecorded];
                        Array.Copy(samples, temp, sampleLength);
                       
                        Array.Copy(buffer, 0, temp, sampleLength,wavehdr.dwBytesRecorded);
                        sampleLength += wavehdr.dwBytesRecorded;
                        samples = temp;

                        i = waveInUnprepareHeader(deviceHandle, ref wavehdr, Convert.ToUInt32(Marshal.SizeOf(wavehdr)));
                        if (i != 0)
                        {
                            this.Text = "Error: waveInUnprepareHeader " + i;
                        }
                        setupBuffer();  
                    }
                    if (endRecording)
                    {
                        return;
                    }
                    }
            } 
            else//SHOULD NOT GET HERE
                endRecording = true; 
        }

        /*
         * int to byte Array method
         *
         *    converts an int to an array of bytes, used for writting header data.
         * 
         * Parameter:
         *          int     int to be converted into a byte array
         *
         * Return byte[]
         */
        private byte[] intToByteArr(int i)
        {
            byte[] tmp = BitConverter.GetBytes(i);
            return tmp;
        }

        /*
         * RecordDialog
         *
         *    Constructor for the record dialog box
         * 
         * Parameter:
         *          Form1 f
         *
         * Return void
         */
        public RecordDialog(Form1 f)
        {
            InitializeComponent();
            form = f;
            playBtn.Enabled = false;
            endPlayBtn.Enabled = false;
            endRecordBtn.Enabled = false;
        }

        /*
         * RecordDialog_Load
         *
         *  Unused 
         * 
         *  Return void
         */
        private void RecordDialog_Load(object sender, EventArgs e)
        {
            
        }

        /*
         * playBtn_Click
         *
         *    Listener for the play button, plays back recorded audio when clicked
         * 
         * Parameter:
         *          sender
         *          e
         *
         * Return void
         */
        public void playBtn_Click(object sender, EventArgs e)
        {

            // win32 code
            //waveOut = this.callbackWaveOut;
            //play(waveOut);

            // C# code
             if (samples.Length != 0)
             {
                 byte[] tmp = new byte[samples.Length + 44];

                 Buffer.BlockCopy(chunkID, 0, tmp, 0, 4);
                 Buffer.BlockCopy(intToByteArr(tmp.Length - 8), 0, tmp, 4, 4);
                 Buffer.BlockCopy(riffType, 0, tmp, 8, 4);
                 Buffer.BlockCopy(fmtID, 0, tmp, 12, 4);
                 Buffer.BlockCopy(intToByteArr(fmtSize), 0, tmp, 16, 4);
                 Buffer.BlockCopy(intToByteArr(fmtCode), 0, tmp, 20, 2);
                 Buffer.BlockCopy(intToByteArr(channels), 0, tmp, 22, 2);
                 Buffer.BlockCopy(intToByteArr(sampleRate), 0, tmp, 24, 4);
                 Buffer.BlockCopy(intToByteArr(fmtAvgBPS), 0, tmp, 28, 4);
                 Buffer.BlockCopy(intToByteArr(fmtBlockAlign), 0, tmp, 32, 2);
                 Buffer.BlockCopy(intToByteArr(bitDepth), 0, tmp, 34, 2);
                 Buffer.BlockCopy(dataID, 0, tmp, 36, 4);
                 Buffer.BlockCopy(intToByteArr(samples.Length), 0, tmp, 40, 4);

                 Buffer.BlockCopy(samples, 0, tmp, 44, samples.Length);

                 using (System.IO.MemoryStream ms = new System.IO.MemoryStream(tmp))
                 {
                     player = new System.Media.SoundPlayer(ms);
                     player.Play();
                     //System.IO.File.WriteAllBytes("TEST.wav", tmp);
                 }
             }
        }

        /*
         * play
         *
         * Function uses win32 to play the audio imported/recored by the progam
         * 
         * Parameter: 
         *      RecordDialog.AudioPlayBackDeleagate waveOut
         *
         * return void
         */
        public static void play(AudioPlayBackDelegate waveOut)
        {
            
            handle2 = new IntPtr();

            WAVEFORMAT format;
            format.wFormatTag = 1;
            format.nChannels = 1;
            format.nSamplesPerSec = 11025;
            format.wBitsPerSample = 8;
            format.nBlockAlign = 1;
            format.nAvgBytesPerSec = 11025;
            format.cbSize = 0;
            int i = -1;

            i = waveOutOpen(ref handle2, 4294967295, ref format, Marshal.GetFunctionPointerForDelegate(waveOut), 0, CALLBACK_FUNCTION);
            if (i != 0)
            {
               // this.Text = "Error: waveOutOpen";
                return;
            }

            i = waveOutPrepareHeader(handle2, ref pWaveHdr1, Convert.ToUInt32(Marshal.SizeOf(pWaveHdr1)));
            if (i != 0)
            {
                return;
            }

            i = waveOutWrite(handle2, ref pWaveHdr1, Convert.ToUInt32(Marshal.SizeOf(pWaveHdr1)));
            if (i != 0)
            {
                return;
            }
        }

        /*
         * endPlayBtn_Click
         *
         *    Listener for the end playback button, stops playback if playing and if wave data exists
         * 
         * Parameter:
         *          sender
         *          e
         *
         * Return void
         */
        private void endPlayBtn_Click(object sender, EventArgs e)
        {
            // C# code
            if(player != null)
            {
                player.Stop();
            }

            // win32 code
            //waveOutReset(handle2);
        }

        /*
         * recordBtn_Click
         *
         *    Listener for the record button, starts recording of audio using default recording device.
         * 
         * Parameter:
         *          sender
         *          e
         *
         * Return void
         */
        unsafe void recordBtn_Click(object sender, EventArgs e)
        {
            setupWaveIn();
            recordBtn.Enabled = false;
            playBtn.Enabled = false;
            endPlayBtn.Enabled = false;
            endRecordBtn.Enabled = true;
        }

        /*
         * endRecordBtn_Click
         *
         *    Listener for the listener for the end record button, stops recording and places data into the main program
         * 
         * Parameter:
         *          sender
         *          e
         *
         * Return void
         */
        private void endRecordBtn_Click(object sender, EventArgs e)
        {
           
            uint ii = waveInStop(handle);
            endRecording = true;
            Thread.Sleep(200);
            ii = waveInReset(handle);
            ii = waveInClose(handle);

            byte[] tmp = new byte[samples.Length + 44];

            Buffer.BlockCopy(chunkID, 0, tmp, 0, 4);
            Buffer.BlockCopy(intToByteArr(tmp.Length - 8), 0, tmp, 4, 4);
            Buffer.BlockCopy(riffType, 0, tmp, 8, 4);
            Buffer.BlockCopy(fmtID, 0, tmp, 12, 4);
            Buffer.BlockCopy(intToByteArr(fmtSize), 0, tmp, 16, 4);
            Buffer.BlockCopy(intToByteArr(fmtCode), 0, tmp, 20, 2);
            Buffer.BlockCopy(intToByteArr(channels), 0, tmp, 22, 2);
            Buffer.BlockCopy(intToByteArr(sampleRate), 0, tmp, 24, 4);
            Buffer.BlockCopy(intToByteArr(fmtAvgBPS), 0, tmp, 28, 4);
            Buffer.BlockCopy(intToByteArr(fmtBlockAlign), 0, tmp, 32, 2);
            Buffer.BlockCopy(intToByteArr(bitDepth), 0, tmp, 34, 2);
            Buffer.BlockCopy(dataID, 0, tmp, 36, 4);
            Buffer.BlockCopy(intToByteArr(samples.Length), 0, tmp, 40, 4);

            Buffer.BlockCopy(samples, 0, tmp, 44, samples.Length);

            double[] left, right;
            form.wave = new WaveReader();
            form.wave.recWav(tmp, out left, out right);

            form.left = left;
            form.right = right;

            this.Invoke(form.inv, new object[] { form.left });

            if (form.left.Length < 500)
            {
                form.offset = 0;
                form.range = left.Length;
            }
            else
            {
                form.offset = left.Length / 2 - 250;
                form.range = 500;
            }
            form.window();

            recordBtn.Enabled = true;
            endRecordBtn.Enabled = false;
            playBtn.Enabled = true;
            endPlayBtn.Enabled = true;
        }
    }

}