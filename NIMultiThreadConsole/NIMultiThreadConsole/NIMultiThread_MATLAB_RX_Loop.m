function frame_vec_dfe = NIMultiThread_MATLAB_RX_Loop(x_rec_in, x_n, M, fc, fsym, fs_rx, nsym_train, Frac, b_butter, a_butter, N1, N2, Kf1, Kf2, Kg1, Kg2, lambda)
try
%%% Data
% x_rec, x_n, x_rec_prev
d_out = [];
x_rec_prev = [];
%%% Params
% sps, fs_rx, fc nsym_train, Frac, idx_low, idx_up, b_butter, a_butter
filter_pad_1 = 2;
filter_pad_2 = 2;
filter_gain = 1;
%%% refs passed from previous iteration
% ns_t, nsym, train_mode, dfe_start
ns_t = 0;
nsym = 0;
train_mode = 1;
dfe_start = 0;



ns_start = 0;
qam_lookup_table = qammod(0:(2^M-1), 2^M);

Ts = 1/fs_rx;
sps = fs_rx/fsym;

idx_low = N2*sps/Frac + filter_pad_1;
idx_up = N1*sps/Frac + filter_pad_2;

ns_idx = idx_low + 1;
x_rec = double(x_rec_in) * 0.0000685353588778526;
for i = (idx_low + 1):length(x_rec)
%         if barker_corr > 0.08
%             if corr_flag == 0
%                 ns_t = 0;
%                 ctr = 0;
% %                 if first_start ~= 1 && k ~= 1
% %                     k = k_init;
% %                 end        
%                 ns_start = i;
%                 ns_last_idx = i;
%                 corr_flag = 1;
%                 ns_idx = i;
%             end
            if rem(ns_t,sps) == 0
                if nsym > nsym_train
                    train_mode = 0;
                end
                if  (( ns_idx - idx_low) > 0) && ((( ns_idx + idx_up)) <= length(x_rec))% && ( ((length(x_n) > k_qammod ) || ~train_mode))
                    x_rec_s_padded = x_rec(( ns_idx - idx_low):( ns_idx + idx_up));
%                         x_rec_used = [x_rec_used(1,:) ( ns_idx - filter_pad_1):( ns_idx + samples_required_for_dfe + filter_pad_2);...
%                             x_rec_used(2,:) x_rec(( ns_idx - filter_pad_1):( ns_idx + samples_required_for_dfe + filter_pad_2))];
                    % tvec=((t_end:Ts_tx:(t_end+(length(x_p_tx)-1)*Ts)));
                    t_rx = (((ns_t - idx_low):(ns_t + idx_up)))*Ts;
                    carrier_rx = 2*exp(-1j*((2*pi*fc)*t_rx));
                    x_rec_bb = x_rec_s_padded.*carrier_rx;
%                     x_rec_bb_imag = x_rec_s_padded.*carrier_rx_imag;
                    % TODO: correct filter to include samples beyond the current range so that it
                    % will not pad/exclude anything
%                     x_rec_filt_padded = filter_gain*filter(b_butter, a_butter, x_rec_bb);
                    x_rec_filt_I = filter_gain*filter(b_butter, a_butter, real(x_rec_bb));
                    x_rec_filt_Q = filter_gain*filter(b_butter, a_butter, real(x_rec_bb));
                    x_rec_filt_padded = x_rec_filt_I + 1j*x_rec_filt_Q;
%                     x_rec_filt = x_rec_filt_padded((filter_pad_1+1):length(x_rec_filt_padded)-filter_pad_2);
                    x_rec_sym = x_rec_filt_padded((filter_pad_1+1):length(x_rec_filt_padded)-filter_pad_2);
                    % TODO: doppler and symbol timing estimates
%                     x_rec_dc = x_rec_filt;
%                     x_rec_sym = x_rec_filt;
                    x_rec_sym_padded = x_rec_filt_padded;
                    % Initialize DFE
                    if dfe_start == 0

                        % TODO: For the first Barker+guard sequence, find tap filter
                        % locations (meanwhile, change #of guard samples in the simulations 
                        % so that there will be enough amount of guard time 
                        b_ind = 1;

                        % Use these after finding tap filter locations
                        k_init = ceil(max(max(b_ind)*sps, (N2*sps)/Frac)/sps)+1;
        %                 i_end = floor((length(x_rec_sym)-N1*sps/Frac)/sps);
        %                 i_init = 1;
        %                 i_end = length(x_rec);

        %                 d_hat = 0;%zeros(1,i_end);
                        len_start = floor((length(x_rec) - ns_start - idx_up)/sps);
                        d = zeros(1,len_start);
                        d_tild = zeros(1,len_start);
                        
                        d_hat = zeros(1,len_start);
                        e = zeros(1,len_start);
                        theta_hat = zeros(1,len_start+1);
                        tao_hat = zeros(1,len_start+1);
                        
%                         x_qammod_init = qammod(x_n(1:k_init), 2^M);
                        x_qammod_init = qam_lookup_table(x_n(1:k_init)+1);
                        
%                         d_tild = x_qammod_init;
%                         d = d_tild(1:k_init);
                        d_tild(1:k_init) = x_qammod_init;
                        d(1:k_init) = d_tild(1:k_init);

                        k = k_init+1;
                        k_qammod = k_init+1;

                        phi_sum = 0;
                        psi_sum = 0;

                        a = (1+1j)*ones(1, (N1+N2)+1);
                        b = (1+1j)*ones(1, length(b_ind)); % Fb filter initialization
                        P = eye(length(b_ind)+(N1+N2)+1);

                        dfe_start = 1;
                    end

                    %%% This is where DFE will be
                    % go one symbol at a time, not sample
                    if nsym >= k_init
                        if train_mode
%                             d_tild(k) = qammod(x_n(k_qammod), 2^M);
                            d_tild(k) = qam_lookup_table(x_n(k_qammod)+1);
                        end
                        x_rec_sym_rev = x_rec_sym(1:(sps/Frac):length(x_rec_sym));
                        x_sym_vec = x_rec_sym_rev(length(x_rec_sym_rev):-1:1);%
            %             x_sym_vec = x_rec_sym((k*sps + N1*sps/Frac + tao_hat(k)):-(sps/Frac):(k*sps - N2*sps/Frac + tao_hat(k))); % v(n, tao_hat)
                        pn = a*(x_sym_vec.'*(exp(-1j*theta_hat(k))));
                        qn = b*d_tild(k-(b_ind)).';
                        d_hat(k) = pn - qn;
                        if ~train_mode
                            d_tild(k) = round((d_hat(k)-1-1j)/2)*2+1+1j;
                            if abs(real(d_tild(k))) > M-1
                                d_tild(k) = sign(real(d_tild(k)))*(M-1) + 1j*imag(d_tild(k));
                            end
                            if abs(imag(d_tild(k))) > M-1
                                d_tild(k) = 1j*sign(imag(d_tild(k)))*(M-1) + real(d_tild(k));
                            end
                        end
                        d(k) = d_tild(k);
                        e(k) = d(k) - d_hat(k);

                        xsymd = (x_sym_vec - (x_rec_sym_padded((length(x_rec_sym_padded)-filter_pad_2-1):-(sps/Frac):filter_pad_1)))/Ts;
                        x_sym_vec_dot = xsymd.';
                        % Updates
                        phi = imag(pn*conj(d(k) + qn));
                        epsi = exp(-1j*(theta_hat(k))*e(k)');
                        psi = real(a*(x_sym_vec_dot*epsi));

                        phi_sum = phi_sum + Kf2*phi;
                        psi_sum = psi_sum + Kg2*psi;

    %                     theta_hat = [theta_hat theta_hat(k) + Kf1*phi + phi_sum];
    %                     tao_hat = [tao_hat round(tao_hat(k) + Kg1*psi + psi_sum)];
                        theta_hat(k+1) = theta_hat(k) + Kf1*phi + phi_sum;
                        tao_hat(k+1) = round(tao_hat(k) + Kg1*psi + psi_sum);

                        u1 = conj(x_sym_vec)*exp(1j*theta_hat(k));
                        u2 = conj(d_tild(k-(b_ind)));
                        u = [u1 u2].';
                        c = [a -b];
                        alpha = d(k) - u'*c.';
                        Pu = (P*u);
                        g = Pu/(lambda + u'*Pu);
                        P = (P-g*(u'*P))/lambda;
                        c = c + alpha*g.';     
                        a = c(1:((N1+N2)+1));
                        b = (-c(((N1+N2)+2):length(c)));
                        
    %                     if train_mode
    %                         x_n(1:min((k-k_init),length(x_n))) = [];
    %                     end

                        k = k + 1;
                        k_qammod = k_qammod + 1;
                        ns_last_idx = ns_idx - idx_low + sps;
    %                     ns_last_t = ns_t;
                        nsym = nsym + 1;
                    else
                        nsym = nsym + 1;
                    end                    
                end
            end
            ns_t = ns_t + 1;
        ns_idx = ns_idx + 1;
    end
    loop_start = idx_low + 1;%ns_idx - ns_last_idx + 1;

    x_rec_prev = x_rec(( ns_last_idx):length(x_rec));
    ns_last_idx = 1;
    ns_t = ns_t - 1;

    if k > k_init + 1
        d_out = [d_out d(1:(k-k_init-1))];
        x_dfe_demod = qamdemod(d_out(1:floor((length(d_out))*M/8)*8/M), 2^M);
        x_dfe_demod_bi = de2bi(x_dfe_demod, M);
        frame_vec_dfe = bi2de(reshape(x_dfe_demod_bi', 8, (length(x_dfe_demod)*M/8))')';
%         frame_vec_dfe = [frame_vec_dfe_old bi2de(reshape(x_dfe_demod_bi', 8, (length(x_dfe_demod)*M/8))')'];
%         if length(frame_vec_dfe) == 1 || isequal(frame_vec_dfe(end-1:end), [222 5])
%         frame_vec_dfe_old = frame_vec_dfe;
%         frame_vec_dfe = [];
%         else
%             frame_vec_dfe_old = [];
%         end
%                         frame_vec_dfe = ones(size(frame_vec_dfe));
        d_out = d_out((floor(length(d_out)*M/8)*8/M+1):end);

        d = d((k-k_init):(k-1));% zeros(1,floor((length(x_rec) - ns_start - N1*sps/Frac - filter_pad_2)/sps)-k_init)];
        d_tild = d_tild((k-k_init):(k-1));% zeros(1,floor((length(x_rec) - ns_start - N1*sps/Frac - filter_pad_2)/sps)-k_init)];
        d_hat_plot = d_hat(1:k-k_init);
        d_hat = d_hat((k-k_init+1):(k-1));% zeros(1,floor((length(x_rec) - ns_start - N1*sps/Frac - filter_pad_2)/sps)-k_init)];
        e = e((k-k_init):(k-1));% zeros(1,floor((length(x_rec) - ns_start - N1*sps/Frac - filter_pad_2)/sps)-k_init)];
        theta_hat = theta_hat(k-k_init:k);% zeros(1,floor((length(x_rec) - ns_start - N1*sps/Frac - filter_pad_2)/sps)-k_init)];
        tao_hat = tao_hat(k-k_init:k);% zeros(1,floor((length(x_rec) - ns_start - N1*sps/Frac - filter_pad_2)/sps)-k_init)];
        k = k_init+1;
    else
        d = [];
    end
    if k > 1
        k = k_init;
    end
catch e
    error(strcat(e.message, num2str(e.stack(1).line)));
end
end