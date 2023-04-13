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
                int[] vfifo  = sbc.vfifo[ch];
                int[] offset = sbc.offset[ch];

                for(blk = 0; blk < sbc.frame.blocks; blk++)
                {
                    int[] pcm = &sbc.pcm_sample[ch][blk * 4];
                    
                    int s;


                    for(i = 0; i < 8; i++)
                    {
                        int[] synmatrix = new int[8];
                        Array.Copy(Tables.SBC_DECODER_SYNMATRIX_4,0,synmatrix,0,8);

                        /* Shifting */
                        if(--offset[i] < 0)
                        {
                            offset[i] = 79;
                            //memcpy(vfifo + 80, vfifo, 9 * sizeof(*vfifo));
                        }

                        /* Distribute the new matrix value to the shifted position */

                            s  = synmatrix[0] * pcm[0];
                            s += synmatrix[1] * pcm[1];
                            s += synmatrix[2] * pcm[2];
                            s += synmatrix[3] * pcm[3];

                            vfifo[offset[i]] = SCALE4_STAGED1(s);
                    }

                    /* Compute the samples */
                    for(idx = 0, i = 0; i < 4; i++, idx += 5)
                    {
                        j = offset[i];
                        k = offset[i + 4];

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

                        *pcm++ = SCALE4_STAGED1(s);

                    }
                }
            }
        }

        public sbc_dec_err_code sbc_decoder_deinit(sbc_dec_info sbc)
        {
            #if SBC_DECODER_HW_ACC_ENABLE
            REG_SYSTEM_0x20 |= (1 << 1);
            #endif

            return sbc_dec_err_code.SBC_DECODER_ERROR_OK;
        }
       
    }      
}