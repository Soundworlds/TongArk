//
//
// Licensed under the MIT license.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Timers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Exocortex.DSP;
using FMOD;
using GeneticAlgorythm;
using System.Threading;
using GeneticAlgorithm_Waves;
using NAudio.Wave;
using NAudio.FileFormats;
using NAudio.CoreAudioApi;
using NAudio;
using System.Diagnostics;

namespace TongArk
{
    public partial class Agent //: System.Windows.Window
    {
        //struct volumeData
        //{
        //    public float newVolume;
        //    public float oldVolume;
        //    public int channelNum;
        //    public int steps;
        //    public float oneStep;
        //}

        public int[] targetData;
        public int[] source_data;
        public int[] currentVolume;
        public int oscMaxCount;
        public int soundScaleMax = 84;
        public int[] oscBandToPositionInScale;

        public int FFT_WINDOW_SIZE = 4096;
        int SAMPLE_RATE = 44100;

        GeneticAlgorythm.GA gaClass; 

        //FMOD
        FMOD.RESULT Fresult;

        FMOD.System Fsystem;
        IntPtr intPtrSystem;
        FMOD.DSP[] Fdsp_Highpass;
        FMOD.DSP[] Fdsp_Lowpass;
        FMOD.Channel[] Fchannel;
        IntPtr[] intPtr_DSP_Highpass;
        IntPtr[] intPtr_DSP_Lowpass;
        IntPtr[] intPtr_Channel;
        IntPtr[] intPtr_Sound;
        Sound[] Fsound;
        ChannelGroup[] FChannelGroup;

        FMOD.System Fsystem_R;
        IntPtr intPtrSystem_R;
        FMOD.DSP[] Fdsp_Highpass_R;
        FMOD.DSP[] Fdsp_Lowpass_R;
        FMOD.Channel[] Fchannel_R;
        IntPtr[] intPtr_DSP_Highpass_R;
        IntPtr[] intPtr_DSP_Lowpass_R;
        IntPtr[] intPtr_Channel_R;
        IntPtr[] intPtr_Sound_R;
        Sound[] Fsound_R;
        ChannelGroup[] FChannelGroup_R;

        float[] dspHighpass_Cutoff = new float[8];
        float[] dspLowpass_Cutoff = new float[8];
        //FMOD END

        WaveFile sourceWaveFile;
        WaveFile destWaveFile;
        string sourceWaveFilePath = @"C:\source.wav";
        string destWaveFilePath = @"C:\dest.wav";

        bool isExit = false;

        int dataSkip = 0;
        //int currentDataStep = 0;

        int NoMusicSignalTimesTotal = 0;

        //PATTERN
        int soundPatternsTotal = 10;
        int currentSoundPattern = 0;
        int emotionPatternsTotal = 3;
        int currentEmotionPattern = 0;
        //END PATTERN

        public struct EmotionList
        {
            public float value;
            public string name;
        }

        EmotionList[] emotionValue = new EmotionList[8];

        string[] emotionsPile;

        int dirMaxNumber = 27;
        int lyricsMaxNumber = 4;
        int currentLyricsFragment = 0;

        bool loopBiDi = true;
        int threadSleepTimeMin = 50;
        int threadSleepTimeMax = 400;
        int countDownValue = 0;

        private bool loadDone;

        public bool isLoadDone
        {
            get { return loadDone; }
            private set { loadDone = value; }
        }

        public string SoundPatternFilePath = Properties.Settings.Default.SoundPatternFile;
        public string SoundElementsFolderPath = Properties.Settings.Default.SoundElementsFolder;


        //private void frmMain_FormClosing(Object sender, FormClosingEventArgs e)
        //{
        //    MessageBox.Show("YES!");
        //    isExit = true;
        //}

        //private void frmMain_Closing(Object sender, FormClosingEventArgs e)
        //{
        //    MessageBox.Show("YES!");
        //    isExit = true;
        //}


        private void soundStart()
        {
            if (isExit) { return; }

            int i;
            Random r = new Random();

            for (i = 0; i < soundScaleMax; ++i)
            {
                Fchannel[i].setPaused(false);
                Fchannel_R[i].setPaused(false);

                Fsystem.update();
                Fsystem_R.update();

                Thread.Sleep(r.Next(threadSleepTimeMin, threadSleepTimeMax));
            }
        }

        private void oneSoundStart(int index)
        {
           
            int i;
            Random r = new Random();

            Thread.Sleep(r.Next(threadSleepTimeMin, threadSleepTimeMax));

            if (isExit) { return; }

            Fchannel[index].setPaused(false);
            Fchannel_R[index].setPaused(false);

            Fsystem.update();
            Fsystem_R.update();
        }

        public void DeInit()
        {
            isExit = true;
            FMOD_DeInitialize();
        }

        private void FMOD_Initialize()
    {
        Fresult = FMOD.Factory.System_Create(out Fsystem);
        ERRCHECK(Fresult, "create system");

        Fresult = Fsystem.setOutput(OUTPUTTYPE.DSOUND);
        ERRCHECK(Fresult, "setOutput");
        
        Fresult = Fsystem.init(soundScaleMax, FMOD.INITFLAGS.NORMAL, intPtrSystem);
        ERRCHECK(Fresult, "init");
        
    }

        private void FMOD_DeInitialize()
    {
        Fresult = Fsystem.close();
        ERRCHECK(Fresult, "close");

        Fresult = Fsystem_R.close();
        ERRCHECK(Fresult, "close");
    }

        private void FMOD_Initialize_R()
    {
        Fresult = FMOD.Factory.System_Create(out Fsystem_R);
        ERRCHECK(Fresult, "create system");

        Fresult = Fsystem_R.setOutput(OUTPUTTYPE.WAVWRITER);
        ERRCHECK(Fresult, "setOutput");

        Fresult = Fsystem_R.init(soundScaleMax, FMOD.INITFLAGS.NORMAL, intPtrSystem_R);
        ERRCHECK(Fresult, "init");
    }


        private void ERRCHECK(FMOD.RESULT Fresult, string comment )
    {
        if (Fresult != FMOD.RESULT.OK) 
        {
            MessageBox.Show("FMOD error in " + comment + ": " + Fresult + "-" + FMOD.Error.String(Fresult));
        }
    }


        public void Init(int populationSize = 1000, float crossoverValue = 0.8f, float mutationValue = 0.3f, int generationsTotal = 2, int mutationStep = 5)
        {

            int[] dirPattern = new int[3];

            List<MainWindow.EmotionList> el = new List<MainWindow.EmotionList>() { };
            
            for (int i = 0; i < 8; i++)
            {
                MainWindow.EmotionList e = new MainWindow.EmotionList { name = "neutral", value = 0 };
                el.Add(e);
            }

            //fill with default
            emotionsPile = new string[soundScaleMax];
            for (int i = 0; i < soundScaleMax; i++)
            {
                emotionsPile[i] = "neutral";
            }

            int ii;
            int nextNumber;
            float[] arrPitch = new float[11];
            double startTone = 27.5f;

            double intervalSecond = 1.059463055f;
            float microTones = 6.0f;

            source_data = new int[FFT_WINDOW_SIZE];

            oscMaxCount = FFT_WINDOW_SIZE / 2;
            oscBandToPositionInScale = new int[oscMaxCount];

            //Happyness
            dspHighpass_Cutoff[0] = 523;
            dspLowpass_Cutoff[0] = 4184;
            //Sadness
            dspHighpass_Cutoff[1] = 10;
            dspLowpass_Cutoff[1] = 22000;
            //Contempt
            dspHighpass_Cutoff[2] = 10;
            dspLowpass_Cutoff[2] = 2092;
            //Fear
            dspHighpass_Cutoff[3] = 2092;
            dspLowpass_Cutoff[3] = 22000;
            //Disgust
            dspHighpass_Cutoff[4] = 262;
            dspLowpass_Cutoff[4] = 2092;
            //Surprise
            dspHighpass_Cutoff[5] = 523;
            dspLowpass_Cutoff[5] = 22000;
            //Anger
            dspHighpass_Cutoff[6] = 10;
            dspLowpass_Cutoff[6] = 262;
            //Neutral
            dspHighpass_Cutoff[7] = 10;
            dspLowpass_Cutoff[7] = 22000;

            intPtr_DSP_Highpass = new IntPtr[soundScaleMax];
            intPtr_DSP_Lowpass = new IntPtr[soundScaleMax];
            intPtr_Channel = new IntPtr[soundScaleMax];
            intPtr_Sound = new IntPtr[soundScaleMax];
            Fdsp_Highpass = new FMOD.DSP[soundScaleMax];
            Fdsp_Lowpass = new FMOD.DSP[soundScaleMax];
            Fchannel = new FMOD.Channel[soundScaleMax];
            Fsound = new Sound[soundScaleMax];
            FChannelGroup = new ChannelGroup[soundScaleMax];

            intPtr_DSP_Highpass_R = new IntPtr[soundScaleMax];
            intPtr_DSP_Lowpass_R = new IntPtr[soundScaleMax];
            intPtr_Channel_R = new IntPtr[soundScaleMax];
            intPtr_Sound_R = new IntPtr[soundScaleMax];
            Fdsp_Highpass_R = new FMOD.DSP[soundScaleMax];
            Fdsp_Lowpass_R = new FMOD.DSP[soundScaleMax];
            Fchannel_R = new FMOD.Channel[soundScaleMax];
            Fsound_R = new Sound[soundScaleMax];
            FChannelGroup_R = new ChannelGroup[soundScaleMax];

            gaClass = new GA();
            targetData = new int[oscMaxCount];
            currentVolume = new int[oscMaxCount];

            intPtrSystem = new IntPtr();
            Fsystem = new FMOD.System (intPtrSystem);
            Fresult = new FMOD.RESULT();

            FMOD_Initialize();

            intPtrSystem_R = new IntPtr();
            Fsystem_R = new FMOD.System(intPtrSystem_R);
            Fresult = new FMOD.RESULT();

            FMOD_Initialize_R();

            double testTone = 55.0f;
            double currentTone = startTone;

            int number = 0;
            int index = 0;

            for (int i = 0; i < soundScaleMax; ++i)
                {

                //PLAYING sound creation
                Fdsp_Highpass[i] = new FMOD.DSP(intPtr_DSP_Highpass[i]);
                Fdsp_Lowpass[i] = new FMOD.DSP(intPtr_DSP_Lowpass[i]);
                Fchannel[i] = new FMOD.Channel(intPtr_Channel[i]);
                Fresult = Fsystem.createChannelGroup("CG" + i, out FChannelGroup[i]);

                //create sound
                Random r = new Random();
                int dir = 0;
                dir = r.Next(dirPattern[0], dirPattern[2]);
                string path = SoundElementsFolderPath + "/" + 1 + "/";
                string filename = i + ".wav";
                path += filename;

                Fsound[i] = new Sound(intPtr_Sound[i]);
                if (loopBiDi)
                {
                    Fresult = Fsystem.createSound(path, MODE.LOOP_BIDI | MODE._2D, out Fsound[i]);
                }
                else
                {
                    Fresult = Fsystem.createSound(path, MODE.LOOP_NORMAL | MODE._2D, out Fsound[i]);
                }
                Fresult = Fsystem.playSound(Fsound[i], FChannelGroup[i], false, out Fchannel[i]);

                //create and set lowpass and highpass dsps
                index = GetDspFilterCutoffIndex(emotionsPile[i]);

                Fresult = Fsystem.createDSPByType(DSP_TYPE.HIGHPASS, out Fdsp_Highpass[i]);
                Fresult = Fchannel[i].addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, Fdsp_Highpass[i]);
                Fresult = Fdsp_Highpass[i].setParameterFloat((int)FMOD.DSP_HIGHPASS.CUTOFF, dspHighpass_Cutoff[index]);
                Fresult = Fsystem.playDSP(Fdsp_Highpass[i], FChannelGroup[i], true, out Fchannel[i]);


                Fresult = Fsystem.createDSPByType(DSP_TYPE.LOWPASS, out Fdsp_Lowpass[i]);
                Fresult = Fchannel[i].addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, Fdsp_Lowpass[i]);
                Fresult = Fdsp_Lowpass[i].setParameterFloat((int)FMOD.DSP_LOWPASS_SIMPLE.CUTOFF, dspLowpass_Cutoff[index]);
                Fresult = Fsystem.playDSP(Fdsp_Lowpass[i], FChannelGroup[i], true, out Fchannel[i]);


                Fresult = FChannelGroup[i].setVolume(0.0f);
                Fresult = Fchannel[i].setPaused(false);

                //END PLAYING sound creation

                //RECORDING sound creation
                Fdsp_Highpass_R[i] = new FMOD.DSP(intPtr_DSP_Highpass_R[i]);
                Fdsp_Lowpass_R[i] = new FMOD.DSP(intPtr_DSP_Lowpass_R[i]);
                Fchannel_R[i] = new FMOD.Channel(intPtr_Channel_R[i]);
                Fresult = Fsystem_R.createChannelGroup("CG_R" + i, out FChannelGroup_R[i]);

                //create sound
                Fsound_R[i] = new Sound(intPtr_Sound_R[i]);
                if (loopBiDi)
                {
                    Fresult = Fsystem_R.createSound(path, MODE.LOOP_BIDI | MODE._2D, out Fsound_R[i]);
                }
                else
                {
                    Fresult = Fsystem_R.createSound(path, MODE.LOOP_NORMAL | MODE._2D, out Fsound_R[i]);
                }
                Fresult = Fsystem_R.playSound(Fsound_R[i], FChannelGroup_R[i], false, out Fchannel_R[i]);

                //create and set lowpass and highpass dsps
                Fresult = Fsystem_R.createDSPByType(DSP_TYPE.HIGHPASS, out Fdsp_Highpass_R[i]);
                Fresult = Fchannel_R[i].addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, Fdsp_Highpass_R[i]);
                Fresult = Fdsp_Highpass_R[i].setParameterFloat((int)FMOD.DSP_HIGHPASS.CUTOFF, dspHighpass_Cutoff[index]);
                Fresult = Fsystem_R.playDSP(Fdsp_Highpass_R[i], FChannelGroup_R[i], true, out Fchannel_R[i]);


                Fresult = Fsystem_R.createDSPByType(DSP_TYPE.LOWPASS, out Fdsp_Lowpass_R[i]);
                Fresult = Fchannel_R[i].addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, Fdsp_Lowpass_R[i]);
                Fresult = Fdsp_Lowpass_R[i].setParameterFloat((int)FMOD.DSP_LOWPASS_SIMPLE.CUTOFF, dspLowpass_Cutoff[index]);
                Fresult = Fsystem_R.playDSP(Fdsp_Lowpass_R[i], FChannelGroup_R[i], true, out Fchannel_R[i]);


                Fresult = FChannelGroup_R[i].setVolume(0.0f);
                Fresult = Fchannel_R[i].setPaused(false);

                //END RECORDING sound creation

                number++;
            } 

          
            Fsystem.update();
            Fsystem_R.update();


            Thread t;
            t = new Thread(new ThreadStart(soundStart));
            t.Start();

            
            int[] target = new int[oscMaxCount];
            int[] source = new int[oscMaxCount];

            for (int i = 0; i<oscMaxCount; ++i)
            {
                target[i] = 0;
                source[i] = 0;
            }

            gaClass.Init(populationSize, crossoverValue, mutationValue, generationsTotal, source, target);
            gaClass.SetMutationStep (mutationStep);

            SetSoundScale();

            
        }


        public void SetAgentVolume()
        {

            
            float volume = 0.0f;
            float oldVolume = 0.0f;
            int steps = 100;
            float oneStep = 0f;
            int bestFreq = 0;
            float bestVolume = 0.0f;

            for (int i = 0; i < 400; ++i)
            {
                if (isExit) { break; }

                if (currentVolume[i] < 0) currentVolume[i] = 0;

                    volume = currentVolume[i] / 50.0f;

               
                    Fresult = FChannelGroup[oscBandToPositionInScale[i]].setVolume(volume);
                    Fresult = FChannelGroup_R[oscBandToPositionInScale[i]].setVolume(volume);
                    Application.DoEvents();

                    if (bestVolume < volume)
                    {
                        bestVolume = volume;
                        bestFreq = i;
                    }
            }

            double soundFreq = 4.087899002222683f * 8;
            double intervalSecond = 1.059463055f;

            for (int i = 0; i < oscBandToPositionInScale[bestFreq]; i++)
            {
                soundFreq *= intervalSecond;
            }

            Fsystem.update();
            Fsystem_R.update();

            dataSkip++;
        }


        public void RunEpoch(byte[] buffer)
        {

            //RUN EPOCH

            currentVolume = gaClass.runEpoch();
            SetAgentVolume();

            //SET TARGET

            int dataSum = 0;
            for (int i = 0; i < FFT_WINDOW_SIZE; i++)
            {
                source_data[i] = BitConverter.ToInt16(buffer, i * 2);
                Application.DoEvents();
                dataSum += Math.Abs(source_data[i]);
            }


            double bestBand = 0.0f;
            double bestFreq = 0.0f;
            for (int i = 0; i < oscMaxCount; i++)
            {
                if (bestBand < Math.Abs(targetData[i]))
                {
                    bestBand = Math.Abs(targetData[i]);
                    bestFreq = i * SAMPLE_RATE / FFT_WINDOW_SIZE;
                }
            }


            ComplexF[] sourceComplexData = new ComplexF[FFT_WINDOW_SIZE];
            ComplexF[] destComplexData = new ComplexF[FFT_WINDOW_SIZE];


            for (int i = 0; i < FFT_WINDOW_SIZE; ++i)
            {
                // Fill the complex data
                sourceComplexData[i].Re = (float)source_data[i]; // Add your real part here
                sourceComplexData[i].Im = 0; // Add your imaginary part here
            }

            // FFT the time domain data to get frequency domain data
            Fourier.FFT(sourceComplexData, FourierDirection.Forward);

            for (int i = 0; i < FFT_WINDOW_SIZE / 2; ++i)
            {
                targetData[i] = (int)Math.Abs((sourceComplexData[i].Re + 300000) / 420000 * 256 - 182);
            }


            //////if (dataSum > 1000000)
            //////{
            //////    NoMusicSignalTimesTotal = 0;
            //////}
            //////else
            //////{
            //////    NoMusicSignalTimesTotal++;
            //////}

            //////if (NoMusicSignalTimesTotal > 30)
            //////{
            //////    int[] zeroTarget = new int[oscMaxCount];
            //////    gaClass.setTarget(zeroTarget);
            //////}
            //////else
            //////{
                gaClass.setTarget(targetData);
            
            //////}
        }

        public void GACrossoverValue(int crossoverValue)
        {
            float newCrossoverValue = (float)(crossoverValue / 100);
            gaClass.SetCrossover(newCrossoverValue);
        }

        public void GAMutationValue(int mutationValue)
        {
            float newMutationValue = (float)(mutationValue / 100);
            gaClass.SetMutation(newMutationValue);
        }

        public void GAMutationStepValue(int mutationStepValue)
        {
            gaClass.SetMutationStep(mutationStepValue);
        }


        private void SetSoundScale()
        {
            double intervalSecond = 1.059463055f;
            double microTones = 6.0f;
            double startTone = 4.087899002222683f * 8;
            double testTone = 4.087899002222683f * 8;
            double currentTone = startTone;

            double[] fft_array = new double[oscMaxCount];
            double[] scale_array = new double[soundScaleMax];


            for (int j = 0; j < oscMaxCount; ++j)
            {
                currentTone = j * SAMPLE_RATE / FFT_WINDOW_SIZE;
                fft_array[j] = currentTone;
            }

            for (int j = 0; j < soundScaleMax; ++j)
            {
                scale_array[j] = testTone;
                testTone *= intervalSecond;
            }


            for (int j = 0; j < oscMaxCount; ++j)
            {
                for (int i = 0; i < soundScaleMax - 1; ++i)
                {
                    if (fft_array[j] > scale_array[i] && fft_array[j] < scale_array[i + 1])
                    {
                        double compare1 = 0.0f;
                        double compare2 = 0.0f;
                        compare1 = fft_array[j] - scale_array[i];
                        compare2 = scale_array[i + 1] - fft_array[j];
                        if (compare2 > compare1)
                        {
                            oscBandToPositionInScale[j] = i;
                            break;
                        }
                        else
                        {
                            oscBandToPositionInScale[j] = i + 1;
                            break;
                        }
                    }
                }
            }

            FileStream fs = new FileStream(@"D:\test.txt", FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            string s = "";
            for (int i = 0; i < oscMaxCount; i++)
            {
                s = i + " | " + (double)fft_array[i] + " | " + scale_array[oscBandToPositionInScale[i]] + " | " + oscBandToPositionInScale[i] + Environment.NewLine;
                bw.Write(s);
            }
            bw.Close();
            fs.Close();
        }

        private void ChangeAllSounds (string newPath)
        {
            //int number = 0;
            int index = 0;

            for (int i = 0; i < soundScaleMax; ++i)
            {
                Fchannel[i].setPaused(true);
                Fresult = Fsound[i].release();

                Fchannel_R[i].setPaused(true);
                Fresult = Fsound_R[i].release();

                string path = newPath;
                string filename = i + ".wav";
                path += filename;

                //create PLAYING sound
                Fsound[i] = new Sound(intPtr_Sound[i]);
                if (loopBiDi)
                {
                    Fresult = Fsystem.createSound(path, MODE.LOOP_BIDI | MODE._2D, out Fsound[i]);
                }
                else
                {
                    Fresult = Fsystem.createSound(path, MODE.LOOP_NORMAL | MODE._2D, out Fsound[i]);
                }
                Fresult = Fsystem.playSound(Fsound[i], FChannelGroup[i], true, out Fchannel[i]);
                //Fchannel[i].setPaused(false);
                //END PLAYING sound creating

                //create RECORDING sound
                Fsound_R[i] = new Sound(intPtr_Sound_R[i]);
                if (loopBiDi)
                {
                    Fresult = Fsystem_R.createSound(path, MODE.LOOP_BIDI | MODE._2D, out Fsound_R[i]);
                }
                else
                {
                    Fresult = Fsystem_R.createSound(path, MODE.LOOP_NORMAL | MODE._2D, out Fsound_R[i]);
                }
                Fresult = Fsystem_R.playSound(Fsound_R[i], FChannelGroup_R[i], true, out Fchannel_R[i]);
                //Fchannel_R[i].setPaused(false);
                //END RECORDING sound creating

                index = GetDspFilterCutoffIndex(emotionsPile[i]);

                Fresult = Fdsp_Highpass[i].setParameterFloat((int)FMOD.DSP_HIGHPASS.CUTOFF, dspHighpass_Cutoff[index]);
                Fresult = Fdsp_Lowpass[i].setParameterFloat((int)FMOD.DSP_LOWPASS_SIMPLE.CUTOFF, dspLowpass_Cutoff[index]);
                Fchannel[i].setPaused(true);

                Fresult = Fdsp_Highpass_R[i].setParameterFloat((int)FMOD.DSP_HIGHPASS.CUTOFF, dspHighpass_Cutoff[index]);
                Fresult = Fdsp_Lowpass_R[i].setParameterFloat((int)FMOD.DSP_LOWPASS_SIMPLE.CUTOFF, dspLowpass_Cutoff[index]);
                Fchannel_R[i].setPaused(true);

                //Fsystem.update();
                //Fsystem_R.update();
            }


            Thread t;
            t = new Thread(new ThreadStart(soundStart));
            t.Start();

            Fsystem.update();
            Fsystem_R.update();
        }

        private void SetReverb(int index)
        {
            REVERB_PROPERTIES rpr = new REVERB_PROPERTIES();

            switch (index)
            {
                case 0:
                    rpr = PRESET.OFF();
                    break;
                case 1:
                    rpr = PRESET.GENERIC();
                    break;
                case 2:
                    rpr = PRESET.PADDEDCELL();
                    break;
                case 3:
                    rpr = PRESET.ROOM();
                    break;
                case 4:
                    rpr = PRESET.BATHROOM();
                    break;
                case 5:
                    rpr = PRESET.LIVINGROOM();
                    break;
                case 6:
                    rpr = PRESET.STONEROOM();
                    break;
                case 7:
                    rpr = PRESET.AUDITORIUM();
                    break;
                case 8:
                    rpr = PRESET.CONCERTHALL();
                    break;
                case 9:
                    rpr = PRESET.CAVE();
                    break;
                case 10:
                    rpr = PRESET.ARENA();
                    break;
                case 11:
                    rpr = PRESET.HANGAR();
                    break;
                case 12:
                    rpr = PRESET.CARPETTEDHALLWAY();
                    break;
                case 13:
                    rpr = PRESET.HALLWAY();
                    break;
                case 14:
                    rpr = PRESET.STONECORRIDOR();
                    break;
                case 15:
                    rpr = PRESET.ALLEY();
                    break;
                case 16:
                    rpr = PRESET.FOREST();
                    break;
                case 17:
                    rpr = PRESET.CITY();
                    break;
                case 18:
                    rpr = PRESET.MOUNTAINS();
                    break;
                case 19:
                    rpr = PRESET.QUARRY();
                    break;
                case 20:
                    rpr = PRESET.PLAIN();
                    break;
                case 21:
                    rpr = PRESET.PARKINGLOT();
                    break;
                case 22:
                    rpr = PRESET.SEWERPIPE();
                    break;
                case 23:
                    rpr = PRESET.UNDERWATER();
                    break;
            }

            Fsystem.setReverbProperties(0, ref rpr);
            Fsystem_R.setReverbProperties(0, ref rpr);

            Fsystem.update();
            Fsystem_R.update();
        }

        private int GetDspFilterCutoffIndex(string emotionName)
        {
            int index = 0;

            switch (emotionName)
            {
                case "happiness":
                    index = 0;
                    break;
                case "sadness":
                    index = 1;
                    break;
                case "contempt":
                    index = 2;
                    break;
                case "fear":
                    index = 3;
                    break;
                case "disgust":
                    index = 4;
                    break;
                case "surprise":
                    index = 5;
                    break;
                case "anger":
                    index = 6;
                    break;
                case "neutral":
                    index = 7;
                    break;
            }

            return index;
        }

        private void SetReverberation(string emotionName)
        {
            REVERB_PROPERTIES rpr = new REVERB_PROPERTIES();
            int listPosition = 0;

            switch (emotionName)
            {
                case "disgust":
                    rpr = PRESET.OFF();
                    listPosition = 0;
                    break;
                case "neutral":
                    rpr = PRESET.GENERIC();
                    listPosition = 1;
                    break;
                case "happiness":
                    rpr = PRESET.AUDITORIUM();
                    listPosition = 7;
                    break;
                case "sadness":
                    rpr = PRESET.CONCERTHALL();
                    listPosition = 8;
                    break;
                case "contempt":
                    rpr = PRESET.HANGAR();
                    listPosition = 11;
                    break;
                case "surprise":
                    rpr = PRESET.STONECORRIDOR();
                    listPosition = 14;
                    break;
                case "fear":
                    rpr = PRESET.QUARRY();
                    listPosition = 19;
                    break;
                case "anger":
                    rpr = PRESET.UNDERWATER();
                    listPosition = 23;
                    break;
            }

        }

        private void EmotionsFillAndShuffle()
        {
            emotionsPile = new string[soundScaleMax];

            //fill with default
            for (int i = 0; i < soundScaleMax; i++)
            {
                emotionsPile[i] = "neutral";
            }

            int counter = 0;

            //fill with emotions names
            for (int i = 0; i < 8; i++)
            {
                if ((int)emotionValue[i].value > 0)
                {
                    for (int j = 0; j < (int)emotionValue[i].value; j++)
                    {
                        emotionsPile[counter] = emotionValue[i].name;
                        counter++;
                    }
                }
            }

            //shuffle
            int NewPos;
            string Hold;
            Random r = new Random();

            for (int i = 0; i < soundScaleMax; i++)
            {
                NewPos = r.Next(0, soundScaleMax);
                Hold = emotionsPile[i];
                emotionsPile[i] = emotionsPile[NewPos];
                emotionsPile[NewPos] = Hold;
            }
        }

        public void ChangeSoundPattern(List<MainWindow.EmotionList> e)
        {

            for (int i = 0; i < 8; i++)
            {
                emotionValue[i].name = e[i].name;
                emotionValue[i].value = e[i].value / 100 * soundScaleMax;
            }

            // set list of emotions
            EmotionsFillAndShuffle();

            // set reverberation type
            SetReverberation(emotionValue[0].name);

            // set new sound pattern
            int[] pattern = new int[3];
            int[] soundsList = new int[soundScaleMax];

            currentSoundPattern++;
            if (currentSoundPattern > soundPatternsTotal) currentSoundPattern = 1;
            pattern = GetPattern(currentSoundPattern);

            string path = "";
            int index = 0;

            // create new sounds list
            for(int i=0; i<3; i++)
            {
                for(int j=0; j<soundScaleMax/3; j++)
                {
                    int pos = (i * soundScaleMax / 3) + j;
                    soundsList[pos] = pattern[i];
                }
            }
            // shuffle list
            int NewPos;
            int Hold;
            Random r = new Random();
            for (int i = 0; i < soundScaleMax; i++)
            {
                NewPos = r.Next(0, soundScaleMax);
                Hold = soundsList[i];
                soundsList[i] = soundsList[NewPos];
                soundsList[NewPos] = Hold;
            }

            // change each sound
            for (int i=0; i<soundScaleMax; i++)
            {
                path = SoundElementsFolderPath + "/" + soundsList[i] + "/";
                string filename = i + ".wav";
                path += filename;
                index = i;
                ChangeOneSound(path, index);
                Application.DoEvents();
            }

            Fsystem.update();
            Fsystem_R.update();
            
        }

        private int[] GetPattern(int index)
        {
            int timer_duration = 0;
            int[] pattern = new int[3];

            FileStream fs = new FileStream(SoundPatternFilePath, FileMode.Open);
            StreamReader sr = new StreamReader(fs);

            for (int i = 0; i < index; i++)
            {
                sr.ReadLine();
                timer_duration = Convert.ToInt32(sr.ReadLine());
                sr.ReadLine();
                pattern[0] = Convert.ToInt32(sr.ReadLine());
                pattern[1] = Convert.ToInt32(sr.ReadLine());
                pattern[2] = Convert.ToInt32(sr.ReadLine());
                sr.ReadLine();
            }
            
            sr.Close();
            fs.Close();

            return pattern;
        }

        private void ChangeOneSound(string newPath, int i)
        {

            Fchannel[i].setPaused(true);
            if (Fsound[i] != null)
            {
                Fresult = Fsound[i].release();
            }

            Fchannel_R[i].setPaused(true);
            if (Fsound_R != null)
            {
                Fresult = Fsound_R[i].release();
            }

            string path = newPath;

            //create PLAYING sound
            Fsound[i] = new Sound(intPtr_Sound[i]);
            if (loopBiDi)
            {
                Fresult = Fsystem.createSound(path, MODE.LOOP_BIDI | MODE._2D, out Fsound[i]);
            }
            else
            {
                Fresult = Fsystem.createSound(path, MODE.LOOP_NORMAL | MODE._2D, out Fsound[i]);
            }
            Fresult = Fsystem.playSound(Fsound[i], FChannelGroup[i], false, out Fchannel[i]);
            //END PLAYING sound creating

            //create RECORDING sound
            Fsound_R[i] = new Sound(intPtr_Sound_R[i]);
            if (loopBiDi)
            {
                Fresult = Fsystem_R.createSound(path, MODE.LOOP_BIDI | MODE._2D, out Fsound_R[i]);
            }
            else
            {
                Fresult = Fsystem_R.createSound(path, MODE.LOOP_NORMAL | MODE._2D, out Fsound_R[i]);
            }
            Fresult = Fsystem_R.playSound(Fsound_R[i], FChannelGroup_R[i], false, out Fchannel_R[i]);
            //END RECORDING sound creating

            int index = 0;
            index = GetDspFilterCutoffIndex(emotionsPile[i]);

            Fresult = Fdsp_Highpass[i].setParameterFloat((int)FMOD.DSP_HIGHPASS.CUTOFF, dspHighpass_Cutoff[index]);
            Fresult = Fdsp_Lowpass[i].setParameterFloat((int)FMOD.DSP_LOWPASS_SIMPLE.CUTOFF, dspLowpass_Cutoff[index]);
            Fchannel[i].setPaused(true);

            Fresult = Fdsp_Highpass_R[i].setParameterFloat((int)FMOD.DSP_HIGHPASS.CUTOFF, dspHighpass_Cutoff[index]);
            Fresult = Fdsp_Lowpass_R[i].setParameterFloat((int)FMOD.DSP_LOWPASS_SIMPLE.CUTOFF, dspLowpass_Cutoff[index]);
            Fchannel_R[i].setPaused(true);

            if (!isExit)
            {
                Thread t;
                t = new Thread(() => oneSoundStart(i));
                t.Start();
            }
        }

        private void PlayLyricsFragment(int fragmentNum)
        {
            Sound fsnd;
            IntPtr intp;
            IntPtr intp_ch;
            Channel ch;
            string path = "D:/Sounds/lyrics_1/" + fragmentNum + ".wav";

            intp = new IntPtr();
            fsnd = new Sound(intp);
            intp_ch = new IntPtr();
            ch = new Channel(intp_ch);
            Fresult = Fsystem.createSound(path, MODE.LOOP_OFF | MODE._2D, out fsnd);
            Fresult = Fsystem.playSound(fsnd, null, false, out ch);
        }
    }
}
