using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace FFMPEGReaderDLL
{
    public class FFMPEGRead
    {

        public void FromFFPEGtoFFPLAY()
        {
            // Thread t = new Thread(new ThreadStart(someFunction));

            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
            ffmpegStartInfo.FileName = "C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe";
            ffmpegStartInfo.Arguments = "-y -f dshow -framerate 5 -i video=\"Logitech Webcam C925e\" -vf scale=160:120 -vcodec h264 -an -f nut pipe:1";
            ffmpegStartInfo.RedirectStandardError = true; // FFMPEG progress updates
            ffmpegStartInfo.RedirectStandardOutput = true; // FFMPEG data
            ffmpegStartInfo.RedirectStandardInput = false;
            ffmpegStartInfo.UseShellExecute = false;
            ffmpegStartInfo.CreateNoWindow = true;
            Process ffmpegProcess = new Process();
            ffmpegProcess.StartInfo = ffmpegStartInfo;

            // ffmpegProcess.EnableRaisingEvents = true;
            // ffmpegProcess.OutputDataReceived += (o, e) => Debug.WriteLine(e.Data ?? "NULL", "ffplay");
            // ffmpegProcess.ErrorDataReceived += (o, e) => Debug.WriteLine(e.Data ?? "NULL", "ffplay");
            // ffmpegProcess.Exited += (o, e) => Debug.WriteLine("Exited", "fp");

            Process ffplayProcess = new Process();
            ProcessStartInfo ffplayStartInfo = new ProcessStartInfo();
            ffplayStartInfo.FileName = "ffplay.exe";
            ffplayStartInfo.Arguments = "-loglevel 99 -i pipe: -autoexit";
            ffplayStartInfo.UseShellExecute = false;
            ffplayStartInfo.RedirectStandardInput = true;
            ffplayStartInfo.RedirectStandardOutput = false;
            ffplayStartInfo.RedirectStandardError = true;
            ffplayStartInfo.CreateNoWindow = false;
            ffplayProcess.StartInfo = ffplayStartInfo;


            try
            {

                ffmpegProcess.Start();

                ffplayProcess.Start();
                BinaryWriter dataWriter = new BinaryWriter(ffplayProcess.StandardInput.BaseStream);
                StreamReader ffplayErrReader = new StreamReader(ffplayProcess.StandardError.BaseStream);
                // StreamReader ffplayOutReader = new StreamReader(ffplayProcess.StandardOutput.BaseStream);

                // Data reader
                using (BinaryReader dataReader = new BinaryReader(ffmpegProcess.StandardOutput.BaseStream))
                {

                    using (StreamReader progReader = new StreamReader(ffmpegProcess.StandardError.BaseStream))
                    {
                        byte[] data = new byte[128];
                        string ffmpegProg;
                        int cnt;
                        int len;
                        cnt = 1;
                        int NUM_READ = 128;
                        Debug.WriteLine(progReader.ReadLine());
                        do
                        {
                            //try
                            //{
                            Debug.WriteLine('3');
                            // if (progReader.Peek() > -1)
                            // {
                            ffmpegProg = progReader.ReadLine();
                            Debug.WriteLine(ffmpegProg);
                            Debug.WriteLine('0');
                            // }

                            // Cannot peek to BaseStream
                            // if (dataReader.PeekChar() > -1)
                            // {

                            // If data array length = 128, FFPLAY starts at cnt = ~110 and hangs at 132
                            // len = dataReader.Read(data, 0, data.Length);
                            // dataWriter.Write(data, 0, len);

                            // If num_read = 128, FFPLAY starts at cnt = 52 and hangs at 96
                            dataWriter.Write(dataReader.ReadBytes(NUM_READ));
                            Debug.WriteLine('1');
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

                            if (ffplayErrReader.Peek() > -1)
                            {
                                Console.WriteLine("FFPLAY-ERR {0}", ffplayErrReader.ReadLine());
                                Debug.WriteLine('5');
                            }

                            //Console.WriteLine("FFPLAY-OUT {0}", ffplayOutReader.ReadLine());
                            Debug.WriteLine('2');
                            cnt += 1;

                            Debug.WriteLine(cnt);
                            // }



                        } while (cnt < 96);
                    }


                    // br.Dispose();
                    // br.Close();

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("{0} Exception caught.", e);
                // cnt = 0;
                // data = null;
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

    }
}
