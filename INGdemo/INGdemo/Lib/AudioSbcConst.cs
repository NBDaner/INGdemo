using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace INGdemo.Lib
{  
    static class Constants
    {
        public const byte SBC_SYNCWORD                  = 0x9C;        /**< SBC synchronize word             */
        public const byte MSBC_SYNCWORD                 = 0xAD;        /**< MSBC synchronize word            */
        public const byte SBC_SAMPLE_RATE_16000         = 0x0;         /**< SBC sampling frequency : 16.0KHz */
        public const byte SBC_SAMPLE_RATE_32000         = 0x1;         /**< SBC sampling frequency : 32.0KHz */
        public const byte SBC_SAMPLE_RATE_44100         = 0x2;         /**< SBC sampling frequency : 44.1KHz */
        public const byte SBC_SAMPLE_RATE_48000         = 0x3;         /**< SBC sampling frequency : 48.0KHz */
        public const byte SBC_BLOCKS_4                  = 0x0;         /**< SBC blocks number 4              */
        public const byte SBC_BLOCKS_8                  = 0x1;         /**< SBC blocks number 8              */
        public const byte SBC_BLOCKS_12                 = 0x2;         /**< SBC blocks number 12             */
        public const byte SBC_BLOCKS_16                 = 0x3;         /**< SBC blocks number 16             */
        public const byte SBC_CHANNEL_MODE_MONO         = 0x0;         /**< SBC channel mode : MONO          */
        public const byte SBC_CHANNEL_MODE_DUAL_CHANNEL = 0x1;         /**< SBC channel mode : Dual Channels */
        public const byte SBC_CHANNEL_MODE_STEREO       = 0x2;         /**< SBC channel mode : Stereo        */
        public const byte SBC_CHANNEL_MODE_JOINT_STEREO = 0x3;         /**< SBC channel mode : Joint Stereo  */
        public const byte SBC_ALLOCATION_METHOD_LOUDNESS= 0x0;         /**< SBC allocation method : Loudness */
        public const byte SBC_ALLOCATION_METHOD_SNR     = 0x1;         /**< SBC allocation method : SNR      */
        public const byte SBC_SUBBANDS_4                = 0x0;         /**< SBC subbands number 4            */
        public const byte SBC_SUBBANDS_8                = 0x1;         /**< SBC subbands number 8            */
        public const int SBC_HEADER_MAX_SZIE            = 13;          /**< SBC header max size 13 in bytes = (8+2+2+2+1+1+8+8+8*1+2*8*4)/8 */
        public const int SBC_MAX_FRAME_SIZE             = 513;         /**< SBC max frame size 513 in bytes = 4+(4*8*2)/8+(8+16*250)/8      */

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
}