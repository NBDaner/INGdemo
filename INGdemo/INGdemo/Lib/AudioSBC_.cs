using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace INGdemo.Lib
{


static inline void sbc_decoder_synthesize_eight(SbcDecoderContext* sbc)
{
    int i, j, k, ch, blk, idx;

    for(ch = 0; ch < sbc->num_channels; ch++)
    {
        int* vfifo  = sbc->vfifo[ch];
        int* offset = sbc->offset[ch];

        for(blk = 0; blk < sbc->frame.blocks; blk++)
        {
            int* pcm = &sbc->pcm_sample[ch][blk * 8];

			#if SBC_DECODER_BITS_EXTEND == 0
            int s;
			#else
			int64_t s;
			#endif

            for(i = 0; i < 16; i++)
            {
                int* synmatrix = (int*)SBC_DECODER_SYNMATRIX_8[i];

                /* Shifting */
                if(--offset[i] < 0)
                {
                    offset[i] = 159;
                    memcpy(vfifo + 160, vfifo, 9 * sizeof(*vfifo));
                }

                /* Distribute the new matrix value to the shifted position */
				#if SBC_DECODER_BITS_EXTEND == 0

                s  = synmatrix[0] * pcm[0];
                s += synmatrix[1] * pcm[1];
                s += synmatrix[2] * pcm[2];
                s += synmatrix[3] * pcm[3];
                s += synmatrix[4] * pcm[4];
                s += synmatrix[5] * pcm[5];
                s += synmatrix[6] * pcm[6];
                s += synmatrix[7] * pcm[7];

                vfifo[offset[i]] = SCALE8_STAGED1(s);

				#else

				s  = (int64_t)synmatrix[0] * pcm[0];
                s += (int64_t)synmatrix[1] * pcm[1];
                s += (int64_t)synmatrix[2] * pcm[2];
                s += (int64_t)synmatrix[3] * pcm[3];
                s += (int64_t)synmatrix[4] * pcm[4];
                s += (int64_t)synmatrix[5] * pcm[5];
                s += (int64_t)synmatrix[6] * pcm[6];
                s += (int64_t)synmatrix[7] * pcm[7];
				s >>= SCALE8_STAGED1_BITS;

                vfifo[offset[i]] = s;

				#endif
            }

            /* Compute the samples */
            for(idx = 0, i = 0; i < 8; i++, idx += 5)
            {
                j = offset[i];
                k = offset[i + 8];

                /* Store in output */
				#if SBC_DECODER_BITS_EXTEND == 0

                s  = vfifo[j + 0] * SBC_DECODER_PROTO_8_80M0[idx + 0];
                s += vfifo[k + 1] * SBC_DECODER_PROTO_8_80M1[idx + 0];
                s += vfifo[j + 2] * SBC_DECODER_PROTO_8_80M0[idx + 1];
                s += vfifo[k + 3] * SBC_DECODER_PROTO_8_80M1[idx + 1];
                s += vfifo[j + 4] * SBC_DECODER_PROTO_8_80M0[idx + 2];
                s += vfifo[k + 5] * SBC_DECODER_PROTO_8_80M1[idx + 2];
                s += vfifo[j + 6] * SBC_DECODER_PROTO_8_80M0[idx + 3];
                s += vfifo[k + 7] * SBC_DECODER_PROTO_8_80M1[idx + 3];
                s += vfifo[j + 8] * SBC_DECODER_PROTO_8_80M0[idx + 4];
                s += vfifo[k + 9] * SBC_DECODER_PROTO_8_80M1[idx + 4];

                *pcm++ = __SSAT16(SCALE8_STAGED2(s));

				#else

				s  = (int64_t)vfifo[j + 0] * SBC_DECODER_PROTO_8_80M0[idx + 0];
                s += (int64_t)vfifo[k + 1] * SBC_DECODER_PROTO_8_80M1[idx + 0];
                s += (int64_t)vfifo[j + 2] * SBC_DECODER_PROTO_8_80M0[idx + 1];
                s += (int64_t)vfifo[k + 3] * SBC_DECODER_PROTO_8_80M1[idx + 1];
                s += (int64_t)vfifo[j + 4] * SBC_DECODER_PROTO_8_80M0[idx + 2];
                s += (int64_t)vfifo[k + 5] * SBC_DECODER_PROTO_8_80M1[idx + 2];
                s += (int64_t)vfifo[j + 6] * SBC_DECODER_PROTO_8_80M0[idx + 3];
                s += (int64_t)vfifo[k + 7] * SBC_DECODER_PROTO_8_80M1[idx + 3];
                s += (int64_t)vfifo[j + 8] * SBC_DECODER_PROTO_8_80M0[idx + 4];
                s += (int64_t)vfifo[k + 9] * SBC_DECODER_PROTO_8_80M1[idx + 4];
				s >>= SCALE8_STAGED2_BITS;

				*pcm++ = __SSAT16(s);

				#endif
            }
        }
    }
}

static void sbc_decoder_subband_synthesis_filter(SbcDecoderContext* sbc)
{
    switch(sbc->frame.subbands)
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

#endif

static int sbc_decoder_frame_length_calc(SbcDecoderContext* sbc)
{
    int frame_length;
    int blocks       = (int)sbc->frame.blocks;
    int subbands     = (int)sbc->frame.subbands;
    int num_channels = (int)sbc->num_channels;

    frame_length = 4 + ((4 * subbands * num_channels) >> 3);

    //for MONO and DUAL_CHANNEL
    if(sbc->frame.channel_mode < 2)
    {
        frame_length += (blocks * num_channels * (int)(sbc->frame.bitpool) + 7) >> 3;
    }
    //for STEREO and JOINT_STEREO
    else
    {
        frame_length += ((sbc->frame.channel_mode == SBC_CHANNEL_MODE_JOINT_STEREO) * subbands + blocks * ((int)sbc->frame.bitpool) + 7) >> 3;
    }

    return frame_length;
}

int sbc_decoder_init(SbcDecoderContext* sbc)
{
    int ch, i;

    #if SBC_DECODER_HW_ACC_ENABLE
    REG_SYSTEM_0x20 &= ~(1 << 1);
    REG_SBC_RES_BYTEL = 0;
    REG_SBC_RES_BYTEM = 0;
    REG_SBC_RES_BYTEH = 0;
    REG_SBC_BIT_NUM = 0;
    REG_SBC_SF = 0;
    REG_SBC_LEVEL = 0;
    REG_SBC_RES_BIT = 0;
    {
        uint* p = (uint*)&REG_SBC_MEM0;
        for(i = 0; i < (256+340); i++) *p++ = 0;
    }
    #endif

    memset((void*)sbc, 0, sizeof(SbcDecoderContext));

    for(ch = 0; ch < 2; ch++)
    {
        for(i = 0; i < 8 * 2; i++)
        {
            sbc->offset[ch][i] = (10 * i + 10);
        }
    }

    sbc->output_stereo_flag = 1;
    sbc->output_pcm_width   = 24;

    return SBC_DECODER_ERROR_OK;
}

int sbc_decoder_ctrl(SbcDecoderContext* sbc, uint cmd, uint arg)
{
    switch(cmd)
    {
    case SBC_DECODER_CTRL_CMD_GET_OUTPUT_STEREO_FLAG:
        *(uint*)arg = sbc->output_stereo_flag;
        break;
    case SBC_DECODER_CTRL_CMD_SET_OUTPUT_STEREO_FLAG:
        sbc->output_stereo_flag = arg;
        break;
    case SBC_DECODER_CTRL_CMD_GET_OUTPUT_PCM_WIDTH:
        *(uint*)arg = sbc->output_pcm_width;
        break;
    case SBC_DECODER_CTRL_CMD_SET_OUTPUT_PCM_WIDTH:
        sbc->output_pcm_width = arg;
        break;
    default:
        break;
    }

    return SBC_DECODER_ERROR_OK;
}

int sbc_decoder_decode(SbcDecoderContext* sbc, const usbyte* data, int length)
{
    int ch;
    int sb;
    int blk;
    int consumed;

    SbcCommonContext* frame = &sbc->frame;

    /* frame_header */
    switch(data[0])
    {
    case SBC_SYNCWORD:
        frame->sample_rate_index  = (data[1] >> 6) & 0x03;
        frame->blocks             = (((data[1] >> 4) & 0x03) + 1) << 2;
        frame->channel_mode       = (data[1] >> 2) & 0x03;
        frame->allocation_method  = (data[1] >> 1) & 0x01;
        frame->subbands           = ((data[1] & 0x01) + 1) << 2;
        frame->bitpool            = data[2];
        break;
    case MSBC_SYNCWORD:
        frame->blocks = 15;
        if(data[1] || data[2])
        {
            frame->sample_rate_index  = (data[1] >> 6) & 0x03;
            frame->channel_mode       = (data[1] >> 2) & 0x03;
            frame->allocation_method  = (data[1] >> 1) & 0x01;
            frame->subbands           = ((data[1] & 0x01) + 1) << 2;
            frame->bitpool            = data[2];
        }
        else
        {
            frame->sample_rate_index  = 0;
            frame->channel_mode       = 0;
            frame->allocation_method  = 0;
            frame->subbands           = 8;
            frame->bitpool            = 26;
        }
        break;
    default:
        return SBC_DECODER_ERROR_SYNC_INCORRECT;
    }

    sbc->num_channels = frame->channel_mode == SBC_CHANNEL_MODE_MONO ? 1 : 2;
    sbc->pcm_length   = frame->blocks * frame->subbands;
    sbc->sample_rate  = sbc_common_sample_rate_get(frame->sample_rate_index);

    if(sbc_decoder_frame_length_calc(sbc) > length)
    {
        return SBC_DECODER_ERROR_STREAM_EMPTY;
    }

    if(((frame->channel_mode == SBC_CHANNEL_MODE_MONO   || frame->channel_mode == SBC_CHANNEL_MODE_DUAL_CHANNEL) && (frame->bitpool > (frame->subbands << 4))) ||
       ((frame->channel_mode == SBC_CHANNEL_MODE_STEREO || frame->channel_mode == SBC_CHANNEL_MODE_JOINT_STEREO) && (frame->bitpool > (frame->subbands << 5))))
    {
        return SBC_DECODER_ERROR_BITPOOL_OUT_BOUNDS;
    }

    consumed = 32;

    if(frame->channel_mode == SBC_CHANNEL_MODE_JOINT_STEREO)
    {
        int  sb;
        uint join = 0;

        for(sb = 0; sb < frame->subbands - 1; sb++)
        {
            join |= ((data[4] >> (7 - sb)) & 0x01) << sb;
        }

        frame->join = (usbyte)join;

        consumed += frame->subbands;
    }

    /* scale_factor */
    for(ch = 0; ch < sbc->num_channels; ch++)
    {
        sbyte* sf = frame->scale_factor[ch];

        for(sb = 0; sb < frame->subbands; sb++)
        {
            sf[sb] = (data[consumed >> 3] >> (4 - (consumed & 0x7))) & 0x0F;;
            consumed += 4;
        }
    }

    /* bit_allocation */
    sbc_common_bit_allocation(frame);

    /* audio_samples & reconstruction */
    for(ch = 0; ch < sbc->num_channels; ch++)
    {
        sbyte*  bits   = frame->bits[ch];
        int* levels = frame->mem[ch];

        for(sb = 0; sb < frame->subbands; sb++)
        {
            levels[sb] = (0x1 << bits[sb]) - 1;
        }
    }

    for(blk = 0; blk < frame->blocks; blk++)
    {
        for(ch = 0; ch < sbc->num_channels; ch++)
        {
            sbyte*   bits   = frame->bits[ch];
            usbyte*  sf     = (usbyte*)frame->scale_factor[ch];
            uint* levels = (uint*)frame->mem[ch];

            #if SBC_DECODER_HW_ACC_ENABLE
            int*  pcm    = (int*)(&REG_SBC_MEM0 + ch * sbc->pcm_length + blk * frame->subbands);
            #else
            int*  pcm    = &sbc->pcm_sample[ch][blk * frame->subbands];
            #endif

            for(sb = 0; sb < frame->subbands; sb++)
            {
                if(levels[sb] > 0)
                {
                    #if SBC_DECODER_HW_ACC_ENABLE
                    {
                         int consum_byte = consumed >> 3;

                         REG_SBC_BIT_NUM   = bits[sb];
                         REG_SBC_SF        = sf[sb];
                         REG_SBC_LEVEL     = levels[sb];
                         REG_SBC_RES_BIT   = consumed & 0x7;
                         REG_SBC_RES_BYTEH = data[consum_byte+2];
                         REG_SBC_RES_BYTEM = data[consum_byte+1];
                         REG_SBC_RES_BYTEL = data[consum_byte+0];
                         REG_SBC_DEC_EN    = 0x01;
                         while(REG_SBC_DEC_EN);
                         *pcm++ = REG_SBC_PCM;
                         consumed += bits[sb];
                    }
                    #else
                    {
                        int  bit;
                        uint value = 0;

                        #if SBC_DECODER_BITS_EXTEND == 1
                        int64_t  t;
                        #endif

                        for(bit = 0; bit < bits[sb]; bit++)
                        {
                            if((data[consumed >> 3] >> (7 - (consumed & 0x7))) & 0x01)
                            {
                                value |= 1 << (bits[sb] - bit - 1);
                            }

                            consumed++;
                        }

                        #if SBC_DECODER_BITS_EXTEND == 0

                        *pcm++ = ((((value << 1) | 1) << sf[sb]) + (levels[sb] >> 1)) / levels[sb] - (1 << sf[sb]);

                        #else

                        t = value;
                        *pcm++ = ((((t << 1) | 1) << (1 + sf[sb])) + (levels[sb] >> 1)) / levels[sb] - (1 << (1 + sf[sb]));

                        #endif
                    }
                    #endif
                }
                else
                {
                    *pcm++ = 0;
                }
            }
        }
    }

    /* joint_stereo */
    if(frame->channel_mode == SBC_CHANNEL_MODE_JOINT_STEREO)
    {
        int  idx, t0, t1;

        #if SBC_DECODER_HW_ACC_ENABLE
        int* pcm0 = (int*)(&REG_SBC_MEM0);
        int* pcm1 = (int*)(&REG_SBC_MEM0 + sbc->pcm_length);
        #else
        int* pcm0 = sbc->pcm_sample[0];
        int* pcm1 = sbc->pcm_sample[1];
        #endif

        for(blk = 0; blk < frame->blocks; blk++)
        {
            idx = blk * frame->subbands;

            for(sb = 0; sb < frame->subbands; sb++)
            {
                if(frame->join & (0x01 << sb))
                {
                    t0 = pcm0[idx];
                    t1 = pcm1[idx];

                    pcm0[idx] = t0 + t1;
                    #if SBC_DECODER_HW_ACC_ENABLE
                    __asm__("b.nop 5"); //Around hardware bug when SBC and DSP access AHB bus at same time
                    #endif
                    pcm1[idx] = t0 - t1;
                }

                idx++;
            }
        }
    }

    /* padding */
    consumed = ((consumed + 7) & 0xFFFFFFF8) >> 3;

    #if SBC_DECODER_HW_ACC_ENABLE
    {
        REG_SBC_CTRL = (MSK_SBC_CTRL_EN) |
                       (sbc->num_channels & MSK_SBC_CTRL_CHANNEL) |
                       ((frame->subbands >> 1) & MSK_SBC_CTRL_SUBBAND) |
                       (sbc->num_channels == 2 ? MSK_SBC_CTRL_CHN_COMB : 0) |
                       ((frame->blocks / 4 - 1) << SFT_SBC_CTRL_BLOCKS) |
                       ((frame->blocks == 15) << SFT_SBC_CTRL_MSBC_FLAG);

        while(REG_SBC_CTRL & MSK_SBC_CTRL_EN)
        {
            void system_cpu_halt(void);
            system_cpu_halt();
        }

        if(sbc->num_channels == 2)
        {
            switch(sbc->output_pcm_width)
            {
            case 16:
                {
                    int  i;
                    int* src = (int*)&REG_SBC_MEM0;
                    int* dst = (int*)sbc->pcm_sample;

                    for(i = 0; i < sbc->pcm_length; i++)
                    {
                        *dst++ = *src++;
                    }
                }
                break;
            case 24:
                {
                    int  i;
                    short* src = (short*)&REG_SBC_MEM0;
                    int* dst = (int*)sbc->pcm_sample;

                    for(i = 0; i < sbc->pcm_length; i++)
                    {
                        *dst++ = *src++ << 8;
                        *dst++ = *src++ << 8;
                    }
                }
                break;
            default:
                break;
            }
        }
        else
        {
            if(sbc->output_stereo_flag)
            {
                switch(sbc->output_pcm_width)
                {
                case 16:
                    {
                        int  i, s;
                        int* src = (int*)&REG_SBC_MEM0;
                        short* dst = (short*)sbc->pcm_sample;

                        for(i = 0; i < sbc->pcm_length; i++)
                        {
                            s = *src++;
                            *dst++ = s;
                            *dst++ = s;
                        }
                    }
                    break;
                case 24:
                    {
                        int  i, s;
                        int* src = (int*)&REG_SBC_MEM0;
                        int* dst = (int*)sbc->pcm_sample;

                        for(i = 0; i < sbc->pcm_length; i++)
                        {
                            s = *src++ << 8;
                            *dst++ = s;
                            *dst++ = s;
                        }
                    }
                    break;
                default:
                    break;
                }

                sbc->num_channels = 2;
            }
            else
            {
                switch(sbc->output_pcm_width)
                {
                case 16:
                    {
                        int  i;
                        int* src = (int*)&REG_SBC_MEM0;
                        short* dst = (short*)sbc->pcm_sample;

                        for(i = 0; i < sbc->pcm_length; i++)
                        {
                            *dst++ = *src++;
                        }
                    }
                    break;
                case 24:
                    {
                        int  i;
                        int* src = (int*)&REG_SBC_MEM0;
                        int* dst = (int*)sbc->pcm_sample;

                        for(i = 0; i < sbc->pcm_length; i++)
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
    }
    #else
    {
        sbc_decoder_subband_synthesis_filter(sbc);

        if(sbc->num_channels == 2)
        {
            switch(sbc->output_pcm_width)
            {
            case 16:
                {
                    int  i;
                    int* src = (int*)sbc->pcm_sample[1];
                    short* dst = (short*)sbc->pcm_sample + 1;

                    for(i = 0; i < sbc->pcm_length; i++)
                    {
                        *dst = *src++;
                        dst += 2;
                    }
                }
                break;
            case 24:
                {
                    int  i;
                    short* src16 = (short*)sbc->pcm_sample + sbc->pcm_length * 2 - 1;
                    int* src32 = (int*)sbc->pcm_sample[1];
                    short* dst16 = (short*)sbc->pcm_sample + 1;
                    int* dst32 = (int*)sbc->pcm_sample + sbc->pcm_length * 2 - 1;

                    for(i = 0; i < sbc->pcm_length; i++)
                    {
                        *dst16 = *src32++;
                        dst16 += 2;
                    }

                    for(i = 0; i < sbc->pcm_length; i++)
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
            if(sbc->output_stereo_flag)
            {
                switch(sbc->output_pcm_width)
                {
                case 16:
                    {
                        int  i;
                        int* src = (int*)sbc->pcm_sample[0];
                        short* dst = (short*)sbc->pcm_sample + 1;

                        for(i = 0; i < sbc->pcm_length; i++)
                        {
                            *dst = *src++;
                            dst += 2;
                        }
                    }
                    break;
                case 24:
                    {
                        int  i, s;
                        int* src = (int*)&sbc->pcm_sample[0][sbc->pcm_length - 1];
                        int* dst = (int*)&sbc->pcm_sample[0][sbc->pcm_length * 2 - 1];

                        for(i = 0; i < sbc->pcm_length; i++)
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

                sbc->num_channels = 2;
            }
            else
            {
                switch(sbc->output_pcm_width)
                {
                case 16:
                    {
                        int  i;
                        int* src = (int*)sbc->pcm_sample[0];
                        short* dst = (short*)sbc->pcm_sample + 0;

                        for(i = 0; i < sbc->pcm_length; i++)
                        {
                            *dst++ = *src++;
                        }
                    }
                    break;
                case 24:
                    {
                        int  i;
                        int* src = (int*)sbc->pcm_sample[0];
                        int* dst = (int*)sbc->pcm_sample[0];

                        for(i = 0; i < sbc->pcm_length; i++)
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
    }
    #endif

    return consumed;
}

}