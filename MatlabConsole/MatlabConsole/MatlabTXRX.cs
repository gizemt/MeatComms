
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MatlabConsole
{
    public class MatlabTXRX
    {
        static MLApp.MLApp matlab;
        static MatlabTXRX mtx;

        double M;
        double fsym_tx;
        double fs_tx;
        double fc;

        double[,] output_data;
        double[,] excess_output;
        double t_end = 0;

        static void Main()
        {
            // var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here

            //try
            //{
            mtx = new MatlabTXRX();
            mtx.create_matlab_instance("C:\\Users\\Gizem\\Box\\Research\\Projects\\MeatComms\\Experiments\\C#");

            // mtx.test_struct_pass();
            
            mtx.initalize_tx(2.0, 10.0, 20.0, 0.0);
            
            byte[] input_data = { 1, 2, 3, 1, 2, 4, 5};//, 3, 2, 4, 6, 2, 5, 6, 7, 4, 3 };
                
            mtx.generate_tx_data(input_data);


            /*
            Debug.WriteLine(mtx.output_data.Length);
            Debug.WriteLine(mtx.excess_output.Length);
            Debug.WriteLine("Output data");
            for (int k = 0; k < mtx.output_data.GetLength(1); k++)
            {
                Debug.Write(" "+mtx.output_data[0, k]);
            }
            Debug.WriteLine("Excess Output");
            for (int k = 0; k < mtx.excess_output.GetLength(1); k++)
            {
                Debug.Write(" "+mtx.excess_output[0, k]);
            }
            mtx.generate_tx_data(input_data);
            Debug.WriteLine("");
            Debug.WriteLine(mtx.output_data.Length);
            Debug.WriteLine(mtx.excess_output.Length);
            Debug.WriteLine("Output data");
            for (int k = 0; k < mtx.output_data.GetLength(1); k++)
            {
                Debug.Write(" "+mtx.output_data[0, k]);
            }
            Debug.WriteLine("Excess Output");
            for (int k = 0; k < mtx.excess_output.GetLength(1); k++)
            {
                Debug.Write(" "+mtx.excess_output[0, k]);
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Execution Time: {0} ms", elapsedMs);
            */
            //}
            /*
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine(e.Source);
        } */

        }
        private void test_struct_pass()
        {
            object obj_struct_out;
            matlab.Feval("test_struct_out", 1, out obj_struct_out, 1, 2, 3);
            object[] res_out = obj_struct_out as object[];
            Debug.WriteLine(res_out);

            object obj_struct_in;
            matlab.Feval("test_struct_in", 3, out obj_struct_in, obj_struct_out);
            object[] res = obj_struct_in as object[];
            Debug.WriteLine(res[0]);
            Debug.WriteLine(res[1]);
            Debug.WriteLine(res[2]);
        }
        public struct rx_params
        {
            public int s1;
            public int s2;
            public int s3;
        }

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
        public void generate_tx_data(byte[] input_data)
        {
            object res = null;
            matlab.Feval("NIMultiThread_MATLAB_TX", 3, out res, input_data, mtx.M, mtx.fsym_tx, mtx.fs_tx, mtx.fc, mtx.excess_output, mtx.t_end);
            object[] tx_out_obj = res as object[];
            double[,] od;
            if (tx_out_obj[0] is double){
                od = new double[1,1];
                od[0,0] = (double)tx_out_obj[0];
            }
            else
            {
                od = (double[,])tx_out_obj[0]; 
                // var watch = System.Diagnostics.Stopwatch.StartNew();
                // od = ((double[,])tx_out_obj[0]).OfType<double>().ToArray();
                // watch.Stop();
                // var elapsedMs = watch.ElapsedMilliseconds;
                // Debug.WriteLine("Execution Time: {0} ms", elapsedMs);

            }
            mtx.output_data = od;
            double[,] exd;
            if (tx_out_obj[1] is double)
            {
                exd = new double[1, 1];
                exd[0, 0] = (double)tx_out_obj[1];
            }
            else
            {
                exd = (double[,])tx_out_obj[1];
            }
            mtx.excess_output = exd;
            // output_data = matlab.GetVariable("output_data", "base");
            // excess_output = matlab.GetVariable("excess_output", "base");
            mtx.t_end = (double)tx_out_obj[2];

        }

    }
}
