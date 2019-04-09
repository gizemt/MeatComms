using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MatlabConsole
{
    public class retired_MatlabTXRX
    {/*
        static MLApp.MLApp matlab;
        static MatlabTXRX mtx;
        
        double M;
        double fsym_tx;
        double fs_tx;
        double fc;
        
        static void Main()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here
            
            //try
            //{
            mtx = new MatlabTXRX();
            mtx.create_matlab_instance("C:\\Users\\Gizem\\Box\\Research\\Projects\\MeatComms\\Experiments\\C#");
            mtx.initalize_tx(2.0, 100000.0, 5000000.0, 1300000.0);

            byte[] input_data = { 1, 2, 3, 1, 2, 4, 5, 3, 2, 4, 6, 2, 5, 6, 7, 4, 3 };
            double[,] output_data;
            double[,] excess_output = null;
            double t_end = 0;
            mtx.generate_tx_data(input_data, out output_data, ref excess_output, ref t_end);
            

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Execution Time: {0} ms", elapsedMs);
            
            //}
                /*
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.Source);
            } 
            
        */}
/*    

        public void create_matlab_instance(string directory)
        {
            // Create the MATLAB instance 
            matlab = new MLApp.MLApp();

            // Change to the directory where the function is located
            string cmd = "cd " + directory;
            matlab.Execute(@cmd);
        }
        public void initalize_tx(double M, double fsym_tx, double fs_tx, double fc)
        {
            mtx.M = M;
            mtx.fsym_tx = fsym_tx;
            mtx.fs_tx = fs_tx;
            mtx.fc = fc;

        }
        public void generate_tx_data(byte[] input_data, out double[,] output_data, ref double[,] excess_output, ref double t_end)
        {
            object res = null;
            matlab.Feval("NIMultiThread_MATLAB_TX", 3, out res, input_data, mtx.M, mtx.fsym_tx, mtx.fs_tx, mtx.fc, excess_output, t_end);
            object[] tx_out_obj = res as object[];
            output_data = (double[,])tx_out_obj[0];
            excess_output = (double[,])tx_out_obj[1];
            // output_data = matlab.GetVariable("output_data", "base");
            // excess_output = matlab.GetVariable("excess_output", "base");
            t_end = (double)tx_out_obj[2];

        } 

    }
*/}