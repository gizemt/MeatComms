function [Ts_tx,sps_tx, qammod_lookup_real, qammod_lookup_imag, rc_filt_tx] = initialize_NIMultiThread_MATLAB_TX(M, fsym_tx, fs_tx)
sps_tx = round(fs_tx/(fsym_tx*2))*2;
Ts_tx = 1/fs_tx;
qammod_lookup = qammod(0:(2^M-1), 2^M);
qammod_lookup_real = real(qammod_lookup);
qammod_lookup_imag = imag(qammod_lookup);
rc_filt_tx = rcosdesign(0.8, 2, sps_tx);
% set_aside = zeros(1,length(rc_filt_tx)-1);
end