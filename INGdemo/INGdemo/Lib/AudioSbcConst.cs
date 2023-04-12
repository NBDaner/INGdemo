using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace INGdemo.Lib
{  
    static class Constants
    {
        public const byte SBC_SYNCWORD          = 0x9C;        /**< SBC synchronize word             */
        public const byte MSBC_SYNCWORD         = 0xAD;        /**< MSBC synchronize word            */
        public const byte SBC_SAMPLE_RATE_16000 = 0x0;         /**< SBC sampling frequency : 16.0KHz */
        public const byte SBC_SAMPLE_RATE_32000 = 0x1;         /**< SBC sampling frequency : 32.0KHz */
        public const byte SBC_SAMPLE_RATE_44100 = 0x2;         /**< SBC sampling frequency : 44.1KHz */
        public const byte SBC_SAMPLE_RATE_48000 = 0x3;         /**< SBC sampling frequency : 48.0KHz */
        public const byte SBC_BLOCKS_4          = 0x0;         /**< SBC blocks number 4              */
        public const byte SBC_BLOCKS_8          = 0x1;         /**< SBC blocks number 8              */
        public const byte SBC_BLOCKS_12         = 0x2;         /**< SBC blocks number 12             */
        public const byte SBC_BLOCKS_16         = 0x3;         /**< SBC blocks number 16             */
        public const byte SBC_CHANNEL_MODE_MONO = 0x0;         /**< SBC channel mode : MONO          */
        public const byte SBC_CHANNEL_MODE_DUAL_CHANNEL   =0x1;         /**< SBC channel mode : Dual Channels */
        public const byte SBC_CHANNEL_MODE_STEREO         =0x2;         /**< SBC channel mode : Stereo        */
        public const byte SBC_CHANNEL_MODE_JOINT_STEREO   =0x3;         /**< SBC channel mode : Joint Stereo  */
        public const byte SBC_ALLOCATION_METHOD_LOUDNESS  =0x0;         /**< SBC allocation method : Loudness */
        public const byte SBC_ALLOCATION_METHOD_SNR       =0x1;         /**< SBC allocation method : SNR      */
        public const byte SBC_SUBBANDS_4        = 0x0;        /**< SBC subbands number 4            */
        public const byte SBC_SUBBANDS_8        = 0x1;         /**< SBC subbands number 8            */
        public const int SBC_HEADER_MAX_SZIE              =13;         /**< SBC header max size 13 in bytes = (8+2+2+2+1+1+8+8+8*1+2*8*4)/8 */
        public const int SBC_MAX_FRAME_SIZE              =513;         /**< SBC max frame size 513 in bytes = 4+(4*8*2)/8+(8+16*250)/8      */

        //各项系数
        public const int SCALE_PROTO4_TBL    = 15;
        public const int SCALE_ANA4_TBL      = 17;
        public const int SCALE_PROTO8_TBL    = 16;
        public const int SCALE_ANA8_TBL      = 17;
        public const int SCALE_SPROTO4_TBL   = 12;
        public const int SCALE_SPROTO8_TBL   = 14;
        public const int SCALE_NPROTO4_TBL   = 11;
        public const int SCALE_NPROTO8_TBL   = 11;
        public const int SCALE4_STAGE1_BITS  = 15;
        public const int SCALE4_STAGE2_BITS  = 15;
        public const int SCALE8_STAGE1_BITS  = 15;
        public const int SCALE8_STAGE2_BITS  = 15;
        public const int SCALE4_STAGED1_BITS = 15;
        public const int SCALE4_STAGED2_BITS = 16;
        public const int SCALE8_STAGED1_BITS = 15;
        public const int SCALE8_STAGED2_BITS = 16;
    }

    public class exp
    {
        static public int CI(uint i)
        {
            return Convert.ToInt32(i);
        }

    }



#define ASR(val, bits)      = (int32_t)(val)) >> (bits)
#define SP4(val)                        ASR(val, SCALE_PROTO4_TBL)
#define SA4(val)                        ASR(val, SCALE_ANA4_TBL)
#define SP8(val)                        ASR(val, SCALE_PROTO8_TBL)
#define SA8(val)                        ASR(val, SCALE_ANA8_TBL)
#define SS4(val)                        ASR(val, SCALE_SPROTO4_TBL)
#define SS8(val)                        ASR(val, SCALE_SPROTO8_TBL)
#define SN4(val)                        ASR(val, SCALE_NPROTO4_TBL)
#define SN8(val)                        ASR(val, SCALE_NPROTO8_TBL)
#define SCALE4_STAGE1(src)              ASR(src, SCALE4_STAGE1_BITS)
#define SCALE4_STAGE2(src)              ASR(src, SCALE4_STAGE2_BITS)
#define SCALE8_STAGE1(src)              ASR(src, SCALE8_STAGE1_BITS)
#define SCALE8_STAGE2(src)              ASR(src, SCALE8_STAGE2_BITS)
#define SCALE4_STAGED1(src)             ASR(src, SCALE4_STAGED1_BITS)
#define SCALE4_STAGED2(src)             ASR(src, SCALE4_STAGED2_BITS)
#define SCALE8_STAGED1(src)             ASR(src, SCALE8_STAGED1_BITS)
#define SCALE8_STAGED2(src)             ASR(src, SCALE8_STAGED2_BITS)

/**
 * @brief SBC decoder context
 */
typedef struct _SbcCommonContext
{
    int8_t   blocks;                    /**< block number       */
    int8_t   subbands;                  /**< subbands number    */
    uint8_t  join;                      /**< bit number x set means joint stereo has been used in sub-band x */
    uint8_t  bitpool;                   /**< indicate the size of the bit allocation pool that has been used for encoding the stream */

    int8_t   channel_mode;              /**< channel mode       */
    int8_t   sample_rate_index;         /**< sample rate index, 0:16000, 1:32000, 2:44100, 3:48000 */
    int8_t   allocation_method;         /**< allocation method  */
    int8_t   reserved8;                 /**< dummy, reserved for byte align */

    int8_t   bits[2][8];                /**< calculate result by bit allocation. */

    int8_t   scale_factor[2][8];        /**< only the lower 4 bits of every element are to be used */

    int32_t  mem[2][8];                 /**< Memory used as bit need and levels */

}SbcCommonContext;

/**
 * @brief SBC frame header context
 */
typedef struct _SbcFrameHeader
{
    #if defined(__BIG_ENDIAN__)
    //big endianness
    uint32_t crc_check          :8;
    uint32_t bitpool            :8;
    uint32_t subband_mode       :1;
    uint32_t allocation_method  :1;
    uint32_t channel_mode       :2;
    uint32_t block_mode         :2;
    uint32_t sample_rate_index  :2;
    uint32_t syncword           :8;
    #else
    //little endianness
    uint32_t syncword           :8;
    uint32_t subband_mode       :1;
    uint32_t allocation_method  :1;
    uint32_t channel_mode       :2;
    uint32_t block_mode         :2;
    uint32_t sample_rate_index  :2;
    uint32_t bitpool            :8;
    uint32_t crc_check          :8;
    #endif
}SbcFrameHeader;

/**
 * @brief  Get sample rate by index
 * @param  idx sample rate index, 0:16000, 1:32000, 2:44100, 3:48000
 * @return sample rate
 */
uint16_t sbc_common_sample_rate_get(uint32_t idx);

/**
 * @brief  CRC8 calculation
 * @param  data  data buffer to do calculation
 * @param  len   data buffer length in bits
 * @return CRC8 value
 */
uint8_t  sbc_common_crc8(const uint8_t* data, uint32_t len);

/**
 * @brief  SBC bit allocation calculate for both encoder and decoder
 * @param  sbc SBC common context pointer
 * @return NULL
 */
void sbc_common_bit_allocation(SbcCommonContext* sbc); 
}