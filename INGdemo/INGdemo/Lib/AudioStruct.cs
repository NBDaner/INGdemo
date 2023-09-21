using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace INGdemo.Lib
{   
    public enum Channels
    {
        MONO		    = Constants.SBC_MODE_MONO,
        DUAL_CHANNEL	= Constants.SBC_MODE_DUAL_CHANNEL,
        STEREO		    = Constants.SBC_MODE_STEREO,
        JOINT_STEREO	= Constants.SBC_MODE_JOINT_STEREO
    }

    public enum Allocate
    {
        LOUDNESS	= Constants.SBC_AM_LOUDNESS,
        SNR		    = Constants.SBC_AM_SNR
    }

    public class sbc_frame 
    {
        public byte frequency;
        public byte block_mode;
        public byte blocks;
        public Channels mode;
        public byte channels;
        public Allocate allocation;
        public byte subband_mode;
        public byte subbands;
        public byte bitpool;
        public ushort codesize;
        public ushort length;
        public ushort frame_count;
        public byte joint;  // bit number x set means joint stereo has been used in subband x 
        public uint[,]  scale_factor;   // only the lower 4 bits of every element are to be used
        public int[,,] sb_sample_f; // raw integer subband samples in the frame
        public int[,,] sb_sample;   // modified subband samples
        public short[,] pcm_sample; // original pcm audio samples
    }

    public class sbc_decoder_state
    {
        public int subbands;
        public int[,] V;
        public int[,] offset;
    }

    public class sbc_priv
    {
        public bool init;
        public sbc_frame frame;
        public sbc_decoder_state dec_state;
    }

    public class sbc_struct
    {
        public ulong flags;
        public byte frequency;
        public byte blocks;
        public byte subbands;
        public byte mode;
        public byte allocation;
        public byte bitpool;
        public byte endian;
        public sbc_priv priv;     
    }
}