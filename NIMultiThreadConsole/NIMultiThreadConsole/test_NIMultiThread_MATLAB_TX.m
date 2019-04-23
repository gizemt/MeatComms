input_data = x_dec;%[0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 1, 8; 3:7, 2:8, 0:3];
M = 2;
fsym_tx = 1e5;
fs_tx = 5e6;
fs_rx = fs_tx;
fc = 13e5;
excess_output = [];
t_end = 0;
max_waveform_size = 6000000;
first_start = 0;
[Ts_tx,sps_tx, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx] = initialize_NIMultiThread_MATLAB_TX(M, fsym_tx, fs_tx);
set_aside_real = zeros(1,(length(rc_filt_tx)-1));
set_aside_imag = zeros(1,(length(rc_filt_tx)-1));

output_data = [];
% for i = 1:2
% [output_data1, excess_output, set_aside_real, set_aside_imag, t_end] = NIMultiThread_MATLAB_TX(input_data(i,:), M, Ts_tx,sps_tx, fc, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx, first_start,  excess_output, set_aside_real, set_aside_imag, t_end, max_waveform_size);
% output_data = [output_data, output_data1];
% end
% figure,plot(output_data);hold on;plot(output_data(1:end-length(output_data1)));    

[output_data_full, excess_output_full, set_aside_real_full, set_aside_imag_full, t_end_full] = NIMultiThread_MATLAB_TX(input_data , M, Ts_tx,sps_tx, fc, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx, first_start,  [], zeros(1,length(set_aside_real)), zeros(1,length(set_aside_imag)), 0, max_waveform_size);
figure,plot(output_data_full);hold on;plot(output_data);hold on;plot(output_data(1:end-length(output_data1))); 
legend('Full', '2-parts', 'only first');
%     
% [output_data1, excess_output1, set_aside1, t_end1] = NIMultiThread_MATLAB_TX(input_data, M, Ts_tx,sps_tx, fc, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx,  excess_output, set_aside, t_end, max_waveform_size);
% input_data2 = [3, 4, 5, 6, 7, 8, 0, 1, 2];
% [output_data2, excess_output2, set_aside2, t_end2] = NIMultiThread_MATLAB_TX(input_data2, M, Ts_tx,sps_tx, fc, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx,  excess_output1, set_aside1, t_end1, max_waveform_size);
% output_data = [output_data1, output_data2];
% figure,plot(output_data);hold on;plot(output_data1);
% %% 
% fsym = fsym_tx;
% x_rec = [output_data_old; excess_output_old]';%output_data;
% fs = fs_rx;
% t_rx = (0:length(x_rec)-1)/fs;
% carrier_rx = 2*exp(-1j*(2*pi*(fc)*t_rx-pi/4));
% x_rec_bb = x_rec.*carrier_rx;
% [b_butter, a_butter] = butter(4, [1.15*fsym/(fs/2)], 'low');
% x_rec_filt = filtfilt(b_butter, a_butter, x_rec_bb);
% theta_zero = 0;
% fd_hat = 0;
% x_rec_dc_I = (real(x_rec_filt).*cos(theta_zero + 2*pi*fd_hat.*t_rx) + imag(x_rec_filt).*sin(theta_zero + 2*pi*fd_hat.*t_rx));%.*exp(1i*fd_hat.*t_rx);
% x_rec_dc_Q = (-real(x_rec_filt).*sin(theta_zero + 2*pi*fd_hat.*t_rx) + imag(x_rec_filt).*cos(theta_zero + 2*pi*fd_hat.*t_rx));%.*exp(1i*fd_hat.*t_rx);
% x_rec_dc = x_rec_dc_I + 1j*x_rec_dc_Q;
% sps_rx = fs_rx/fsym;
% %%
% i = 11;figure,scatter(real(x_rec_dc(i:sps_rx:end)), imag(x_rec_dc(i:sps_rx:end)), 'x');title(i);