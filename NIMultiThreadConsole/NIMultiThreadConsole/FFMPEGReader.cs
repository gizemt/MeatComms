using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace NIMultiThreadConsole
{
    public class FFMPEGReader
    {

        byte[] data_from_ffmpeg;
        // byte[] data_to_ffplay;
        int buffer_end = 0;
        int last_idx = 0;
        public int read_cnt = 1;


        int NUM_READ = 1024;
        public int EMPTY_LENGTH = 4096;
        public int BUFFER_SIZE = 8192;

        public Boolean done_flag = false;
        public Boolean program_end = false;

        BinaryWriter dataWriter;
        StreamReader ffplayOutReader;
        // bool first_data_read = false;
        Thread threadFFMPEG;
        Thread FFMPEGtoFFPLAY;

        // static readonly object _locker = new object();

        static FFMPEGReader fmpReader = new FFMPEGReader();

        /*public void Main()
        {
            FFMPEGReader mt = new FFMPEGReader();


            threadFFMPEG = new Thread(() => mt.FFMPEGThread());
            // FFMPEGtoFFPLAY = new Thread(() => mt.FFMPEGtoFFPLAYThread());

            threadFFMPEG.Start();
            // FFMPEGtoFFPLAY.Start();

        }*/
        public bool get_program_end()
        {
            return fmpReader.program_end;
        }

        public void FFMPEGThread()
        {
            Process ffmpegP;
            FFMPEGStart(out ffmpegP);
            FFMPEGRead(ffmpegP);
        }

        private void FFMPEGStart(out Process ffmpegProcess)
        {
            ffmpegProcess = new Process();
            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
            ffmpegStartInfo.FileName = "C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe";
            ffmpegStartInfo.Arguments = "-y -f dshow -framerate 5 -i video=\"Logitech Webcam C925e\" -vf scale=160:120 -vcodec h264 -an -f nut pipe:1";
            ffmpegStartInfo.RedirectStandardError = false; // FFMPEG progress updates
            ffmpegStartInfo.RedirectStandardOutput = true; // FFMPEG data
            ffmpegStartInfo.RedirectStandardInput = false;
            ffmpegStartInfo.UseShellExecute = false;
            ffmpegStartInfo.CreateNoWindow = true;

            ffmpegProcess.StartInfo = ffmpegStartInfo;
            ffmpegProcess.Start();
        }

        private void FFMPEGRead(Process ffmpegProcess)//, out byte[] data, out int buffer_end)
        {
            // ffmpegProcess.EnableRaisingEvents = true;
            // ffmpegProcess.OutputDataReceived += (o, e) => Debug.WriteLine(e.Data ?? "NULL", "ffplay");
            // ffmpegProcess.ErrorDataReceived += (o, e) => Debug.WriteLine(e.Data ?? "NULL", "ffplay");
            // ffmpegProcess.Exited += (o, e) => Debug.WriteLine("Exited", "fp");
            fmpReader.data_from_ffmpeg = new byte[fmpReader.BUFFER_SIZE];
            try
            {
                // Data reader
                using (BinaryReader dataReader = new BinaryReader(ffmpegProcess.StandardOutput.BaseStream))
                {
                    // using (StreamReader progReader = new StreamReader(ffmpegProcess.StandardError.BaseStream))
                    // {
                        // string ffmpegProg;
                        // int cnt = 0;
                        int local_last_idx = fmpReader.last_idx;
                        int local_buffer_end = fmpReader.buffer_end;
                        var watch = System.Diagnostics.Stopwatch.StartNew();


                        // Debug.WriteLine(progReader.ReadLine());
                        do
                        {
                            //try
                            //{
                            // Debug.WriteLine("[FFMPEG] Progress read");
                            // if (progReader.Peek() > -1)
                            // {
                            // ffmpegProg = progReader.ReadLine();
                            // Debug.WriteLine(ffmpegProg);
                            // DateTime now = DateTime.Now;
                            // File.AppendAllText(@"DebugFFMPEG.txt", now.TimeOfDay.ToString() + ffmpegProg + Environment.NewLine);

                            // }

                            // Cannot peek to BaseStream
                            // if (dataReader.PeekChar() > -1)
                            // {
                            // lock (_locker)
                            // {
                            if (local_last_idx + NUM_READ <= BUFFER_SIZE)
                            {
                                // var bytes = dataReader.ReadBytes(NUM_READ);
                                // Buffer.BlockCopy(bytes, 0, fmpReader.data_from_ffmpeg, local_last_idx, NUM_READ);
                                // local_last_idx += bytes.Length;
                                watch.Restart();
                                
                                // char[] bytes = new char[NUM_READ];
                                // var n_read = dataReader.Read(bytes, 0, NUM_READ);
                                // Buffer.BlockCopy(bytes, 0, fmpReader.data_from_ffmpeg, local_last_idx, n_read);
                                // local_last_idx += n_read;
                                local_last_idx += dataReader.Read(fmpReader.data_from_ffmpeg, local_last_idx, NUM_READ);

                                watch.Stop();
                                Debug.WriteLine("[FFMPEGREADER]: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);
                                
                                
                                // Debug.WriteLine("[FFMPEG] Data read until {0}.", local_last_idx);
                                // now = DateTime.Now;
                                // File.AppendAllText(@"DebugFFMPEG.txt", now.TimeOfDay.ToString() + "[FFMPEG] Data read until " + local_last_idx.ToString() + Environment.NewLine);
                                fmpReader.last_idx = local_last_idx;
                                fmpReader.read_cnt += 1;
                            }
                            else
                            {
                                //lock (mt)
                                //{
                                Debug.WriteLine("[FFMPEG] Buffer full, moving to the beginning.");
                                local_buffer_end = local_last_idx;
                                fmpReader.read_cnt += 1;

                                // var bytes = dataReader.ReadBytes(NUM_READ);
                                // Buffer.BlockCopy(bytes, 0, fmpReader.data_from_ffmpeg, 0, NUM_READ);
                                // local_last_idx = bytes.Length;
                                watch.Restart();
                                
                                // char[] bytes = new char[NUM_READ];
                                // local_last_idx = dataReader.Read(bytes, 0, NUM_READ);
                                // Buffer.BlockCopy(bytes, 0, fmpReader.data_from_ffmpeg, 0, local_last_idx);
                                local_last_idx = dataReader.Read(fmpReader.data_from_ffmpeg, 0, NUM_READ);
                                watch.Stop();
                                Debug.WriteLine("[FFMPEGREADER]: {0} ms = {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);

                                Debug.WriteLine("[FFMPEG] Finished at {0}, read until {1} into the beginning.", local_buffer_end, local_last_idx);
                                fmpReader.last_idx = local_last_idx;
                                
                                //}

                            }
                            // mt.first_data_read = true;

                            fmpReader.buffer_end = local_buffer_end;
                            // }

                            // dataWriter.Write(data, 0, len);

                            // data = dataReader.ReadBytes(NUM_READ);

                            
                            // Debug.WriteLine(mt.read_cnt);
                            // now = DateTime.Now;
                            // File.AppendAllText(@"DebugFFMPEG.txt", now.TimeOfDay.ToString() + "[FFMPEG] " + fmpReader.read_cnt.ToString() + Environment.NewLine);
                            // }
                        } while (!fmpReader.program_end);
                    // }


                    // br.Dispose();
                    // br.Close();

                }
            }
            catch (Exception e)
            {
                // Debug.WriteLine("[FFMPEG] Exception caught {0}", e);
                DateTime now = DateTime.Now;
                File.WriteAllText(@"ErrorFFMPEG.txt", now.TimeOfDay.ToString() + e.ToString());

                // mt.data_from_ffmpeg = null;
                buffer_end = 0;
                // ffmpegProcess.Kill();
                fmpReader.program_end = true;
                // ffmpegProg = null;
                // break;
            }
            // cmdProcess.CloseMainWindow();
            // cmdProcess.Kill();


            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            // Console.WriteLine("Hello World!");
            // Console.ReadKey();

            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }

        private void FFMPEGtoFFPLAYThread()
        {
            Process ffplayP;
            FFPLAYStart(out ffplayP);


            // byte[] d;
            /* int write_cnt = 0;
            int prev_write_end = 0;
            int last_checked = 0;
            int n_cycle = 1;

            while (write_cnt < 2000)
            {
                read_from_buffer(ref prev_write_end, ref last_checked, ref n_cycle, out byte[] d);
                Debug.WriteLine("[FFPLAY] last_checked = {0}, prev_write_end = {1}", last_checked, prev_write_end);
                send_data_to_ffplay(d);
                // Debug.WriteLine("[FFPLAY] Data between {0} and {1} sent to FFPLAY", prev_write_end, last_checked - 1);
                write_cnt += 1;
            } */
        }

        private void FFPLAYStart(out Process ffplayProcess)
        {
            ffplayProcess = new Process();
            ProcessStartInfo ffplayStartInfo = new ProcessStartInfo();
            ffplayStartInfo.FileName = "ffplay.exe";
            ffplayStartInfo.Arguments = "-loglevel 99 -i pipe: -autoexit";
            ffplayStartInfo.UseShellExecute = false;
            ffplayStartInfo.RedirectStandardInput = true;
            ffplayStartInfo.RedirectStandardOutput = true;
            ffplayStartInfo.RedirectStandardError = false;
            ffplayStartInfo.CreateNoWindow = false;
            ffplayProcess.StartInfo = ffplayStartInfo;

            ffplayProcess.EnableRaisingEvents = true;
            ffplayProcess.Exited += (o, e) =>
            {
                fmpReader.threadFFMPEG.Abort();
                fmpReader.program_end = true;
            };

            ffplayProcess.Start();

            fmpReader.dataWriter = new BinaryWriter(ffplayProcess.StandardInput.BaseStream);
            fmpReader.ffplayOutReader = new StreamReader(ffplayProcess.StandardOutput.BaseStream);
            // Debug.WriteLine("[FFPLAY] Started");
            DateTime now = DateTime.Now;
            File.AppendAllText(@"DebugFFPLAY.txt", now.TimeOfDay.ToString() + "[FFPLAY] Started " + Environment.NewLine);

        }

        public byte[] read_from_buffer(ref int prev_write_end, ref int last_checked, ref int n_cycle)
        {
            fmpReader.done_flag = false;
            try
            {
                // https://stackoverflow.com/questions/4431568/variable-initalisation-in-while-loop
                // Debug.WriteLine("[FFPLAY] Waiting");
                // while ((last_checked = fmpReader.last_idx) <= prev_write_end) ;
                byte[] d;
                int lc;
                if ((lc = fmpReader.last_idx) <= prev_write_end)
                {
                    d = new byte[EMPTY_LENGTH];
                }
                else
                {
                    last_checked = lc;
                    // Debug.WriteLine("[FFPLAY] Not waiting");
                    d = new byte[last_checked - prev_write_end + 1];
                    Buffer.BlockCopy(fmpReader.data_from_ffmpeg, prev_write_end, d, 0, last_checked - prev_write_end + 1);
                    // Array.Copy(fmpReader.data_from_ffmpeg, prev_write_end, d, 0, last_checked - prev_write_end + 1);
                    // Debug.WriteLine("[FFPLAY] Copied {0} points starting from {1}", last_checked - prev_write_end + 1, prev_write_end);
                    // DateTime now = DateTime.Now;
                    // File.AppendAllText(@"DebugFFPLAY.txt", now.TimeOfDay.ToString() + "[FFPLAY] Copied " + Environment.NewLine);
                    // dataWriter.Write(mt.data_to_ffplay, prev_write_end, last_checked - prev_write_end);

                    if (last_checked + NUM_READ > BUFFER_SIZE)
                    {
                        prev_write_end = 0;
                        // last_checked = 0;
                        while ((n_cycle == fmpReader.read_cnt)) ;
                        n_cycle++;
                        // while (!(mt.last_idx < last_checked)) ;
                    }
                    else
                    {
                        prev_write_end = last_checked;
                    }
                    fmpReader.done_flag = true;
                }
                    
                return d;
            }
            catch (Exception e)
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(@"ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.ToString() + Environment.NewLine);
                if (e.InnerException != null)
                    File.AppendAllText(@"ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.InnerException.ToString() + Environment.NewLine);
                // Debug.WriteLine("[FFPLAY] Exception caught {0}", e);
                // d = null;
                // fmpReader.threadFFMPEG.Abort();
                // fmpReader.FFMPEGtoFFPLAY.Abort();
                fmpReader.done_flag = true;
                fmpReader.program_end = true;
                return null;
            }

        }


        public void send_data_to_ffplay(byte[] d)
        {
            while (!fmpReader.done_flag) ;
            try
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(@"DebugFFPLAY.txt", now.TimeOfDay.ToString() + "d " + d.ToString()+ Environment.NewLine);
                if (d != null)
                {
                    fmpReader.dataWriter.Write(d);
                    // Debug.WriteLine("[FFPLAY] Sent");
                    now = DateTime.Now;
                    File.AppendAllText(@"DebugFFPLAY.txt", now.TimeOfDay.ToString() + "[FFPLAY] Sent" + Environment.NewLine);

                    /* if (mt.ffplayOutReader.Peek() > -1)
                    {
                        Console.WriteLine("[FFPLAY] OUT {0}", ffplayOutReader.ReadLine());
                    } */
                }
            }
            catch (Exception e)
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(@"ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.ToString() + Environment.NewLine);
                if (e.InnerException != null)
                    File.AppendAllText(@"ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.InnerException.ToString() + Environment.NewLine);
                // Debug.WriteLine("[FFPLAY] Exception caught {0}", e);
                d = null;
                // fmpReader.threadFFMPEG.Abort();
                // fmpReader.FFMPEGtoFFPLAY.Abort();
                fmpReader.program_end = true;

            }

        }

    }
}
