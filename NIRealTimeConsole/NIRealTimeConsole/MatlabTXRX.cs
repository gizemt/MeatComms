using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NIRealTimeConsole
{
    public class MatlabTXRX
    {
        // MatlabTXRX mtx;
        MLApp.MLApp matlab;

        double M;
        double fsym_tx;
        double fs_tx;
        double fs_rx;
        double fc;
        double Ts_tx;
        double sps_tx;
        double[] qammod_lookup_real;
        double[] qammod_lookup_imag;
        double[] rc_filt_tx;

        double[] b_butter;
        double[] a_butter;

        public double[,] excess_output;
        double[,] set_aside_real;
        double[,] set_aside_imag;

        /*
        public MatlabTXRX(double M, double fsym_tx, double fs_tx, double fc)
        {
            
            // var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here

            //try
            //{
            // mtx = new MatlabTXRX();
            mtx.create_matlab_instance("C:\\Users\\Gizem\\Box\\Research\\Projects\\MeatComms\\Experiments\\C#");
            // public void initalize_tx(double M, double fsym_tx, double fs_tx, double fc)
            mtx.initalize_tx(M, fsym_tx, fs_tx, fc);

            // byte[] input_data = { 1, 2, 3, 1, 2, 4, 5};//, 3, 2, 4, 6, 2, 5, 6, 7, 4, 3 };
                
            // mtx.generate_tx_data(input_data);
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
    } 

    }*/


        public void create_matlab_instance(string directory)
        {
            // Create the MATLAB instance 
            matlab = new MLApp.MLApp();

            // Change to the directory where the function is located
            string cmd = "cd " + directory;
            matlab.Execute(@cmd);
        }
        public void initalize_tx_params(double M_i, double fsym_tx_i, double fs_tx_i, double fc_i)
        {
            this.M = M_i;
            this.fsym_tx = fsym_tx_i;
            this.fs_tx = fs_tx_i;
            this.fc = fc_i;
            object init_out;
            matlab.Feval("initialize_NIMultiThread_MATLAB_TX", 5, out init_out, M_i, fsym_tx_i, fs_tx_i);
            object[] tx_init_obj = init_out as object[];
            this.Ts_tx = (double)tx_init_obj[0];
            this.sps_tx = (double)tx_init_obj[1];
            block_copy((double[,])tx_init_obj[2], out this.qammod_lookup_real);
            // this.qammod_lookup_real = (double[,])tx_init_obj[2];
            block_copy((double[,])tx_init_obj[3], out this.qammod_lookup_imag);
            // this.qammod_lookup_imag = (double[,])tx_init_obj[3];
            block_copy((double[,])tx_init_obj[4], out this.rc_filt_tx);
            set_aside_real = new double[1, this.rc_filt_tx.Length - 1];
            set_aside_imag = new double[1, this.rc_filt_tx.Length - 1];
            // this.rc_filt_tx = (double[,])tx_init_obj[4];
            // this.max_waveform_size = max_waveform_size;

        }
        public void block_copy(double[,] data, out double[] waveform)
        {
            var len = data.GetLength(1);
            waveform = new double[len];
            Buffer.BlockCopy(data, 0, waveform, 0, len * sizeof(double));
        }
        public double[] generate_tx_data(byte[] input_data, bool first_start, int max_waveform_size, ref double t_end)
        {
            object res = null;
            // input_data, M, Ts_tx, sps_tx, fc, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx,  excess_output, t_end, max_waveform_size
            matlab.Feval("NIMultiThread_MATLAB_TX", 5, out res, input_data, this.M, this.Ts_tx, this.sps_tx, this.fc, this.qammod_lookup_real, this.qammod_lookup_imag, this.rc_filt_tx, first_start, this.excess_output, this.set_aside_real, this.set_aside_imag, t_end, max_waveform_size);
            object[] tx_out_obj = res as object[];
            double[,] od;
            if (tx_out_obj[0] is double)
            {
                od = new double[1, 1];
                od[0, 0] = (double)tx_out_obj[0];
            }
            else
            {
                od = (double[,])tx_out_obj[0];
            }
            //this.output_data = od;
            var len = od.GetLength(1);
            double[] output_data = new double[len];
            Buffer.BlockCopy(od, 0, output_data, 0, len * sizeof(double));
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
            this.excess_output = exd;
            this.set_aside_real = (double[,])tx_out_obj[2];
            this.set_aside_imag = (double[,])tx_out_obj[3];
            t_end = (double)tx_out_obj[4];
            return output_data;

        }

        public void initalize_rx_params(double M, double fsym_tx, double fs_rx, double fc)
        {
            this.M = M;
            this.fsym_tx = fsym_tx;
            this.fs_rx = fs_rx;
            this.fc = fc;

            object res = null;
            matlab.Feval("initialize_NIMultiThread_MATLAB_RX", 2, out res, fs_rx, fsym_tx);
            object[] rx_out_obj = res as object[];
            double[,] bb = (double[,])rx_out_obj[0];
            double[,] ab = (double[,])rx_out_obj[1];
            this.b_butter = new double[bb.GetLength(1)];
            this.a_butter = new double[ab.GetLength(1)];
            Buffer.BlockCopy(bb, 0, b_butter, 0, b_butter.Length * sizeof(double));
            Buffer.BlockCopy(ab, 0, a_butter, 0, a_butter.Length * sizeof(double));
        }
        public double[,] process_rx_data(short[] x_rec, byte[] x_n, double nsym_train, double Frac, double N1, double N2, double Kf1, double Kf2, double Kg1, double Kg2, double lambda)
        {
            object res = null;
            double[,] od;

            matlab.Feval("NIMultiThread_MATLAB_RX_Loop", 1, out res, x_rec, x_n, this.M, this.fc, this.fsym_tx, this.fs_rx, nsym_train, Frac, this.b_butter, this.a_butter, N1, N2, Kf1, Kf2, Kg1, Kg2, lambda);
            object[] rx_data_out = res as object[];

            od = (double[,])rx_data_out[0];

            return od;

        }
    }
}
