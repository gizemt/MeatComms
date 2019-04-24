function [b_butter, a_butter] = initialize_NIMultiThread_MATLAB_RX(fs_rx, fsym)
[b_butter, a_butter] = butter(4, [1.15*fsym/(fs_rx/2)], 'low');
end
