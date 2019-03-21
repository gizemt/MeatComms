using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace FFMPEGReader
{
    public class FFMPEGMultiThread
    {
        

        byte[] data;
        int buffer_end = 0;
        int last_idx = 0;
        int cnt = 1;
        int NUM_READ = 512;
        int BUFFER_SIZE = 8192;
        // bool first_data_read = false;
        

        // static readonly object _locker = new object();

        static FFMPEGMultiThread mt = new FFMPEGMultiThread();

        static void Main()
        {
            FFMPEGMultiThread mt = new FFMPEGMultiThread();
            Thread threadFFMPEG;
            Thread threadFFPLAY;

            threadFFMPEG = new Thread(() => mt.FFMPEGThread());
            threadFFPLAY = new Thread(() => mt.FFPLAYThread());

            threadFFMPEG.Start();
            threadFFPLAY.Start();
            

        }

        private void FFMPEGThread()
        {
            Process ffmpegP;
            FFMPEGStart(out ffmpegP);
            FFMPEGRead(ffmpegP);
        }

        private void FFPLAYThread()
        {
            Process ffplayP;
            FFPLAYStart(out ffplayP);
            FFPLAYWrite(ffplayP);
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

            try {
                // Data reader
                using (BinaryReader dataReader = new BinaryReader(ffmpegProcess.StandardOutput.BaseStream))
                {

                    using (StreamReader progReader = new StreamReader(ffmpegProcess.StandardError.BaseStream))
                    {
                        string ffmpegProg;
                        // int cnt = 0;
                        int local_last_idx = mt.last_idx;
                        int local_buffer_end = mt.buffer_end;
                        mt.data = new byte[mt.BUFFER_SIZE];


                        Debug.WriteLine(progReader.ReadLine());
                        do
                        {
                            //try
                            //{
                            // Debug.WriteLine("[FFMPEG] Progress read");
                            // if (progReader.Peek() > -1)
                            // {
                            ffmpegProg = progReader.ReadLine();
                            Debug.WriteLine(ffmpegProg);
                            // }

                            // Cannot peek to BaseStream
                            // if (dataReader.PeekChar() > -1)
                            // {
                            // lock (_locker)
                            // {
                            if (local_last_idx + NUM_READ <= BUFFER_SIZE)
                            {
                                local_last_idx += dataReader.Read(mt.data, local_last_idx, NUM_READ);
                                Debug.WriteLine("[FFMPEG] Data read until {0}.", local_last_idx);
                                mt.last_idx = local_last_idx;
                            }
                            else
                            {
                                //lock (mt)
                                //{
                                Debug.WriteLine("[FFMPEG] Buffer full, moving to the beginning.");
                                local_buffer_end = local_last_idx;
                                local_last_idx = dataReader.Read(mt.data, 0, NUM_READ);
                                Debug.WriteLine("[FFMPEG] Finished at {0}, read until {1} into the beginning.", local_buffer_end, local_last_idx);
                                mt.last_idx = local_last_idx;
                                //}
                                
                            }
                            // mt.first_data_read = true;
                            
                            mt.buffer_end = local_buffer_end;
                            // }
                            
                            
                            // dataWriter.Write(data, 0, len);

                            // data = dataReader.ReadBytes(NUM_READ);

                            mt.cnt += 1;
                            Debug.WriteLine(mt.cnt);
                        // }
                        } while (mt.cnt < 500) ;
                    }


                    // br.Dispose();
                    // br.Close();

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[FFMPEG] Exception caught {0}", e);
                mt.data = null;
                buffer_end = 0;
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

            ffplayProcess.Start();
            Debug.WriteLine("[FFPLAY] Started");
        }

        private void FFPLAYWrite(Process ffplayP)
        {
            
            BinaryWriter dataWriter = new BinaryWriter(ffplayP.StandardInput.BaseStream);
            // StreamReader ffplayErrReader = new StreamReader(ffplayP.StandardError.BaseStream);
            StreamReader ffplayOutReader = new StreamReader(ffplayP.StandardOutput.BaseStream);

            // If num_read = 128, FFPLAY starts at cnt = 52 and hangs at 96
            try
            {
                int write_cnt = 0;
                int prev_write_end = 0;
                int last_checked = 0;
                int n_cycle = 1;
                
                while (write_cnt < 2000)
                {
                    // https://stackoverflow.com/questions/4431568/variable-initalisation-in-while-loop
                    while ((last_checked = mt.last_idx) <= prev_write_end ) ;
                    dataWriter.Write(mt.data, prev_write_end, last_checked - prev_write_end);
                    Debug.WriteLine("[FFPLAY] Data between {0} and {1} sent to FFPLAY", prev_write_end, last_checked-1);
                    if (last_checked + NUM_READ > BUFFER_SIZE)
                    {
                        prev_write_end = 0;
                        
                        while ((n_cycle == mt.cnt)) ;
                        n_cycle += 1;
                        // while (!(mt.last_idx < last_checked)) ;
                    }
                    else
                    {
                        prev_write_end = last_checked;
                    }
                    
                    write_cnt += 1;

                    // if ((write_cnt + 1)*NUM_READ < BUFFER_SIZE)
                    // {
                    // 
                    //     dataWriter.Write(mt.data, write_cnt * NUM_READ, NUM_READ);
                    //     Debug.WriteLine("[FFPLAY] Sent data to FFPLAY");
                    //     write_cnt += 1;
                    // }
                    // else
                    // {
                    //     Debug.WriteLine("[FFPLAY] Reached end of buffer. Reading from the beginning.");
                    //     dataWriter.Write(mt.data, 0, NUM_READ);
                    //     Debug.WriteLine("[FFPLAY] Sent data to FFPLAY");
                    //     write_cnt = 0;
                    // }

                }

                // while ((len = dataReader.Read(data, 0, data.Length)) > 0)
                // {
                //     dataWriter.Write(data, 0, len);
                //     Debug.WriteLine('1');
                //     // }
                //     // if (ffplayErrReader.Peek() > -1)
                //     // {
                //     //     Console.WriteLine("FFPLAY-ERR {0}", ffplayErrReader.ReadLine());
                //     //     Debug.WriteLine('5');
                //     // }
                // }

                if (ffplayOutReader.Peek() > -1)
                {
                    Console.WriteLine("[FFPLAY] OUT {0}", ffplayOutReader.ReadLine());
                    // Debug.WriteLine('5');
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("[FFPLAY] Exception caught {0}", e);
            }
            

            //Console.WriteLine("FFPLAY-OUT {0}", ffplayOutReader.ReadLine());

        }
    }
}
