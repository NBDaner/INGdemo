#define SBC_DECODER_BITS_EXTEND
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace INGdemo.Lib
{
    public class Maths
    {
        static public int CI(uint i)
        {
            return Convert.ToInt32(i);
        }

        static public int ASR(int val, int bits)
        {
            return val >> bits;
        }

        static public int SP4(int val) 
        {
            return val >> Constants.SCALE_PROTO4_TBL;
        }

        static public int SA4(int val)
        {
            return val >> Constants.SCALE_ANA4_TBL;
        }

        static public int SP8(int val)
        {
            return val >> Constants.SCALE_ANA4_TBL;
        }

        static public int SA8(int val)
        {
            return val >> Constants.SCALE_ANA8_TBL;
        }

        static public int SS4(int val)     
        {
            return val >> Constants.SCALE_SPROTO4_TBL;
        }

        static public int SS8(int val)     
        {
            return val >> Constants.SCALE_SPROTO8_TBL;
        }

        static public int SN4(int val)     
        {
            return val >> Constants.SCALE_NPROTO4_TBL;
        }

        static public int SN8(int val)     
        {
            return val >> Constants.SCALE_NPROTO8_TBL;
        }
        
        static public int SCALE4_STAGE1(int val)     
        {
            return val >> Constants.SCALE4_STAGE1_BITS;
        }

        static public int SCALE4_STAGE2(int val)     
        {
            return val >> Constants.SCALE4_STAGE2_BITS;
        }

        static public int SCALE8_STAGE1(int val)     
        {
            return val >> Constants.SCALE8_STAGE1_BITS;
        }

        static public int SCALE8_STAGE2(int val)     
        {
            return val >> Constants.SCALE8_STAGE2_BITS;
        }

        static public int SCALE4_STAGED1(int val)     
        {
            return val >> Constants.SCALE4_STAGED1_BITS;
        }

        static public int SCALE4_STAGED2(int val)     
        {
            return val >> Constants.SCALE4_STAGED2_BITS;
        }

        static public int SCALE8_STAGED1(int val)     
        {
            return val >> Constants.SCALE8_STAGED1_BITS;
        }

        static public int SCALE8_STAGED2(int val)     
        {
            return val >> Constants.SCALE8_STAGED2_BITS;
        }

        static public ushort sbc_common_sample_rate_get(uint idx)
        {
            return Tables.SBC_SAMPLE_RATES[idx];
        }

        static public byte sbc_common_crc8(byte[] data, uint len)
        {
            byte crc = 0x0f;
            int i;
            byte octet;

            for (i = 0; i < len / 8; i++)
                crc = Tables.SBC_CRC_TABLE[crc ^ data[i]];

            octet = (byte)((len % 8 == 0) ?  0 : data[i]);
            for (i = 0; i < len % 8; i++) {
                byte bit = (byte)(((octet ^ crc) & 0x80) >> 7);

                crc = (byte)(((crc & 0x7f) << 1) ^ (bit == 0 ? 0 : 0x1d));

                octet = (byte)(octet << 1);
            }

            return crc;
        }

        static public short __SSAT16(int s)
        {
            if(s > 0x7FFF)
                return 0x7FFF;
            else if(s < -0x8000)
                return -0x8000;
            else
                return (short)s;
        }

        public void sbc_decoder_synthesize_four(sbc_dec_info sbc)
        {
            int i, j, k, ch, blk, idx;

            for(ch = 0; ch < sbc.num_channels; ch++)
            {
                // int* vfifo  = sbc.vfifo[ch];
                // int* offset = sbc.offset[ch];
                int[] vfifo = new int[170];
                int[] offset = new int[16];
                Array.Copy(sbc.vfifo, ch * 170, vfifo, 0, 170);
                Array.Copy(sbc.offset, ch * 16, offset, 0, 16);

                for(blk = 0; blk < sbc.frame.blocks; blk++)
                {
                    //int* pcm = &sbc.pcm_sample[ch,blk * 4];
                    int[] pcm = new int[4];
                    Array.Copy(sbc.pcm_sample, ch * sbc.frame.blocks + blk * 4, pcm, 0, 4);


                    #if SBC_DECODER_BITS_EXTEND
                    long s;
                    #else
                    int s;
                    #endif

                    for(i = 0; i < 8; i++)
                    {
                        //int* synmatrix = (int*)SBC_DECODER_SYNMATRIX_4[i];
                        int[] synmatrix = new int[4];
                        Array.Copy(Tables.SBC_DECODER_SYNMATRIX_4, i * 4, synmatrix, 0, 4);

                        /* Shifting */
                        if(--offset[i] < 0)
                        {
                            offset[i] = 79;
                            // memcpy(vfifo + 80, vfifo, 9 * sizeof(*vfifo));
                        }

                        /* Distribute the new matrix value to the shifted position */
                        #if SBC_DECODER_BITS_EXTEND

                        s  = (long)synmatrix[0] * pcm[0];
                        s += (long)synmatrix[1] * pcm[1];
                        s += (long)synmatrix[2] * pcm[2];
                        s += (long)synmatrix[3] * pcm[3];
                        s >>= Constants.SCALE4_STAGED1_BITS;

                        vfifo[offset[i]] = (int)s;

                        #else

                        s  = synmatrix[0] * pcm[0];
                        s += synmatrix[1] * pcm[1];
                        s += synmatrix[2] * pcm[2];
                        s += synmatrix[3] * pcm[3];

                        vfifo[offset[i]] = SCALE4_STAGED1(s);

                        #endif
                    }

                    /* Compute the samples */
                    for(idx = 0, i = 0; i < 4; i++, idx += 5)
                    {
                        j = offset[i];
                        k = offset[i + 4];

                        /* Store in output, Q0 */
                        #if SBC_DECODER_BITS_EXTEND

                        s  = (long)vfifo[j + 0] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 0];
                        s += (long)vfifo[k + 1] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 0];
                        s += (long)vfifo[j + 2] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 1];
                        s += (long)vfifo[k + 3] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 1];
                        s += (long)vfifo[j + 4] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 2];
                        s += (long)vfifo[k + 5] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 2];
                        s += (long)vfifo[j + 6] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 3];
                        s += (long)vfifo[k + 7] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 3];
                        s += (long)vfifo[j + 8] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 4];
                        s += (long)vfifo[k + 9] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 4];
                        s >>= Constants.SCALE4_STAGED2_BITS;

                        //*pcm++ = __SSAT16((int)s);

                        #else
                        s  = vfifo[j + 0] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 0];
                        s += vfifo[k + 1] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 0];
                        s += vfifo[j + 2] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 1];
                        s += vfifo[k + 3] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 1];
                        s += vfifo[j + 4] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 2];
                        s += vfifo[k + 5] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 2];
                        s += vfifo[j + 6] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 3];
                        s += vfifo[k + 7] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 3];
                        s += vfifo[j + 8] * Tables.SBC_DECODER_PROTO_4_40M0[idx + 4];
                        s += vfifo[k + 9] * Tables.SBC_DECODER_PROTO_4_40M1[idx + 4];

                        //*pcm++ = SCALE4_STAGED1(s);

                        #endif
                    }
                }
            }
        }

        public void sbc_decoder_synthesize_eight(sbc_dec_info sbc)
        {
            int i, j, k, ch, blk, idx;

            for(ch = 0; ch < sbc.num_channels; ch++)
            {
                // int* vfifo  = sbc.vfifo[ch];
                // int* offset = sbc.offset[ch];
                int[] vfifo = new int[170];
                int[] offset = new int[16];
                Array.Copy(sbc.vfifo, ch * 170, vfifo, 0, 170);
                Array.Copy(sbc.offset, ch * 16, offset, 0, 16);

                for(blk = 0; blk < sbc.frame.blocks; blk++)
                {
                    // int* pcm = &sbc.pcm_sample[ch,blk * 8];
                    int[] pcm = new int[8];
                    Array.Copy(sbc.pcm_sample, ch * sbc.frame.blocks + blk * 8, pcm, 0, 4);

                    #if SBC_DECODER_BITS_EXTEND
                    long s;
                    #else
                    int s;
                    #endif

                    for(i = 0; i < 16; i++)
                    {
                        // int* synmatrix = (int*)SBC_DECODER_SYNMATRIX_8[i];
                        int[] synmatrix = new int[8];
                        Array.Copy(Tables.SBC_DECODER_SYNMATRIX_8, i * 8, synmatrix, 0, 8);
                        /* Shifting */
                        if(--offset[i] < 0)
                        {
                            offset[i] = 159;
                            // memcpy(vfifo + 160, vfifo, 9 * sizeof(*vfifo));
                        }

                        /* Distribute the new matrix value to the shifted position */
                        #if SBC_DECODER_BITS_EXTEND

                        s  = (long)synmatrix[0] * pcm[0];
                        s += (long)synmatrix[1] * pcm[1];
                        s += (long)synmatrix[2] * pcm[2];
                        s += (long)synmatrix[3] * pcm[3];
                        s += (long)synmatrix[4] * pcm[4];
                        s += (long)synmatrix[5] * pcm[5];
                        s += (long)synmatrix[6] * pcm[6];
                        s += (long)synmatrix[7] * pcm[7];
                        s >>= Constants.SCALE8_STAGED1_BITS;

                        vfifo[offset[i]] = (int)s;

                        #else

                        s  = synmatrix[0] * pcm[0];
                        s += synmatrix[1] * pcm[1];
                        s += synmatrix[2] * pcm[2];
                        s += synmatrix[3] * pcm[3];
                        s += synmatrix[4] * pcm[4];
                        s += synmatrix[5] * pcm[5];
                        s += synmatrix[6] * pcm[6];
                        s += synmatrix[7] * pcm[7];

                        vfifo[offset[i]] = SCALE8_STAGED1(s);

                        #endif
                    }

                    /* Compute the samples */
                    for(idx = 0, i = 0; i < 8; i++, idx += 5)
                    {
                        j = offset[i];
                        k = offset[i + 8];

                        /* Store in output */
                        #if SBC_DECODER_BITS_EXTEND

                        s  = (long)vfifo[j + 0] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 0];
                        s += (long)vfifo[k + 1] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 0];
                        s += (long)vfifo[j + 2] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 1];
                        s += (long)vfifo[k + 3] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 1];
                        s += (long)vfifo[j + 4] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 2];
                        s += (long)vfifo[k + 5] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 2];
                        s += (long)vfifo[j + 6] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 3];
                        s += (long)vfifo[k + 7] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 3];
                        s += (long)vfifo[j + 8] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 4];
                        s += (long)vfifo[k + 9] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 4];
                        s >>= Constants.SCALE8_STAGED2_BITS;

                        // *pcm++ = __SSAT16(s);

                        #else

                        s  = vfifo[j + 0] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 0];
                        s += vfifo[k + 1] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 0];
                        s += vfifo[j + 2] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 1];
                        s += vfifo[k + 3] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 1];
                        s += vfifo[j + 4] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 2];
                        s += vfifo[k + 5] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 2];
                        s += vfifo[j + 6] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 3];
                        s += vfifo[k + 7] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 3];
                        s += vfifo[j + 8] * Tables.SBC_DECODER_PROTO_8_80M0[idx + 4];
                        s += vfifo[k + 9] * Tables.SBC_DECODER_PROTO_8_80M1[idx + 4];

                        // *pcm++ = __SSAT16(SCALE8_STAGED2(s));

                        #endif
                    }
                }
            }
        }

                
        public void sbc_decoder_subband_synthesis_filter(sbc_dec_info sbc)
        {
            switch(sbc.frame.subbands)
            {
                case 4:
                    sbc_decoder_synthesize_four(sbc);
                    break;
                case 8:
                    sbc_decoder_synthesize_eight(sbc);
                    break;
                default:
                    break;
            }
        }

        public int sbc_decoder_frame_length_calc(sbc_dec_info sbc)
        {
            int frame_length;
            int blocks       = (int)sbc.frame.blocks;
            int subbands     = (int)sbc.frame.subbands;
            int num_channels = (int)sbc.num_channels;

            frame_length = 4 + ((4 * subbands * num_channels) >> 3);

            //for MONO and DUAL_CHANNEL
            if(sbc.frame.channel_mode < 2)
            {
                frame_length += (blocks * num_channels * (int)(sbc.frame.bitpool) + 7) >> 3;
            }
            //for STEREO and JOINT_STEREO
            else
            {
                frame_length += (((sbc.frame.channel_mode == Constants.SBC_CHANNEL_MODE_JOINT_STEREO) ? 1 : 0) * subbands + blocks * ((int)sbc.frame.bitpool) + 7) >> 3;
            }

            return frame_length;
        }

        public int sbc_decoder_decode(sbc_dec_info sbc, byte[] data, int length)
        {
            int ch;
            int sb;
            int blk;
            int consumed;

            sbc_frame_info frame = sbc.frame;

            /* frame_header */
            switch(data[0])
            {
                case Constants.SBC_SYNCWORD:
                    frame.sample_rate_index  = (sbyte)((data[1] >> 6) & 0x03);
                    frame.blocks             = (sbyte)((((data[1] >> 4) & 0x03) + 1) << 2);
                    frame.channel_mode       = (sbyte)((data[1] >> 2) & 0x03);
                    frame.allocation_method  = (sbyte)((data[1] >> 1) & 0x01);
                    frame.subbands           = (sbyte)(((data[1] & 0x01) + 1) << 2);
                    frame.bitpool            = data[2];
                    break;
                case Constants.MSBC_SYNCWORD:
                    frame.blocks = 15;
                    if(data[1]!=0 || data[2]!=0)
                    {
                        frame.sample_rate_index  = (sbyte)((data[1] >> 6) & 0x03);
                        frame.channel_mode       = (sbyte)((data[1] >> 2) & 0x03);
                        frame.allocation_method  = (sbyte)((data[1] >> 1) & 0x01);
                        frame.subbands           = (sbyte)(((data[1] & 0x01) + 1) << 2);
                        frame.bitpool            = data[2];
                    }
                    else
                    {
                        frame.sample_rate_index  = 0;
                        frame.channel_mode       = 0;
                        frame.allocation_method  = 0;
                        frame.subbands           = 8;
                        frame.bitpool            = 26;
                    }
                    break;
                default:
                    return (int)sbc_dec_err_code.SBC_DECODER_ERROR_SYNC_INCORRECT;
            }

            sbc.num_channels = (sbyte)(frame.channel_mode == Constants.SBC_CHANNEL_MODE_MONO ? 1 : 2);
            sbc.pcm_length   = (byte)(frame.blocks * frame.subbands);
            sbc.sample_rate  = sbc_common_sample_rate_get((uint)(frame.sample_rate_index));

            if(sbc_decoder_frame_length_calc(sbc) > length)
            {
                return (int)sbc_dec_err_code.SBC_DECODER_ERROR_STREAM_EMPTY;
            }

            if(((frame.channel_mode == Constants.SBC_CHANNEL_MODE_MONO   || frame.channel_mode == Constants.SBC_CHANNEL_MODE_DUAL_CHANNEL) && (frame.bitpool > (frame.subbands << 4))) ||
            ((frame.channel_mode == Constants.SBC_CHANNEL_MODE_STEREO || frame.channel_mode == Constants.SBC_CHANNEL_MODE_JOINT_STEREO) && (frame.bitpool > (frame.subbands << 5))))
            {
                return (int)sbc_dec_err_code.SBC_DECODER_ERROR_BITPOOL_OUT_BOUNDS;
            }

            consumed = 32;

            if(frame.channel_mode == Constants.SBC_CHANNEL_MODE_JOINT_STEREO)
            {
                uint join = 0;

                for(sb = 0; sb < frame.subbands - 1; sb++)
                {
                    join |= (uint)(((data[4] >> (7 - sb)) & 0x01) << sb);
                }

                frame.join = (byte)join;

                consumed += frame.subbands;
            }

            /* scale_factor */
            for(ch = 0; ch < sbc.num_channels; ch++)
            {
                // sbyte* sf = frame.scale_factor[ch];
                sbyte[] sf = new sbyte[8];
                Array.Copy(frame.scale_factor, ch*8, sf, 0, 8);

                for(sb = 0; sb < frame.subbands; sb++)
                {
                    sf[sb] = (sbyte)((data[consumed >> 3] >> (4 - (consumed & 0x7))) & 0x0F);
                    consumed += 4;
                }
            }

            /* bit_allocation */
            sbc_common_bit_allocation(frame);

            /* audio_samples & reconstruction */
            for(ch = 0; ch < sbc.num_channels; ch++)
            {
                // sbyte*  bits   = frame.bits[ch];
                // int* levels = frame.mem[ch];
                sbyte[] bits = new sbyte[8];
                int[] levels = new int[8];
                Array.Copy(frame.bits, ch*8, bits, 0, 8);
                Array.Copy(frame.mem, ch*8, levels, 0, 8);

                for(sb = 0; sb < frame.subbands; sb++)
                {
                    levels[sb] = (0x1 << bits[sb]) - 1;
                }
            }

            //目前不确定放在这儿是否靠谱
            int pcm_index = 0;

            for(blk = 0; blk < frame.blocks; blk++)
            {
                for(ch = 0; ch < sbc.num_channels; ch++)
                {
                    // sbyte*   bits   = frame.bits[ch];
                    // byte*  sf     = (byte*)frame.scale_factor[ch];
                    // uint* levels = (uint*)frame.mem[ch];
                    // int*  pcm    = &sbc.pcm_sample[ch,blk * frame.subbands];
                    sbyte[] bits = new sbyte[8];
                    byte[] sf = new byte[8];
                    uint[] levels = new uint[8];
                    int[] pcm = new int[frame.subbands];

                    
                    Array.Copy(frame.scale_factor, ch*8, sf, 0, 8);
                    Array.Copy(frame.mem, ch*8, levels, 0, 8);
                    Array.Copy(sbc.pcm_sample, ch*frame.blocks*frame.subbands + blk * frame.subbands, pcm, 0, frame.subbands);

                    for(sb = 0; sb < frame.subbands; sb++)
                    {
                        if(levels[sb] > 0)
                        {
                                int  bit;
                                uint value = 0;

                                for(bit = 0; bit < bits[sb]; bit++)
                                {
                                    if(((data[consumed >> 3] >> (7 - (consumed & 0x7))) & 0x01) != 0)
                                    {
                                        value |= (uint)(1 << (bits[sb] - bit - 1));
                                    }

                                    consumed++;
                                }

                            #if SBC_DECODER_BITS_EXTEND
                            long t = value;
                            pcm[pcm_index++] = (int)(((((t << 1) | 1) << (1 + sf[sb])) + (levels[sb] >> 1)) / levels[sb] - (1 << (1 + sf[sb])));       
                            #else
                            pcm[pcm_index++] = ((((value << 1) | 1) << sf[sb]) + (levels[sb] >> 1)) / levels[sb] - (1 << sf[sb]);
                            #endif

                        }
                        else
                        {
                            pcm[pcm_index++] = 0;
                        }
                    }
                }
            }

            /* joint_stereo */
            if(frame.channel_mode == Constants.SBC_CHANNEL_MODE_JOINT_STEREO)
            {
                int  idx, t0, t1;

                // int* pcm0 = sbc.pcm_sample[0];
                // int* pcm1 = sbc.pcm_sample[1];
                int[] pcm0 = new int[128];
                int[] pcm1 = new int[128];
                Array.Copy(sbc.pcm_sample,0,pcm0,0,128);
                Array.Copy(sbc.pcm_sample,128,pcm1,0,128);


                for(blk = 0; blk < frame.blocks; blk++)
                {
                    idx = blk * frame.subbands;

                    for(sb = 0; sb < frame.subbands; sb++)
                    {
                        if((frame.join & (0x01 << sb)) != 0)
                        {
                            t0 = pcm0[idx];
                            t1 = pcm1[idx];

                            pcm0[idx] = t0 + t1;
                            pcm1[idx] = t0 - t1;
                        }

                        idx++;
                    }
                }
            }

            /* padding 数据填充*/
            consumed = (int)(((consumed + 7) & 0xFFFFFFF8) >> 3);


            sbc_decoder_subband_synthesis_filter(sbc);

            if(sbc.num_channels == 2)
            {
                switch(sbc.output_pcm_width)
                {
                case 16:
                    {
                        int  i;
                        // int* src = (int*)sbc.pcm_sample[1];
                        // short* dst = (short*)sbc.pcm_sample + 1;
                        int[] src = new int[128];
                        short[] dst = new short[128];
                        Array.Copy(sbc.pcm_sample,0,src,0,128);
                        Array.Copy(sbc.pcm_sample,128,dst,0,128);

                        for(i = 0; i < sbc.pcm_length; i++)
                        {
                            *dst = *src++;
                            dst += 2;
                        }
                    }
                    break;
                case 24:
                    {
                        int  i;
                        // short* src16 = (short*)sbc.pcm_sample + sbc.pcm_length * 2 - 1;
                        // int* src32 = (int*)sbc.pcm_sample[1];
                        // short* dst16 = (short*)sbc.pcm_sample + 1;
                        // int* dst32 = (int*)sbc.pcm_sample + sbc.pcm_length * 2 - 1;
                        short[] src16 = new short[];
                        int[] src32 = new int[];
                        short[] dst16 = new short[];
                        int[] dst32 = new int[];


                        for(i = 0; i < sbc.pcm_length; i++)
                        {
                            *dst16 = *src32++;
                            dst16 += 2;
                        }

                        for(i = 0; i < sbc.pcm_length; i++)
                        {
                            *dst32-- = *src16-- << 8;
                            *dst32-- = *src16-- << 8;
                        }
                    }
                    break;
                default:
                    break;
                }
            }
            else
            {
                if(sbc.output_stereo_flag != 0)
                {
                    switch(sbc.output_pcm_width)
                    {
                    case 16:
                        {
                            int  i;
                            // int* src = (int*)sbc.pcm_sample[0];
                            // short* dst = (short*)sbc.pcm_sample + 1;
                            int[] src = new int[];
                            short[] dst = new short[];

                            for(i = 0; i < sbc.pcm_length; i++)
                            {
                                *dst = *src++;
                                dst += 2;
                            }
                        }
                        break;
                    case 24:
                        {
                            int  i, s;
                            int* src = (int*)&sbc.pcm_sample[0,sbc.pcm_length - 1];
                            int* dst = (int*)&sbc.pcm_sample[0,sbc.pcm_length * 2 - 1];

                            for(i = 0; i < sbc.pcm_length; i++)
                            {
                                s = *src-- << 8;
                                *dst-- = s;
                                *dst-- = s;
                            }
                        }
                        break;
                    default:
                        break;
                    }

                    sbc.num_channels = 2;
                }
                else
                {
                    switch(sbc.output_pcm_width)
                    {
                    case 16:
                        {
                            int  i;
                            int* src = (int*)sbc.pcm_sample[0];
                            short* dst = (short*)sbc.pcm_sample + 0;

                            for(i = 0; i < sbc.pcm_length; i++)
                            {
                                *dst++ = *src++;
                            }
                        }
                        break;
                    case 24:
                        {
                            int  i;
                            int* src = (int*)sbc.pcm_sample[0];
                            int* dst = (int*)sbc.pcm_sample[0];

                            for(i = 0; i < sbc.pcm_length; i++)
                            {
                                *dst++ = *src++ << 8;
                            }
                        }
                        break;
                    default:
                        break;
                    }
                }
            }

            return consumed;
        }

        int sbc_decoder_init(sbc_dec_info sbc)
        {
            int ch, i;

            //sbc_dec_info结构体初始化为0

            for(ch = 0; ch < 2; ch++)
            {
                for(i = 0; i < 8 * 2; i++)
                {
                    sbc.offset[ch,i] = (10 * i + 10);
                }
            }

            sbc.output_stereo_flag = 1;
            sbc.output_pcm_width   = 24;

            return (int)sbc_dec_err_code.SBC_DECODER_ERROR_OK;
        }

        int sbc_decoder_ctrl(sbc_dec_info sbc, sbc_dec_ctrl_cmd cmd, uint arg)
        {
            switch(cmd)
            {
            case sbc_dec_ctrl_cmd.SBC_DECODER_CTRL_CMD_GET_OUTPUT_STEREO_FLAG:
               arg = sbc.output_stereo_flag;
                break;
            case sbc_dec_ctrl_cmd.SBC_DECODER_CTRL_CMD_SET_OUTPUT_STEREO_FLAG:
                sbc.output_stereo_flag = (byte)arg;
                break;
            case sbc_dec_ctrl_cmd.SBC_DECODER_CTRL_CMD_GET_OUTPUT_PCM_WIDTH:
                arg = sbc.output_pcm_width;
                break;
            case sbc_dec_ctrl_cmd.SBC_DECODER_CTRL_CMD_SET_OUTPUT_PCM_WIDTH:
                sbc.output_pcm_width = (byte)arg;
                break;
            default:
                break;
            }

            return (int)sbc_dec_err_code.SBC_DECODER_ERROR_OK;
        }

        int sbc_decoder_deinit(sbc_dec_info sbc)
        {
            return (int)sbc_dec_err_code.SBC_DECODER_ERROR_OK;
        }

        public void sbc_common_bit_allocation(sbc_frame_info sbc)
        {
            int  ch;
            int  sb;
            int  slicecount;
            int  bitcount;
            int  bitslice;
            int  max_bitneed;
            int  loudness;

            sbyte[] sf = new sbyte[8];
            sbyte[] bits = new sbyte[8];
            int[] bitneed = new int[8];

            if((sbc.channel_mode == Constants.SBC_CHANNEL_MODE_MONO) || (sbc.channel_mode == Constants.SBC_CHANNEL_MODE_DUAL_CHANNEL))
            {
                for(ch = 0; ch < sbc.channel_mode + 1; ch++)
                {
                    // sf      = sbc.scale_factor[ch];
                    // bits    = sbc.bits[ch];
                    // bitneed = sbc.mem[ch];
                    Array.Copy(sbc.scale_factor,0,sf,0,8);
                    Array.Copy(sbc.bits,0,bits,0,8);
                    Array.Copy(sbc.mem,0,bitneed,0,8);


                    max_bitneed = 0;
            
                    if(sbc.allocation_method == Constants.SBC_ALLOCATION_METHOD_SNR)
                    {
                        for(sb = 0; sb < sbc.subbands; sb++)
                        {
                            bitneed[sb] = sf[sb];
                            if(bitneed[sb] > max_bitneed)
                            {
                                max_bitneed = bitneed[sb];
                            }
                        }
                    }
                    else
                    {
                        byte sri = (byte)sbc.sample_rate_index;

                        for(sb = 0; sb<sbc.subbands; sb++)
                        {
                            if(sf[sb] == 0)
                            {
                                bitneed[sb] = -5;
                            }
                            else
                            {
                                if(sbc.subbands == 4)
                                {
                                    loudness = sf[sb] - Tables.SBC_COMMON_OFFSET4[sri,sb];
                                }
                                else
                                {
                                    loudness = sf[sb] - Tables.SBC_COMMON_OFFSET8[sri,sb];
                                }

                                if(loudness > 0)
                                {
                                    bitneed[sb] = loudness / 2;
                                }
                                else
                                {
                                    bitneed[sb] = loudness;
                                }
                            }

                            if(bitneed[sb] > max_bitneed)
                            {
                                max_bitneed = bitneed[sb];
                            }
                        }
                    }

                    bitcount   = 0;
                    slicecount = 0;
                    bitslice   = max_bitneed + 1;

                    do
                    {
                        bitslice--;
                        bitcount += slicecount;
                        slicecount = 0;

                        for(sb = 0; sb < sbc.subbands; sb++)
                        {
                            if((bitneed[sb] > bitslice + 1) && (bitneed[sb] < bitslice + 16))
                            {
                                slicecount++;
                            }
                            else if(bitneed[sb] == bitslice + 1)
                            {
                                slicecount += 2;
                            }
                        }
                    }while(bitcount + slicecount < sbc.bitpool);

                    if(bitcount + slicecount == sbc.bitpool)
                    {
                        bitcount += slicecount;
                        bitslice--;
                    }

                    for(sb = 0; sb < sbc.subbands; sb++)
                    {
                        if(bitneed[sb] < bitslice + 2)
                        {
                            bits[sb] = 0;
                        }
                        else
                        {
                            bits[sb] = (sbyte)(bitneed[sb] - bitslice);
                            if(bits[sb] > 16)
                            {
                                bits[sb] = 16;
                            }
                        }
                    }

                    for(sb = 0; bitcount < sbc.bitpool && sb < sbc.subbands; sb++)
                    {
                        if((bits[sb] >= 2) && (bits[sb] < 16))
                        {
                            bits[sb]++;
                            bitcount++;
                        }
                        else if((bitneed[sb] == bitslice+1) && (sbc.bitpool > bitcount + 1))
                        {
                            bits[sb]  = 2;
                            bitcount += 2;
                        }
                    }

                    for(sb = 0; bitcount < sbc.bitpool && sb < sbc.subbands; sb++)
                    {
                        if(bits[sb] < 16)
                        {
                            bits[sb]++;
                            bitcount++;
                        }
                    }
                }
            }
            else if((sbc.channel_mode == Constants.SBC_CHANNEL_MODE_STEREO) || (sbc.channel_mode == Constants.SBC_CHANNEL_MODE_JOINT_STEREO))
            {
                max_bitneed = 0;
                if(sbc.allocation_method == Constants.SBC_ALLOCATION_METHOD_SNR)
                {
                    for(ch = 0; ch < 2; ch++)
                    {
                        // sf      = sbc.scale_factor[ch];
                        // bitneed = sbc.mem[ch];
                        Array.Copy(sbc.scale_factor,0,sf,0,8);
                        Array.Copy(sbc.mem,0,bitneed,0,8);

                        for(sb = 0; sb < sbc.subbands; sb++)
                        {
                            bitneed[sb] = sf[sb];

                            if(bitneed[sb] > max_bitneed)
                            {
                                max_bitneed = bitneed[sb];
                            }
                        }
                    }
                }
                else
                {
                    byte sri = (byte)sbc.sample_rate_index;

                    for(ch = 0; ch < 2; ch++)
                    {
                        // sf      = sbc.scale_factor[ch];
                        // bitneed = sbc.mem[ch];
                        Array.Copy(sbc.scale_factor,0,sf,0,8);
                        Array.Copy(sbc.mem,0,bitneed,0,8);

                        for(sb = 0; sb < sbc.subbands; sb++)
                        {
                            if(sf[sb] == 0)
                            {
                                bitneed[sb] = -5;
                            }
                            else
                            {
                                if(sbc.subbands == 4)
                                {
                                    loudness = sf[sb] - Tables.SBC_COMMON_OFFSET4[sri,sb];
                                }
                                else
                                {
                                    loudness = sf[sb] - Tables.SBC_COMMON_OFFSET8[sri,sb];
                                }

                                if(loudness > 0)
                                {
                                    bitneed[sb] = loudness / 2;
                                }
                                else
                                {
                                    bitneed[sb] = loudness;
                                }
                            }

                            if(bitneed[sb] > max_bitneed)
                            {
                                max_bitneed = bitneed[sb];
                            }
                        }
                    }
                }

                bitcount   = 0;
                slicecount = 0;
                bitslice   = max_bitneed + 1;

                do
                {
                    bitslice--;
                    bitcount += slicecount;
                    slicecount = 0;

                    for(ch = 0; ch < 2; ch++)
                    {
                        // bitneed = sbc.mem[ch];
                        Array.Copy(sbc.mem,0,bitneed,0,8);

                        for(sb = 0; sb < sbc.subbands; sb++)
                        {
                            if((bitneed[sb] > bitslice + 1) && (bitneed[sb] < bitslice + 16))
                            {
                                slicecount++;
                            }
                            else if(bitneed[sb] == bitslice + 1)
                            {
                                slicecount += 2;
                            }
                        }
                    }
                }while(bitcount + slicecount < sbc.bitpool);

                if(bitcount + slicecount == sbc.bitpool)
                {
                    bitcount += slicecount;
                    bitslice--;
                }

                for(ch = 0; ch < 2; ch++)
                {
                    // bits    = sbc.bits[ch];
                    // bitneed = sbc.mem[ch];
                    Array.Copy(sbc.bits,0,bits,0,8);
                    Array.Copy(sbc.mem,0,bitneed,0,8);

                    for(sb = 0; sb < sbc.subbands; sb++)
                    {
                        if(bitneed[sb] < bitslice + 2)
                        {
                            bits[sb] = 0;
                        }
                        else
                        {
                            bits[sb] = (sbyte)(bitneed[sb] - bitslice);
                            if(bits[sb] > 16)
                            {
                                bits[sb] = 16;
                            }
                        }
                    }
                }

                sb = 0;

                while(bitcount < sbc.bitpool)
                {
                    // bits    = sbc.bits[0];
                    // bitneed = sbc.mem[0];
                    Array.Copy(sbc.bits,0,bits,0,8);
                    Array.Copy(sbc.mem,0,bitneed,0,8);

                    if((bits[sb] >= 2) && (bits[sb] < 16))
                    {
                        bits[sb]++;
                        bitcount++;
                    }
                    else if((bitneed[sb] == bitslice + 1) && (sbc.bitpool > bitcount + 1))
                    {
                        bits[sb]  = 2;
                        bitcount += 2;
                    }

                    if(bitcount >= sbc.bitpool)
                    {
                        break;
                    }

                    // bits    = sbc.bits[1];
                    // bitneed = sbc.mem[1];
                    Array.Copy(sbc.bits,0,bits,0,8);
                    Array.Copy(sbc.mem,0,bitneed,0,8);

                    if((bits[sb] >= 2) && (bits[sb] < 16))
                    {
                        bits[sb]++;
                        bitcount++;
                    }
                    else if((bitneed[sb] == bitslice + 1) && (sbc.bitpool > bitcount + 1))
                    {
                        bits[sb]  = 2;
                        bitcount += 2;
                    }

                    if(++sb >= sbc.subbands)
                    {
                        break;
                    }
                }

                sb = 0;

                while(bitcount < sbc.bitpool)
                {
                    // bits = sbc.bits[0];
                    Array.Copy(sbc.bits,0,bits,0,8);

                    if(bits[sb] < 16)
                    {
                        bits[sb]++;

                        if(++bitcount >= sbc.bitpool)
                        {
                            break;
                        }                
                    }            

                    // bits = sbc.bits[1];
                    Array.Copy(sbc.bits,0,bits,0,8);

                    if(bits[sb] < 16)
                    {
                        bits[sb]++;
                        bitcount++;
                    }

                    if(++sb >= sbc.subbands)
                    {
                        break;
                    }
                }
            }
        }
    }      
}