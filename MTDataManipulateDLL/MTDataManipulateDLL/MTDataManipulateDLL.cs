﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace FFMPEGReader
{
    public class MTDataManipulateDLL
    {

        byte[] data_from_ffmpeg;
        // byte[] data_to_ffplay;
        int buffer_end = 0;
        int last_idx = 0;
        int read_cnt = 1;


        int NUM_READ = 512;
        int BUFFER_SIZE = 8192;

        Boolean done_flag = false;
        Boolean program_end = false;

        BinaryWriter dataWriter;
        StreamReader ffplayOutReader;
        // bool first_data_read = false;
        Thread threadFFMPEG;
        Thread FFMPEGtoFFPLAY;

        // static readonly object _locker = new object();

        static MTDataManipulateDLL mt = new MTDataManipulateDLL();

        public void Main()
        {
            MTDataManipulateDLL mt = new MTDataManipulateDLL();
            

            threadFFMPEG = new Thread(() => mt.FFMPEGThread());
            FFMPEGtoFFPLAY = new Thread(() => mt.FFMPEGtoFFPLAYThread());

            threadFFMPEG.Start();
            FFMPEGtoFFPLAY.Start();

        }
        public bool get_program_end()
        {
            return mt.program_end;
        }

        private void FFMPEGThread()
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
            ffmpegStartInfo.RedirectStandardError = true; // FFMPEG progress updates
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
            mt.data_from_ffmpeg = new byte[mt.BUFFER_SIZE];
            try
            {
                // Data reader
                using (BinaryReader dataReader = new BinaryReader(ffmpegProcess.StandardOutput.BaseStream))
                {
                    using (StreamReader progReader = new StreamReader(ffmpegProcess.StandardError.BaseStream))
                    {
                        string ffmpegProg;
                        // int cnt = 0;
                        int local_last_idx = mt.last_idx;
                        int local_buffer_end = mt.buffer_end;
                        

                        Debug.WriteLine(progReader.ReadLine());
                        do
                        {
                            //try
                            //{
                            // Debug.WriteLine("[FFMPEG] Progress read");
                            // if (progReader.Peek() > -1)
                            // {
                            ffmpegProg = progReader.ReadLine();
                            // Debug.WriteLine(ffmpegProg);
                            DateTime now = DateTime.Now;
                            File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFMPEG.txt", now.TimeOfDay.ToString() + ffmpegProg);

                            // }

                            // Cannot peek to BaseStream
                            // if (dataReader.PeekChar() > -1)
                            // {
                            // lock (_locker)
                            // {
                            if (local_last_idx + NUM_READ <= BUFFER_SIZE)
                            {
                                local_last_idx += dataReader.Read(mt.data_from_ffmpeg, local_last_idx, NUM_READ);
                                // Debug.WriteLine("[FFMPEG] Data read until {0}.", local_last_idx);
                                now = DateTime.Now;
                                File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFMPEG.txt", now.TimeOfDay.ToString() + "[FFMPEG] Data read until " + local_last_idx.ToString());
                                mt.last_idx = local_last_idx;
                            }
                            else
                            {
                                //lock (mt)
                                //{
                                Debug.WriteLine("[FFMPEG] Buffer full, moving to the beginning.");
                                local_buffer_end = local_last_idx;
                                local_last_idx = dataReader.Read(mt.data_from_ffmpeg, 0, NUM_READ);
                                Debug.WriteLine("[FFMPEG] Finished at {0}, read until {1} into the beginning.", local_buffer_end, local_last_idx);
                                mt.last_idx = local_last_idx;
                                //}

                            }
                            // mt.first_data_read = true;

                            mt.buffer_end = local_buffer_end;
                            // }

                            // dataWriter.Write(data, 0, len);

                            // data = dataReader.ReadBytes(NUM_READ);

                            mt.read_cnt += 1;
                            // Debug.WriteLine(mt.read_cnt);
                            now = DateTime.Now;
                            File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFMPEG.txt", now.TimeOfDay.ToString() + "[FFMPEG] " + mt.read_cnt.ToString());
                            // }
                        } while (!mt.program_end);
                    }


                    // br.Dispose();
                    // br.Close();

                }
            }
            catch (Exception e)
            {
                // Debug.WriteLine("[FFMPEG] Exception caught {0}", e);
                DateTime now = DateTime.Now;
                File.WriteAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\ErrorFFMPEG.txt", now.TimeOfDay.ToString() + e.ToString());

                // mt.data_from_ffmpeg = null;
                buffer_end = 0;
                ffmpegProcess.Kill();
                mt.program_end = true;
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
                mt.threadFFMPEG.Abort();
                mt.program_end = true;
            };

            ffplayProcess.Start();

            mt.dataWriter = new BinaryWriter(ffplayProcess.StandardInput.BaseStream);
            mt.ffplayOutReader = new StreamReader(ffplayProcess.StandardOutput.BaseStream);
            // Debug.WriteLine("[FFPLAY] Started");
            DateTime now = DateTime.Now;
            File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFPLAY.txt", now.TimeOfDay.ToString() + "[FFPLAY] Started ");

        }

        public byte[] read_from_buffer(ref int prev_write_end, ref int last_checked, ref int n_cycle)
        {
            mt.done_flag = false;
            try
            {
                // https://stackoverflow.com/questions/4431568/variable-initalisation-in-while-loop
                // Debug.WriteLine("[FFPLAY] Waiting");
                while ((last_checked = mt.last_idx) <= prev_write_end) ;
                // Debug.WriteLine("[FFPLAY] Not waiting");
                byte[] d = new byte[last_checked - prev_write_end + 1];
                Array.Copy(mt.data_from_ffmpeg, prev_write_end, d, 0, last_checked - prev_write_end + 1);
                // Debug.WriteLine("[FFPLAY] Copied {0} points starting from {1}", last_checked - prev_write_end + 1, prev_write_end);
                DateTime now = DateTime.Now;
                File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFPLAY.txt", now.TimeOfDay.ToString() + "[FFPLAY] Copied ");
                // dataWriter.Write(mt.data_to_ffplay, prev_write_end, last_checked - prev_write_end);

                if (last_checked + NUM_READ > BUFFER_SIZE)
                {
                    prev_write_end = 0;
                    while ((n_cycle == mt.read_cnt)) ;
                    n_cycle++;
                    // while (!(mt.last_idx < last_checked)) ;
                }
                else
                {
                    prev_write_end = last_checked;
                }
                mt.done_flag = true;
                return d;
            }
            catch (Exception e)
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.ToString());
                if (e.InnerException != null)
                    File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.InnerException.ToString());
                // Debug.WriteLine("[FFPLAY] Exception caught {0}", e);
                // d = null;
                mt.threadFFMPEG.Abort();
                mt.FFMPEGtoFFPLAY.Abort();
                mt.done_flag = true;
                mt.program_end = true;
                return null;
            }
            
        }
            

        public void send_data_to_ffplay(byte[] d)
        {
            while (!mt.done_flag) ;
            try
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFPLAY.txt", now.TimeOfDay.ToString() + "d " + d.ToString());
                if (d != null)
                {
                    mt.dataWriter.Write(d);
                    // Debug.WriteLine("[FFPLAY] Sent");
                    now = DateTime.Now;
                    File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\DebugFFPLAY.txt", now.TimeOfDay.ToString() + "[FFPLAY] Sent");

                    /* if (mt.ffplayOutReader.Peek() > -1)
                    {
                        Console.WriteLine("[FFPLAY] OUT {0}", ffplayOutReader.ReadLine());
                    } */
                }
            }
            catch (Exception e)
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.ToString());
                if (e.InnerException != null)
                    File.AppendAllText(@"C:\Users\Gizem\Box\Research\Projects\MeatComms\Experiments\C#\ErrorFFPLAY.txt", now.TimeOfDay.ToString() + e.InnerException.ToString());
                // Debug.WriteLine("[FFPLAY] Exception caught {0}", e);
                d = null;
                mt.threadFFMPEG.Abort();
                mt.FFMPEGtoFFPLAY.Abort();
                mt.program_end = true;

            }

        }

    }
}
