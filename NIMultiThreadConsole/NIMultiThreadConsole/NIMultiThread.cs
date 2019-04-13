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
        static NIMultiThread niMT2;
        
        niFgen niFgenObj;
        niScope niScopeObj;
        static Thread threadFFMPEG;
        static Thread threadTX;
        static Thread threadRX;
        static FFMPEGReader ffmpegReader;// = new FFMPEGReader();
        static MatlabTXRX mtx;

        double M = 4;
        double fsym_tx = 5e5;
        double fs_tx = 5e6;
        double fc = 13e5;
        double fs_rx = 10e6;

        double[] niscope_data;
        byte[] nifgen_symbols; // symbols

        bool nifgen_initiated;
        bool niscope_initiated;
         
        static void Main()
        {
            try
            {
                // T1 - FFMPEG Thread
                // T1.1 - Start FFMPEG
                ffmpegReader = new FFMPEGReader();
                threadFFMPEG = new Thread(() => ffmpegReader.FFMPEGThread());
                threadFFMPEG.Start();
                Debug.WriteLine("threadFFMPEG started");
                

                // T2 - TX Thread
                mtx = new MatlabTXRX();
                niMT = new NIMultiThread();
                threadTX = new Thread(() => niMT.TXThread());
                threadTX.Start();
                Debug.WriteLine("threadTX started");

                // T3 - RX Thread
                niMT2 = new NIMultiThread();
                threadRX = new Thread(() => niMT2.RXThread());
                threadRX.Start();
                Debug.WriteLine("threadRX started");

                
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

            nifgen_symbols = new byte[307200];
            int last_symbol = 0;
            try
            {
                // T2.1 - Configure NIFGEN
            
                string NIFGEN_CHANNEL_NAME = (string)"0";
                double NIFGEN_sample_rate = fs_tx;
                double NIFGEN_gain = 1.0;
                double NIFGEN_offset = 0.0;

                nifgen_initiated = false;
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

                while (true)
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
                        Buffer.BlockCopy(d, 0, nifgen_symbols, last_symbol, d.Length);
                        last_symbol += d.Length;
                        // Debug.WriteLine("read_from_buffer Execution Time: {0} ms = {1} ticks = {2} samples/tick", watch.ElapsedMilliseconds, watch.ElapsedTicks, (d.Length * (8 / M) * (fs_tx / fsym_tx)) / watch.ElapsedTicks);
                        // Debug.WriteLine("[ML] Bytes read from FFMPEG. Length = {0} bytes, = {1} samples", d.Length, d.Length * (8 / M) * (fs_tx / fsym_tx));
                        // T2.5 - Use Matlab script to generate TX data
                        // watch.Restart();
                        mtx.generate_tx_data(d, max_waveform_size);
                        // watch.Stop();
                        // Debug.WriteLine("generate_tx_data Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        // watch.Restart();
                        var len = mtx.output_data.GetLength(1);
                        waveform = new double[len];
                        Buffer.BlockCopy(mtx.output_data, 0, waveform, 0, len * sizeof(double));
                        // watch.Stop();
                        // Debug.WriteLine("Convert array Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        // Debug.WriteLine("[ML] TX data generated");
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
                        // Debug.WriteLine("[NIFGEN] Waveform #{0} is written, length {1}, took {2} ms = {3} ticks", write_cnt, waveform.Length, watch.ElapsedMilliseconds, watch.ElapsedTicks);
                        // Debug.WriteLine("Write Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        total_sent += waveform.Length;
                    }
                    else
                    {
                        if (nifgen_initiated == false)
                        {
                            nifgen_initiated = true;
                            niFgenObj.InitiateGeneration();
                            Debug.WriteLine("Initiated");
                        }
                        // Write to the beginning of the waveform
                        // relativeTo: 0-current, 1-beginning
                        niFgenObj.SetWaveformNextWritePosition(NIFGEN_CHANNEL_NAME, waveform_handle, 1, 0);
                        watch.Restart();
                        niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        watch.Stop();
                        // Debug.WriteLine("Write buffer is full. Moving to the beginning.");
                        // Debug.WriteLine("Write Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        total_sent = waveform.Length;
                    }
                    if (read_zeros)
                    {
                        Debug.WriteLine("[NIFGEN] Waveform #{0} 0s written, length {1}, took {2} ms = {3} ticks", write_cnt, waveform.Length, watch.ElapsedMilliseconds, watch.ElapsedTicks);
                    }
                    else
                    {
                        Debug.WriteLine("[NIFGEN] Waveform #{0} is written, length {1}, took {2} ms = {3} ticks", write_cnt, waveform.Length, watch.ElapsedMilliseconds, watch.ElapsedTicks);
                    }
                    
                    write_cnt += 1;
                }
                
            }
            catch (Exception e)
            {
                Debug.WriteLine("[NIFGEN] The last waveform written #{0}", write_cnt);
                Debug.WriteLine("Exception caught {0}", e);

                

            }
        }
        public void stop_execution(niFgen niFgenObj, niScope niScopeObj)
        {

            niFgenObj.AbortGeneration();
            Debug.WriteLine("[NIFGEN] Generation aborted.");
            niFgenObj.ClearArbMemory();
            Debug.WriteLine("[NIFGEN] Memory cleared.");
            niFgenObj.Dispose();
            Debug.WriteLine("[NIFGEN] Successfully disposed.");
            niScopeObj.Abort();
            Debug.WriteLine("[NISCOPE] Acquisition aborted.");
            niScopeObj.Dispose();
            Debug.WriteLine("[NISCOPE] Successfully disposed.");
            
        }

        public void write_results()
        {
            // write rx signal
            string file_name = "rx_waveform_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() + ".txt";
            int file_ctr = 1;
            string new_file_name;
            do
            {
                new_file_name = file_ctr.ToString() + file_name;
                file_ctr += 1;
            } while (File.Exists(new_file_name));

            File.WriteAllLines(new_file_name, niMT2.niscope_data.Select(d => d.ToString()));
            // Write tx symbols
            file_ctr -= 1;
            string tx_file_name = "tx_symbols_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() + ".txt";
            string tx_new_file_name = file_ctr.ToString() + tx_file_name;
            File.WriteAllLines(tx_new_file_name, niMT.nifgen_symbols.Select(d => d.ToString()));
        }
        public void RXThread()
        {
            // Acquisition parameters
            string NISCOPE_resource_name = "PXI1Slot5";
            string NISCOPE_channel_name = (string)"1";
            int NISCOPE_num_waveform = 1;
            double NISCOPE_sample_rate = fs_rx;
            

            // Configure vertical properties of the acquisition
            int NISCOPE_coupling = niScopeConstants.Dc;
            double NISCOPE_vertical_range = 3;//%1;% 5M %%1.3M: % without amp 0.4;% with amplifier 0.5;
            double NISCOPE_vertical_offset = 0;
            int NISCOPE_probe_attenuation = 1; // %%1.3M % without amplifier 10;% with amplifier 1;
            int NISCOPE_impedance = 50;

            // Horizontal properties of the acquisition
            int NISCOPE_min_record_length = 1000; // default = 1000
            double NISCOPE_ref_position = 0.5; // default = 50%

            // Trigger  properties
            double NISCOPE_trigger_holdoff = 0;
            double NISCOPE_trigger_delay = 0;

            // Fetch parameters
            int NISCOPE_max_per_fetch;
            double NISCOPE_timeout = 0;
            // short[] waveform16;
            double[] waveformD;
            niScopeWfmInfo[] waveform_info = new niScopeWfmInfo[NISCOPE_num_waveform];


            int cnt_acq = 1;
            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                niScopeObj = new niScope(NISCOPE_resource_name, true, true);
                niScopeObj.AutoSetup();
                Debug.WriteLine("[NISCOPE] Auto setup");
                niScopeObj.ConfigureVertical(NISCOPE_channel_name, NISCOPE_vertical_range, NISCOPE_vertical_offset, NISCOPE_coupling, NISCOPE_probe_attenuation, true);
                Debug.WriteLine("[NISCOPE] Configured vertical");
                niScopeObj.ConfigureChanCharacteristics(NISCOPE_channel_name, NISCOPE_impedance, NISCOPE_sample_rate);
                Debug.WriteLine("[NISCOPE] Configured channel char");
                niScopeObj.ConfigureHorizontalTiming(NISCOPE_sample_rate, NISCOPE_min_record_length, NISCOPE_ref_position, NISCOPE_num_waveform, true);
                Debug.WriteLine("[NISCOPE] Configured horixontal");
                niScopeObj.ConfigureTriggerSoftware(NISCOPE_trigger_holdoff, NISCOPE_trigger_delay);
                Debug.WriteLine("[NISCOPE] Configured trigger");
                niScopeObj.SetInt32(niScopeProperties.FetchRelativeTo, niScopeConstants.ReadPointer);
                int mem = niScopeObj.GetInt32(niScopeProperties.OnboardMemorySize);
                Debug.WriteLine("[NISCOPE] Available memory {0}", mem);
                NISCOPE_max_per_fetch = (int) mem / 16;
                Debug.WriteLine("[NISCOPE] Max per fetch {0}", NISCOPE_max_per_fetch);
                // waveform16 = new short[NISCOPE_max_per_fetch];
                waveformD = new double[NISCOPE_max_per_fetch];
                niscope_data = new double[(int)mem / 4];
                Debug.WriteLine("[NISCOPE] Circular buffer length {0}", niscope_data.Length);
                int last_write = 0;
                watch2.Restart();
                while (watch2.ElapsedMilliseconds < 1500) ;
                watch2.Stop();
                // Thread.Sleep(400);
                // while (!(niMT.nifgen_initiated)) ;
                niScopeObj.InitiateAcquisition();
                Debug.WriteLine("[NISCOPE] Initiated scope.");
                niscope_initiated = true;
                var watch_total = System.Diagnostics.Stopwatch.StartNew();
                while (true)
                {
                    watch2.Restart();
                    niScopeObj.Fetch(NISCOPE_channel_name, NISCOPE_timeout, NISCOPE_max_per_fetch, waveformD, waveform_info);
                    // niScopeObj.FetchBinary16(NISCOPE_channel_name, NISCOPE_timeout, NISCOPE_max_per_fetch, waveform16, waveform_info);
                    watch2.Stop();
                    Debug.WriteLine("[NISCOPE] Fetched waveform #{0}, length {1}, took {2} ticks", cnt_acq, waveform_info[0].ActualSamples, watch2.ElapsedTicks);
                    cnt_acq += 1;
                    try
                    {
                        // watch2.Restart();
                        Buffer.BlockCopy(waveformD, 0, niscope_data, last_write, waveform_info[0].ActualSamples);
                        last_write += waveform_info[0].ActualSamples;
                        // watch2.Stop();
                        // Debug.WriteLine("[NISCOPE] Wrote to buffer. Took {0} ticks.", watch2.ElapsedTicks);
                    }
                    catch (Exception e)
                    {
                        watch_total.Stop();
                        Debug.WriteLine("[NISCOPE] Rethrowing exception {0}", e);
                        Debug.WriteLine("[NISCOPE] Last write {0}", last_write);
                        Debug.WriteLine("[NISCOPE] Totalruntime {0}", watch_total.ElapsedTicks);

                        // string file_name = "rx_waveform_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() + ".txt";
                        // int file_ctr = 1;
                        // string new_file_name;
                        // do
                        // {
                        //     new_file_name = file_ctr.ToString() + file_name;
                        //     file_ctr += 1;
                        // } while (File.Exists(new_file_name));
                        // 
                        // File.WriteAllLines(new_file_name, niscope_data.Select(d => d.ToString()));
                        throw;
                    }
                    
                    // watch2.Restart();
                    // File.AppendAllLines(@"waveform.txt", waveform.Select(d => d.ToString()));
                    // watch2.Stop();
                    // Debug.WriteLine("[NISCOPE] Wrote to file. Took {0} ticks.", watch2.ElapsedTicks);
                }

                // File.AppendAllText(@"DebugFFMPEG.txt", now.TimeOfDay.ToString() + ffmpegProg + Environment.NewLine);

            }
            catch (Exception e)
            {
                Debug.WriteLine("[NISCOPE] Exception caught {0}", e);
                write_results();
                stop_execution(niMT.niFgenObj, niScopeObj);

                Process ffplayProcess;
                ffmpegReader.FFPLAYStart(out ffplayProcess);
                ffmpegReader.send_data_to_ffplay(niMT.nifgen_symbols);
                
                
                // niScopeObj.Abort();
                // Debug.WriteLine("[NISCOPE] Acquisition aborted.");
                // niScopeObj.Dispose();
                // Debug.WriteLine("[NISCOPE] Successfully disposed.");
                // niFgenObj.AbortGeneration();
                // Debug.WriteLine("[NIFGEN] Generation aborted.");
                // niFgenObj.ClearArbMemory();
                // Debug.WriteLine("[NIFGEN] Memory cleared.");
                // niFgenObj.Dispose();
                // Debug.WriteLine("[NIFGEN] Successfully disposed.");
            }
            

        }

    }
}
