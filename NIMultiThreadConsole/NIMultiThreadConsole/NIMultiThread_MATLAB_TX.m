function [output_data, excess_output, set_aside_real, set_aside_imag, t_end] = NIMultiThread_MATLAB_TX(input_data, M, Ts_tx,sps_tx, fc, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx, first_start,  excess_output, set_aside_real, set_aside_imag, t_end, max_waveform_size)
%%% Gizem Tabak   %%%
%%% April 3, 2019 %%%
% This function is generated from the TX Matlab script in C:\Users\Gizem\Box\Research\Projects\MeatComms\UG Researchers\Meatcomms-UG\Code
% input_data (1-D uint8 array): input data to be modulated
% M          (int)            : # of bits in QAM modulation, need to be a
                                % multiple of 2
% fsym_tx    (d)            : symbol rate
% fs_tx      (d)            : D/A sampling rate at transmitter and for AWG
% fc         (d)            : carrier frequency
% excess_output (1-D uint8 array): excess output data to be added at the beginning of the next TX cycle
% t_end      (float)          : carrier end time for the output_data
% output_data(1-D uint8 array): modulated output data
% sps_tx = round(fs_tx/(fsym_tx*2))*2;
% Ts_tx = 1/fs_tx;
input_bin = de2bi(input_data, 8)';
x_n_qam = bi2de(reshape(input_bin,M, size(input_bin,2)*8/M)')';
N_qam = length(x_n_qam);

% QAM-modulated signal
qammod_lookup = qammod_lookup_real + 1j*qammod_lookup_imag;
x_qammod_tx = qammod_lookup(x_n_qam+1);%qammod(x_n_qam, 2^M);
x_qam_nt_tx = zeros(1,N_qam*sps_tx);
x_qam_nt_tx(1:sps_tx:end) = x_qammod_tx;

% Pulse shaping (raised cosine) filter
% rc_filt_tx = rcosdesign(0.8, 2, sps_tx);
set_aside = set_aside_real + 1j*set_aside_imag;
x_p_tx_all = conv(x_qam_nt_tx, rc_filt_tx);
x_p_tx_all(1:length(set_aside)) = x_p_tx_all(1:length(set_aside)) + set_aside;
set_aside = x_p_tx_all((length(x_p_tx_all)-length(set_aside)+1):length(x_p_tx_all));
set_aside_real = real(set_aside);
set_aside_imag = imag(set_aside);
x_p_tx = x_p_tx_all(1:(length(x_p_tx_all)-length(set_aside)));
x_p_tx = x_p_tx/max(x_p_tx);
tvec=((t_end:Ts_tx:(t_end+(length(x_p_tx)-1)*Ts_tx)));
tx_carrier = exp(1j*(2*((pi*fc)*tvec)));
x_tx_c = (x_p_tx).*tx_carrier;
barker_seq = [1  1  1  1  1  -1  -1   1  1  -1  1  -1  1 ];
ns_guard = sps_tx;
x_guard = zeros(1,ns_guard);
% len_end = ns_guard + length(barker_seq);
% output_data_all = [barker_seq, x_guard, excess_output, real(x_tx_c)];
if first_start
    output_data_all = [barker_seq, x_guard, excess_output, real(x_tx_c)];
else
    output_data_all = [excess_output, real(x_tx_c)];
end
len_od = length(output_data_all);
% NI-FGEN requires the vector length to be multiples of 64, so the
% remainding elements are added at the beginning of the next vector
% also make sure the data generated is not greater than the max waveform size
excess_length = max(rem(len_od, 64), max(0, len_od - max_waveform_size + 1));
if excess_length == 0
    excess_length = 64;
end
output_data = output_data_all(1:len_od-excess_length);
excess_output = zeros(1,excess_length);
excess_output(1:excess_length) = output_data_all((len_od-excess_length+1):len_od);
% t_end = t_end + (length(output_data) - len_end)*Ts_tx;
t_end = t_end + (length(x_tx_c))*Ts_tx;
end