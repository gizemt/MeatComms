using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace NIMultiThreadConsole
{
    public class NIMultiThread
    {
        static NIMultiThread niMT;
        
        niFgen niFgenObj;
        static Thread threadFFMPEG;
        static Thread threadTX;
        static FFMPEGReader ffmpegReader;// = new FFMPEGReader();
        static MatlabTXRX mtx;

        

        double M = 4;
        double fsym_tx = 1e5;
        double fs_tx = 5e6;
        double fc = 13e5;
         
        static void Main()
        {
            try
            {
                // T1 - FFMPEG Thread
                // T1.1 - Start FFMPEG
                ffmpegReader = new FFMPEGReader();

                threadFFMPEG = new Thread(() => ffmpegReader.FFMPEGThread());
                

                // T2 - TX Thread
                mtx = new MatlabTXRX();
                niMT = new NIMultiThread();
                threadTX = new Thread(() => niMT.TXThread());
                threadTX.Start();

                threadFFMPEG.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception caught {0}", e);
                ffmpegReader.done_flag = true;
                ffmpegReader.program_end = true;
                threadFFMPEG.Abort();
            }
            
        }
        public void TXThread()
        {
            int waveform_handle;
            int max_waveform_size;
            int total_sent = 0;
            int write_cnt = 0;
            try
            {
                // T2.1 - Configure NIFGEN
            
                string NIFGEN_CHANNEL_NAME = (string)"0";
                double NIFGEN_sample_rate = fs_tx;
                double NIFGEN_gain = 1.0;
                double NIFGEN_offset = 0.0;

                bool nifgen_initiated = false;
                waveform_handle = 0;
                
                // max_waveform_size = (int)(ffmpegReader.BUFFER_SIZE * ((8 / M) * (fs_tx/fsym_tx)));

                niFgenObj = new niFgen("PXI1Slot8", true, true);
                niFgenObj.ConfigureChannels(NIFGEN_CHANNEL_NAME);
                // Allocate 1/2th of the memory to streaming waveform
                max_waveform_size = 1228800*2;// niFgenObj.GetInt32(niFgenProperties.MemorySize) / (8 * 2);

                int NIFGEN_OUTMODE = 1; // arbitrary waveform
                niFgenObj.ConfigureOutputMode(NIFGEN_OUTMODE);
                niFgenObj.ConfigureSampleRate(NIFGEN_sample_rate);
                Debug.WriteLine("NIFGEN Configured");

                // T2.2 - Initialize parameters for reading from ffmpeg buffer
                byte[] d;
                bool read_zeros;
                int prev_write_end = 0;
                int last_checked = 0;
                int n_cycle = 1;
                // T2.3- Initialize Matlab instance
                mtx.create_matlab_instance("C:\\Users\\Gizem\\Box\\Research\\Projects\\MeatComms\\Experiments\\C#");
                mtx.initalize_tx_params(M, fsym_tx, fs_tx, fc);
                
                Debug.WriteLine("Matlab Initialized");

                var watch = System.Diagnostics.Stopwatch.StartNew();

                while (write_cnt < 2000)
                {
                    if (write_cnt == 0)
                    {
                        niFgenObj.AllocateWaveform(NIFGEN_CHANNEL_NAME, max_waveform_size, out waveform_handle);
                        Debug.WriteLine("[NIFGEN] Waveform allocated");
                        Debug.WriteLine("[NIFGEN] Max Waveform size is {0}", max_waveform_size);
                        niFgenObj.SetInt32(niFgenProperties.StreamingWaveformHandle, waveform_handle);
                        niFgenObj.ConfigureArbWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, NIFGEN_gain, NIFGEN_offset);
                        niFgenObj.ConfigureOutputEnabled(NIFGEN_CHANNEL_NAME, true);

                        // niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        // total_sent += waveform.Length;
                        // Debug.WriteLine("[NIFGEN] First waveform written, length {0}.", waveform.Length);
                        // niFgenObj.InitiateGeneration();

                        // StreamingSpaceAvailableInWaveform = 1150325
                        // StreamingWaveformName = 1150326,
                        // Debug.WriteLine(niFgenObj.GetInt32(niFgenProperties.StreamingSpaceAvailableInWaveform, niFgenObj.GetString(niFgenProperties.StreamingWaveformName)));//1150325, niFgenObj.GetString(1150326)));
                    }

                    // T2.4 - Read from FFMPEG buffer
                    watch.Restart();
                    d = ffmpegReader.read_from_buffer(ref prev_write_end, ref last_checked, ref n_cycle);
                    watch.Stop();
                    double[] waveform;
                    if (d.Length == ffmpegReader.EMPTY_LENGTH)
                    {
                        waveform = new double[(int)(ffmpegReader.EMPTY_LENGTH * (8 / M) * (fs_tx / fsym_tx))];
                        read_zeros = true;
                    }
                    else
                    {
                        Debug.WriteLine("read_from_buffer Execution Time: {0} ms = {1} ticks = {2} samples/tick", watch.ElapsedMilliseconds, watch.ElapsedTicks, (d.Length * (8 / M) * (fs_tx / fsym_tx)) / watch.ElapsedTicks);
                        Debug.WriteLine("[ML] Bytes read from FFMPEG. Length = {0} bytes, = {1} samples", d.Length, d.Length * (8 / M) * (fs_tx / fsym_tx));
                        // T2.5 - Use Matlab script to generate TX data
                        watch.Restart();
                        mtx.generate_tx_data(d, max_waveform_size);
                        watch.Stop();
                        Debug.WriteLine("generate_tx_data Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        watch.Restart();
                        var len = mtx.output_data.GetLength(1);
                        waveform = new double[len];
                        Buffer.BlockCopy(mtx.output_data, 0, waveform, 0, len * sizeof(double));
                        // var waveform = (mtx.output_data).OfType<double>().ToArray();
                        watch.Stop();
                        Debug.WriteLine("Convert array Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        Debug.WriteLine("[ML] TX data generated");
                        read_zeros = false;
                    }
                    
                    // T2.6 - Send that data to AWG with NIFGEN
                    
                    if (total_sent + waveform.Length < max_waveform_size)
                    {
                        // During generation, the available space may be in multiple locations with, for example, part of the available space at the end of the streaming waveform and the rest at the beginning. 
                        // In this situation, writing a block of waveform data the size of the total space available in the streaming waveform causes NI-FGEN to return an error, as NI-FGEN will not wrap the data from the end of the waveform to the beginning and cannot write data past the end of the waveform buffer. 
                        watch.Restart();
                        niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        watch.Stop();
                        Debug.WriteLine("Write Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        total_sent += waveform.Length;
                    }
                    else
                    {
                        if (nifgen_initiated == false)
                        {
                            niFgenObj.InitiateGeneration();
                            nifgen_initiated = true;
                            Debug.WriteLine("Initiated");
                        }
                        // Write to the beginning of the waveform
                        // relativeTo: 0-current, 1-beginning
                        niFgenObj.SetWaveformNextWritePosition(NIFGEN_CHANNEL_NAME, waveform_handle, 1, 0);
                        watch.Restart();
                        niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        watch.Stop();
                        Debug.WriteLine("Write buffer is full. Moving to the beginning.");
                        Debug.WriteLine("Write Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        total_sent = waveform.Length;
                    }
                    if (read_zeros)
                    {
                        Debug.WriteLine("[NIFGEN] Waveform #{0} - {1} 0's is written", write_cnt, waveform.Length);
                    }
                    else
                    {
                        Debug.WriteLine("[NIFGEN] Waveform #{0} is written, length {1}", write_cnt, waveform.Length);
                    }
                    
                    write_cnt += 1;
                }
                niFgenObj.AbortGeneration();
                niFgenObj.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine("[NIFGEN] The last waveform written #{0}", write_cnt);
                Debug.WriteLine("Exception caught {0}", e);
                niFgenObj.AbortGeneration();
                Debug.WriteLine("[NIFGEN] Generation aborted.");
                niFgenObj.ClearArbMemory();
                Debug.WriteLine("[NIFGEN] Memory cleared.");
                niFgenObj.Dispose();
                Debug.WriteLine("[NIFGEN] Successfully disposed.");

            }
        }
        /*
        unsafe public double[] elemCopyUnsafe(double[,] input)
        {
            var len = input.GetLength(1);
            var result = new double[len];
            fixed (double* pInput = input, pResult = result)
                for (var i = 0; i < len; i++)
                {
                    *(pResult + i) = *(pInput + i);
                }
            return result;

        }*/

    }
}
