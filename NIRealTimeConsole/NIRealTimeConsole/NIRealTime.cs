using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace NIRealTimeConsole
{
    public class NIRealTime
    {
        // static NIRealTime niMT;
        static StreamWriter console_writer = null;

        static niFgen niFgenObj;
        static niScope niScopeObj;
        static Thread threadFFMPEG;
        static Thread threadTX;
        static Thread threadRX;
        static Thread threadRX_matlab;
        static FFMPEGReader ffmpegReader;// = new FFMPEGReader();
        static MatlabTXRX mtx;
        static MatlabTXRX mtx2;

        static bool nifgen_configured = false;
        static bool niscope_ended = false;

        static double M = 4;
        static double fsym_tx = 1e5;
        static double fs_tx = 5e6;
        static double fc = 13e5;
        static double fs_rx = 5e6;

        public static bool webcam_bool;// = true;
        public static int byte_range = 0;

        static short[] rx_data_buffer = new short[524288 * 4];
        static int rx_data_buffer_last_write;


        static Stopwatch RXwatch = new Stopwatch();


        bool nifgen_initiated;
        // bool niscope_initiated;

        static void Main()
        {
            // try
            // {
            string console_filename = "DebugOut_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".txt";
            FileStream fs = new FileStream(console_filename, FileMode.Create);
            console_writer = new StreamWriter(fs);
            Console.SetOut(console_writer);


            // T2 - TX Thread
                
            NIRealTime niMT = new NIRealTime();
            mtx = new MatlabTXRX();
            ffmpegReader = new FFMPEGReader();

            threadTX = new Thread(() => niMT.TXThread());
            threadTX.Start();
            Console.WriteLine("threadTX started");

            // T1 - FFMPEG Thread
            // T1.1 - Start FFMPEG

            threadFFMPEG = new Thread(() => ffmpegReader.FFMPEGThread());
            threadFFMPEG.Start();
            Console.WriteLine("threadFFMPEG started");

            // T3 - RX Thread
            // niMT2 = new NIRealTime();
            threadRX = new Thread(() => niMT.RXThread());
            threadRX.Start();
            Console.WriteLine("threadRX started");

            mtx2 = new MatlabTXRX();
            threadRX_matlab = new Thread(() => niMT.RXMatlabThread());
            threadRX_matlab.Start();
            Console.WriteLine("threadRXMATLAB started");

            // Task continute_RXMatlab = threadRX_matlab.ContinueWith(t => Console.WriteLine("In ContinueWith"));
            // continute_RXMatlab.Wait();
            threadRX_matlab.Join();
            stop_execution(niFgenObj, niScopeObj);
            /* 
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught {0}", e);
                ffmpegReader.done_flag = true;
                ffmpegReader.program_end = true;
                threadFFMPEG.Abort();
            }*/

        }
        public void TXThread()
        {
            int waveform_handle;
            int max_waveform_size;
            int total_sent = 0;
            int write_cnt = 0;
            byte[] nifgen_symbols = new byte[307200];
            int last_symbol = 0;
            webcam_bool = true;
            ffmpegReader.send_webcam_data = webcam_bool;
            ffmpegReader.tx_byte_range = byte_range;

            try
            {
                // T2.1 - Configure NIFGEN

                string NIFGEN_CHANNEL_NAME = (string)"0";
                double NIFGEN_sample_rate = fs_tx;
                double NIFGEN_gain = 1.0;
                double NIFGEN_offset = 0.0;

                double[] waveform;
                double t_end = 0;
                nifgen_initiated = false;
                waveform_handle = 0;

                // max_waveform_size = (int)(ffmpegReader.BUFFER_SIZE * ((8 / M) * (fs_tx/fsym_tx)));

                niFgenObj = new niFgen("PXI1Slot8", true, true);
                niFgenObj.ConfigureChannels(NIFGEN_CHANNEL_NAME);
                // Allocate 1/2th of the memory to streaming waveform
                max_waveform_size = 1228800 * 2;// niFgenObj.GetInt32(niFgenProperties.MemorySize) / (8 * 2);

                int NIFGEN_OUTMODE = 1; // arbitrary waveform
                niFgenObj.ConfigureOutputMode(NIFGEN_OUTMODE);
                niFgenObj.ConfigureSampleRate(NIFGEN_sample_rate);
                Console.WriteLine("NIFGEN Configured");

                // T2.2 - Initialize parameters for reading from ffmpeg buffer
                byte[] d;
                bool read_zeros = false;
                bool old_read_zeros = true;
                bool add_barker = true;
                int prev_write_end = 0;
                int last_checked = 0;
                int n_cycle = 1;
                double[] bark = { 1, 1, 1, 1, 1, -1, -1, 1, 1, -1, 1, -1, 1 };
                // T2.3- Initialize Matlab instance
                mtx.create_matlab_instance("C:\\Users\\Gizem\\source\\repos\\NIMultiThreadConsole\\NIMultiThreadConsole");
                mtx.initalize_tx_params(M, fsym_tx, fs_tx, fc);

                Console.WriteLine("Matlab Initialized");

                // var watch = System.Diagnostics.Stopwatch.StartNew();

                while (true)
                {
                    if (write_cnt == 0)
                    {
                        niFgenObj.AllocateWaveform(NIFGEN_CHANNEL_NAME, max_waveform_size, out waveform_handle);
                        Console.WriteLine("[NIFGEN] Waveform allocated");
                        Console.WriteLine("[NIFGEN] Max Waveform size is {0}", max_waveform_size);
                        niFgenObj.SetInt32(niFgenProperties.StreamingWaveformHandle, waveform_handle);
                        niFgenObj.ConfigureArbWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, NIFGEN_gain, NIFGEN_offset);
                        niFgenObj.ConfigureOutputEnabled(NIFGEN_CHANNEL_NAME, true);
                        nifgen_configured = true;
                        add_barker = true;
                        // niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        // total_sent += waveform.Length;
                        // Console.WriteLine("[NIFGEN] First waveform written, length {0}.", waveform.Length);
                        // niFgenObj.InitiateGeneration();

                        // StreamingSpaceAvailableInWaveform = 1150325
                        // StreamingWaveformName = 1150326,
                        // Console.WriteLine(niFgenObj.GetInt32(niFgenProperties.StreamingSpaceAvailableInWaveform, niFgenObj.GetString(niFgenProperties.StreamingWaveformName)));//1150325, niFgenObj.GetString(1150326)));
                    }

                    // T2.4 - Read from FFMPEG buffer
                    // watch.Restart();
                    old_read_zeros = read_zeros;
                    d = ffmpegReader.read_from_buffer(ref prev_write_end, ref last_checked, ref n_cycle, out read_zeros);

                    // watch.Stop();

                    if (read_zeros)//((d.Length == ffmpegReader.EMPTY_LENGTH) && !(ffmpegReader.EMPTY_LENGTH == last_checked - prev_write_end + 1))
                    {
                        waveform = new double[(int)(ffmpegReader.EMPTY_LENGTH * (8 / M) * (fs_tx / fsym_tx))];
                        if (!old_read_zeros)
                        {
                            Buffer.BlockCopy(bark, 0, waveform, (int)(fs_tx / fsym_tx) * sizeof(double), bark.Length * sizeof(double));
                        }
                        add_barker = true;

                        // read_zeros = true;
                    }
                    else
                    {

                        // Console.WriteLine("read_from_buffer Execution Time: {0} ms = {1} ticks = {2} samples/tick", watch.ElapsedMilliseconds, watch.ElapsedTicks, (d.Length * (8 / M) * (fs_tx / fsym_tx)) / watch.ElapsedTicks);
                        // Console.WriteLine("[ML] Bytes read from FFMPEG. Length = {0} bytes, = {1} samples", d.Length, d.Length * (8 / M) * (fs_tx / fsym_tx));
                        // T2.5 - Use Matlab script to generate TX data
                        // watch.Restart();
                        // double[] output_data;
                        waveform = mtx.generate_tx_data(d, add_barker, max_waveform_size, ref t_end);
                        // File.WriteAllLines("Console_d", d.Select(x => x.ToString()));

                        Buffer.BlockCopy(d, 0, nifgen_symbols, last_symbol, d.Length);
                        last_symbol += d.Length;
                        // watch.Stop();
                        // Console.WriteLine("generate_tx_data Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        // watch.Restart();

                        // watch.Stop();
                        // Console.WriteLine("Convert array Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        // Console.WriteLine("[ML] TX data generated");
                        read_zeros = false;
                        add_barker = false;
                    }

                    // T2.6 - Send that data to AWG with NIFGEN

                    if (total_sent + waveform.Length < max_waveform_size)
                    {
                        // During generation, the available space may be in multiple locations with, for example, part of the available space at the end of the streaming waveform and the rest at the beginning. 
                        // In this situation, writing a block of waveform data the size of the total space available in the streaming waveform causes NI-FGEN to return an error, as NI-FGEN will not wrap the data from the end of the waveform to the beginning and cannot write data past the end of the waveform buffer. 

                        // watch.Restart();
                        // niFgenObj.SetWaveformNextWritePosition(NIFGEN_CHANNEL_NAME, waveform_handle, 1, total_sent);
                        niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        // watch.Stop();
                        // Console.WriteLine("[NIFGEN] Waveform #{0} is written, length {1}, took {2} ms = {3} ticks", write_cnt, waveform.Length, watch.ElapsedMilliseconds, watch.ElapsedTicks);
                        // Console.WriteLine("Write Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        total_sent += waveform.Length;
                    }
                    else
                    {
                        if ((nifgen_initiated == false))
                        {
                            nifgen_initiated = true;
                            niFgenObj.InitiateGeneration();
                            Console.WriteLine("Initiated");
                        }

                        // Write to the beginning of the waveform
                        // relativeTo: 0-current, 1-beginning
                        // total_sent = 0;
                        // watch.Restart();
                        niFgenObj.SetWaveformNextWritePosition(NIFGEN_CHANNEL_NAME, waveform_handle, 1, 0);
                        niFgenObj.WriteWaveform(NIFGEN_CHANNEL_NAME, waveform_handle, waveform.Length, waveform);
                        // watch.Stop();
                        // Console.WriteLine("Write buffer is full. Moving to the beginning.");
                        // Console.WriteLine("Write Execution Time: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                        total_sent = waveform.Length;
                    }
                    if (read_zeros)
                    {
                        // Console.WriteLine("[NIFGEN] Waveform #{0} 0s written, length {1}, took {2} ms = {3} ticks", write_cnt, waveform.Length, watch.ElapsedMilliseconds, watch.ElapsedTicks);
                        Console.WriteLine("[NIFGEN] 0s written {0}", waveform.Length);
                    }
                    else
                    {

                        Console.WriteLine("[NIFGEN] data written {0}", waveform.Length);
                        // Console.WriteLine("[NIFGEN] Waveform #{0} is written, length {1}, total {2}", write_cnt, waveform.Length, total_sent);
                        // break;
                        // if (nifgen_initiated)
                        // {
                        //     Console.WriteLine("[NIFGEN] Writing Consoles");
                        //     File.WriteAllLines("Console_d.txt", d.Select(x => x.ToString()));
                        //     File.WriteAllLines("Console_waveform.txt", waveform.Select(x => x.ToString()));
                        //     throw new System.Exception("[NIFGEN] Wrote Consoles");
                        // }

                    }

                    write_cnt += 1;
                }
                // Console.WriteLine("[NIFGEN] The last waveform written #{0}", write_cnt);
                Console.WriteLine("[NIFGEN] Writing Consoles");
                // File.WriteAllLines("Console_d.txt", d.Select(x => x.ToString()));
                string tx_file_name = "tx_Console_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() + "_webcam" + (webcam_bool ? 1 : 0).ToString() + "_tx_range" + byte_range.ToString() + ".txt";
                int tx_file_ctr = 1;
                string tx_new_file_name;
                do
                {
                    tx_new_file_name = tx_file_ctr.ToString() + tx_file_name;
                    tx_file_ctr += 1;
                } while (File.Exists(tx_new_file_name));

                // File.WriteAllLines(tx_new_file_name, waveform.Select(x => x.ToString()));
                // throw new System.Exception("[NIFGEN] Exiting NIFGEN");

            }
            catch (Exception e)
            {
                Console.WriteLine("[NIFGEN] The last waveform written #{0}", write_cnt);
                Console.WriteLine("Exception caught {0}", e);
                niFgenObj.AbortGeneration();
                Console.WriteLine("[NIFGEN] Generation aborted.");
                niFgenObj.ClearArbMemory();
                Console.WriteLine("[NIFGEN] Memory cleared.");
                niFgenObj.Dispose();
                Console.WriteLine("[NIFGEN] Successfully disposed.");
                // Write tx symbols
                /*
                 * string tx_file_name = "tx_symbols_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() + "_webcam" + (webcam_bool ? 1 : 0).ToString() + "_tx_range" + byte_range.ToString() + ".txt";
                int tx_file_ctr = 1;
                string tx_new_file_name;
                do
                {
                    tx_new_file_name = tx_file_ctr.ToString() + tx_file_name;
                    tx_file_ctr += 1;
                } while (File.Exists(tx_new_file_name));

                File.WriteAllLines(tx_new_file_name, nifgen_symbols.Select(x => x.ToString()));
                 * */
                // stop_execution(niFgenObj, niMT2.niScopeObj);
            }
        }
        public static void stop_execution(niFgen niFgenObj, niScope niScopeObj)
        {

            niFgenObj.AbortGeneration();
            Console.WriteLine("[NIFGEN] Generation aborted.");
            niFgenObj.ClearArbMemory();
            Console.WriteLine("[NIFGEN] Memory cleared.");
            niFgenObj.Dispose();
            Console.WriteLine("[NIFGEN] Successfully disposed.");
            niScopeObj.Abort();
            Console.WriteLine("[NISCOPE] Acquisition aborted.");
            niScopeObj.Dispose();
            Console.WriteLine("[NISCOPE] Successfully disposed.");

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
            double NISCOPE_ref_position = 0; // default = 50%

            // Trigger  properties
            double NISCOPE_trigger_holdoff = 0;
            double NISCOPE_trigger_delay = 0;

            // Fetch parameters
            int NISCOPE_max_per_fetch;
            double NISCOPE_timeout = 0;
            short[] waveform16;
            // double[] waveformD;
            // short[] niscope_data;
            // short[] niscope_all_data;
            // double[] niscope_data;
            niScopeWfmInfo[] waveform_info = new niScopeWfmInfo[NISCOPE_num_waveform];


            int cnt_acq = 1;


            niScopeObj = new niScope(NISCOPE_resource_name, true, true);
            niScopeObj.AutoSetup();
            Console.WriteLine("[NISCOPE] Auto setup");
            niScopeObj.ConfigureVertical(NISCOPE_channel_name, NISCOPE_vertical_range, NISCOPE_vertical_offset, NISCOPE_coupling, NISCOPE_probe_attenuation, true);
            Console.WriteLine("[NISCOPE] Configured vertical");
            niScopeObj.ConfigureChanCharacteristics(NISCOPE_channel_name, NISCOPE_impedance, NISCOPE_sample_rate);
            Console.WriteLine("[NISCOPE] Configured channel char");
            niScopeObj.ConfigureHorizontalTiming(NISCOPE_sample_rate, NISCOPE_min_record_length, NISCOPE_ref_position, NISCOPE_num_waveform, true);
            Console.WriteLine("[NISCOPE] Configured horixontal");
            niScopeObj.ConfigureTriggerSoftware(NISCOPE_trigger_holdoff, NISCOPE_trigger_delay);
            Console.WriteLine("[NISCOPE] Configured trigger");
            niScopeObj.SetInt32(niScopeProperties.FetchRelativeTo, niScopeConstants.ReadPointer);
            Console.WriteLine(niScopeObj.GetInt32(niScopeProperties.FetchRelativeTo));
            int mem = niScopeObj.GetInt32(niScopeProperties.OnboardMemorySize);
            Console.WriteLine("[NISCOPE] Available memory {0}", mem);
            NISCOPE_max_per_fetch = (int)mem / 16;
            Console.WriteLine("[NISCOPE] Max per fetch {0}", NISCOPE_max_per_fetch);
            // waveform16 = new short[NISCOPE_max_per_fetch];
            // waveformD = new double[NISCOPE_max_per_fetch];
            // niscope_data = new short[(int)mem / 4];
            // niscope_all_data = new short[(int)mem / 4];
            // niscope_data = new double[(int)mem / 4];
            // Console.WriteLine("[NISCOPE] Circular buffer length {0}", niscope_data.Length);
            // int last_write = 0;
            int last_all_write = 0;
            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            // watch2.Restart();
            // while (watch2.ElapsedMilliseconds < 1000) ;
            // watch2.Stop();
            while (!(nifgen_configured)) ;
            try
            {
                // Thread.Sleep(400);
                // while (!(niMT.nifgen_initiated)) ;
                niScopeObj.InitiateAcquisition();
                Console.WriteLine("[NISCOPE] Initiated scope.");
                // niscope_initiated = true;
                var watch_total = System.Diagnostics.Stopwatch.StartNew();
                int[] corr_array = { 1, 1, 1, 1, 1, -1, -1, 1, 1, -1, 1, -1, 1 };
                double barker_th = 5.0 * NISCOPE_vertical_range / 2.0;
                int corr_length = corr_array.Length;
                int n_guard = (int)(fs_rx / fsym_tx);
                int n_ahead = n_guard;
                int n_prev = n_ahead + corr_length + 1;
                short[] x_rec_prev = new short[n_prev];
                bool data_started = false;
                bool data_ended = true;
                bool data_started_here;
                int prev_written_count = 0;
                int total_fetched = 0;
                int local_fetched;
                double fetch_gain;
                double fetch_offset;
                double fetch_gain_prev = 0;
                double fetch_offset_prev = 0;
                int corr_first_peak_idx = 0;
                int corr_end_peak_idx = -(n_ahead + 1);
                double corr_first;
                double corr_end;
                int i_end;
                int rx_data_buffer_length = rx_data_buffer.Length;
                int ctr = 0;

                while (prev_written_count < rx_data_buffer_length)
                {
                    data_started_here = false;
                    // watch2.Restart();
                    // niScopeObj.Fetch(NISCOPE_channel_name, NISCOPE_timeout, NISCOPE_max_per_fetch, waveformD, waveform_info);
                    waveform16 = new short[NISCOPE_max_per_fetch];
                    RXwatch.Start();
                    niScopeObj.FetchBinary16(NISCOPE_channel_name, NISCOPE_timeout, NISCOPE_max_per_fetch, waveform16, waveform_info);
                    // watch2.Stop();
                    // Console.WriteLine("[NISCOPE] Fetched {0}", waveform_info[0].ActualSamples);
                    // Console.WriteLine("[NISCOPE] Fetched waveform #{0}, length {1}, took {2} ticks, gain {3}", cnt_acq, waveform_info[0].ActualSamples, watch2.ElapsedTicks, waveform_info[0].Gain);
                    cnt_acq += 1;
                    try
                    {
                        // watch2.Restart();
                        // Buffer.BlockCopy(waveformD, 0, niscope_data, last_write, waveform_info[0].ActualSamples * sizeof(double));
                        // last_write += waveform_info[0].ActualSamples * sizeof(double);
                        // watch2.Stop();
                        // Console.WriteLine("[NISCOPE] Wrote to buffer. Took {0} ticks.", watch2.ElapsedTicks);
                        fetch_gain = waveform_info[0].Gain;
                        fetch_offset = waveform_info[0].Offset;
                        local_fetched = waveform_info[0].ActualSamples;
                        for (int i = 0; i < local_fetched; i++)
                        {
                            corr_first = 0;
                            corr_end = 0;
                            i_end = i + n_ahead;
                            for (int j = 0; j < corr_length; j++)
                            {
                                if (data_started)
                                {
                                    if (i_end + j < n_prev)
                                    {
                                        corr_end += ((x_rec_prev[i_end + j] - fetch_offset_prev) * fetch_gain_prev) * corr_array[j];
                                    }
                                    else
                                    {
                                        corr_end += ((waveform16[i_end + j - n_prev] - fetch_offset) * fetch_gain) * corr_array[j];
                                    }
                                }
                                else if (data_ended && (i > (corr_end_peak_idx + n_ahead)))
                                {
                                    if (i + j < n_prev)
                                    {
                                        corr_first += ((x_rec_prev[i + j] - fetch_offset_prev) * fetch_gain_prev) * corr_array[j];
                                    }
                                    else
                                    {
                                        corr_first += ((waveform16[i + j - n_prev] - fetch_offset) * fetch_gain) * corr_array[j];
                                    }
                                }

                            }

                            if (data_started)
                            {
                                if ((corr_end > barker_th))
                                {/*
                                    // Cross correlation exceeded threshold
                                    if (corr_end > corr_end_old)
                                    {
                                        // Correlation is still increasing
                                        corr_end_old = corr_end;
                                    }
                                    else
                                    {*/
                                    // We reached correlation peak in the previous sample
                                    Console.WriteLine("Data ended {0}", i);
                                    data_ended = true;
                                    data_started = false;
                                    corr_end_peak_idx = i;
                                    // }
                                }
                            }
                            else if (data_ended && (i > (corr_end_peak_idx + n_ahead)))
                            {
                                if ((corr_first > barker_th))
                                {
                                    /*// Cross correlation exceeded threshold
                                    if (corr_first > corr_first_old)
                                    {
                                        // Correlation is still increasing
                                        corr_first_old = corr_first;
                                    }
                                    else
                                    {*/
                                    Console.WriteLine("Data started {0} threshold {1} corr {2}", i, barker_th, corr_first);
                                    // We reached correlation peak in the previous sample
                                    corr_first_peak_idx = i;
                                    data_started = true;
                                    data_started_here = true;
                                    data_ended = false;

                                    // }
                                }
                            }

                            if (!data_ended && ((data_started && !data_started_here) || (data_started && data_started_here && (i > corr_first_peak_idx + n_guard + corr_length - 1))))
                            {
                                if (i < n_prev)
                                {
                                    // niscope_data[last_write] = x_rec_prev[i];
                                    rx_data_buffer[rx_data_buffer_last_write] = x_rec_prev[i];
                                }
                                else
                                {
                                    // niscope_data[last_write] = waveform16[i - n_prev];
                                    rx_data_buffer[rx_data_buffer_last_write] = waveform16[i - n_prev];
                                }

                                rx_data_buffer_last_write += 1;
                            }
                        }
                        Buffer.BlockCopy(waveform16, (local_fetched - n_prev) * sizeof(short), x_rec_prev, 0, n_prev * sizeof(short));
                        total_fetched += local_fetched;
                        Console.WriteLine("[NISCOPE] Total fetched {0}, local fetched {1}, exceeding threshold {2} data started? {3} data ended? {4}", total_fetched, local_fetched - corr_length + 1, rx_data_buffer_last_write - prev_written_count, data_started, data_ended);
                        prev_written_count = rx_data_buffer_last_write;
                        ctr = rx_data_buffer_last_write;
                        fetch_gain_prev = fetch_gain;
                        fetch_offset_prev = fetch_offset;
                        // Buffer.BlockCopy(waveform16, 0, niscope_all_data, last_all_write, local_fetched * sizeof(short));
                        last_all_write += local_fetched * sizeof(short);
                        corr_end_peak_idx = 0;
                    }
                    catch (Exception e)
                    {
                        watch_total.Stop();
                        Console.WriteLine("[NISCOPE] Rethrowing exception {0}", e);
                        Console.WriteLine("[NISCOPE] Last write {0}", rx_data_buffer_last_write);
                        Console.WriteLine("[NISCOPE] Totalruntime {0}", watch_total.ElapsedTicks);

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
                    // Console.WriteLine("[NISCOPE] Wrote to file. Took {0} ticks.", watch2.ElapsedTicks);
                }

                // File.AppendAllText(@"ConsoleFFMPEG.txt", now.TimeOfDay.ToString() + ffmpegProg + Environment.NewLine);

            }
            catch (Exception e)
            {
                Console.WriteLine("[NISCOPE] Exception caught {0}", e);
                niScopeObj.Abort();
                Console.WriteLine("[NISCOPE] Acquisition aborted.");
                niScopeObj.Dispose();
                Console.WriteLine("[NISCOPE] Successfully disposed.");
                niscope_ended = true;

                // write rx signal
                /* string file_name = "rx_waveform_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() +  "_webcam" + (webcam_bool ? 1 : 0).ToString() + "_tx_range" + byte_range.ToString() + ".txt";
                int file_ctr = 1;
                string new_file_name;
                do
                {
                    new_file_name = file_ctr.ToString() + file_name;
                    file_ctr += 1;
                } while (File.Exists(new_file_name));

                // File.WriteAllLines(new_file_name, niscope_data.Select(d => d.ToString()));
                string file_name2 = "rx_all_waveform_fc" + fc.ToString() + "_fsym_tx" + fsym_tx.ToString() + "_fs_tx" + fs_tx.ToString() + "_fs_rx" + fs_rx.ToString() + "_webcam" + (webcam_bool ? 1 : 0).ToString() + "_tx_range" + byte_range.ToString() + ".txt";
                string new_file_name2 = (file_ctr-1).ToString() + file_name2;
                // File.WriteAllLines(new_file_name2, niscope_all_data.Select(d => d.ToString()));
                
                stop_execution(niFgenObj, niScopeObj);
                 * */
            }


        }

        public void RXMatlabThread()
        {
            double[,] matlab_out;
            mtx2.create_matlab_instance("C:\\Users\\Gizem\\source\\repos\\NIMultiThreadConsole\\NIMultiThreadConsole");
            mtx2.initalize_tx_params(M, fsym_tx, fs_tx, fc);
            mtx2.initalize_rx_params(M, fsym_tx, fs_rx, fc);
            int blw;
            while ((blw = rx_data_buffer_last_write) < 500000) ;
            Console.WriteLine("[NISCOPE-MATLAB] Starting copying & processing");
            short[] x_rec = new short[blw];
            Buffer.BlockCopy(rx_data_buffer, 0, x_rec, 0, (blw) * sizeof(short));
            byte[] x_n = { 0, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1, 2, 1, 1 };
            double nsym_train = 10;
            double Frac = 2;
            double N1 = 2;
            double N2 = 4;
            double Kf1 = 5e-4;
            double Kf2 = 8e-5;
            double Kg1 = 1e-11;
            double Kg2 = 1e-12;
            double lambda = 0.997;
            RXwatch.Stop();
            Console.WriteLine("[NISCOPE-MATLAB] Starting processing. Time until now {0} ms", RXwatch.ElapsedMilliseconds);
            RXwatch.Start();
            matlab_out = mtx2.process_rx_data(x_rec, x_n, nsym_train, Frac, N1, N2, Kf1, Kf2, Kg1, Kg2, lambda);
            RXwatch.Stop();
            Console.WriteLine("[NISCOPE-MATLAB] Processed {0} samples. Output {1} bytes. Total time {2} ms", blw, matlab_out.Length, RXwatch.ElapsedMilliseconds);
        }
    }
}
