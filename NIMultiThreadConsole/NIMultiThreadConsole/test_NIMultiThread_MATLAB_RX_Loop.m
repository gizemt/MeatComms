function test_NIMultiThread_MATLAB_RX_Loop()
clear all;
M = 2;
fc = 13e5;
fsym = 1e5;
fs_tx = 5e6;
fs_rx = 5e6;
file_count = 17;

sample_offset = 0;
phase_offset = 0;

webcam = 1;
tx_byte_range = 0;

range = 3;
Ts_rx = 1/fs_rx;
file_path = 'C:\Users\Gizem\source\repos\NIMultiThreadConsole\NIMultiThreadConsole\bin\Debug\';
% file_path = './NI-data/';
rx_file_name = sprintf('%drx_waveform_fc%d_fsym_tx%d_fs_tx%d_fs_rx%d_webcam%d_tx_range%d.txt',file_count, fc, fsym, fs_tx, fs_rx, webcam, tx_byte_range);
x_rx = importdata(strcat(file_path, rx_file_name))';
if max(abs(x_rx)) > range/2
    gain = 0.0000685353588778526;%input('Enter gain (from Debug output): ');
    x_rx = x_rx*gain;
end
try
tx_file_name = sprintf('%dtx_symbols_fc%d_fsym_tx%d_fs_tx%d_fs_rx%d_webcam%d_tx_range%d.txt',file_count, fc, fsym, fs_tx, fs_rx, webcam, tx_byte_range);
x_dec = importdata(strcat(file_path, tx_file_name));
length_dec = find(x_dec, 1, 'last');
input_bin = de2bi(x_dec(1:length_dec), 8)';
x_n = bi2de(reshape(input_bin,M, size(input_bin,2)*8/M)')';
catch e
    disp(e.message);
    disp('TX file does not exist.\n');
end
%%
% niscid = input('Please enter NISCOPE fetch numbers: ');
% niscope_idx = [0;niscid]';
niscope_idx = cumsum([0
    167097
])';
% 167097
% 260256
% 254752
% 230016
% 330016
% 310016
% 370048

% niscope_idx = cumsum(niscope_num)';
%%
% figure,plot((1:length(x_rx))/fs_rx, x_rx);
figure,plot(x_rx);hold on;stem(niscope_idx, ones(1,length(niscope_idx)));
title(rx_file_name, 'interpreter', 'none');
% xlabel('Time (sec)');
disp(length(x_rx));
fprintf('(Approx) (Converted to TX) Samples with information = %d\n',(nnz(abs(x_rx) > 0.01))/(fs_rx/fs_tx));
fprintf('(Approx) (Converted to TX) Symbols with information = %d\n',((nnz(abs(x_rx) > 0.01))/(fs_rx/fs_tx))/(fs_tx/fsym));
fprintf('(Approx) TX Symbols sent = %d\n', length(x_n));
fprintf('(Approx) TX Bytes sent = %d\n', length_dec);

nsym_train = 10;
Frac = 2;
% window_length = 2*4;% Feedback filter taps window length (i.t.o. symbols)
N1 = 2; % Feedforward delay samples %25
N2 = 4; % Feedforward regular samples %150
% bk = 1; % fb filter 1st group length coefficient
% bth = 500/sqrt(filter_gain); % threshold coefficient
Kf1 = 5e-4; %0.03
Kf2 = 0.8e-4; %8e-4
Kg1 = 1e-11; %4e-10
Kg2 = 1e-12; %5e-11
lambda = 0.997; % RLS forgetting factor

[b_butter, a_butter] = butter(4, [1.15*fsym/(fs_rx/2)], 'low');
x_n = zeros(1,15);
tic;
frame_vec_dfe = NIMultiThread_MATLAB_RX_Loop(x_rx, x_n, M, fc, fsym, fs_rx, nsym_train, Frac, b_butter, a_butter, N1, N2, Kf1, Kf2, Kg1, Kg2, lambda);
toc;
end

