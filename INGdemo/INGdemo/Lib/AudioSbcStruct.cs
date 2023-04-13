using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace INGdemo.Lib
{
    public struct sbc_frame_header
    {
        #if __BIG_ENDIAN__
            //big endianness
            uint crc_check;         //:8
            uint bitpool;           //:8
            uint subband_mode;      //:1
            uint allocation_method; //:1
            uint channel_mode;      //:2
            uint block_mode;        //:2
            uint sample_rate_index; //:2
            uint syncword;          //:8
        #else
        //little endianness
            uint syncword;          //:8
            uint subband_mode;      //:1
            uint allocation_method; //:1
            uint channel_mode;      //:2
            uint block_mode;        //:2
            uint sample_rate_index; //:2
            uint bitpool;           //:8
            uint crc_check;         //:8
        #endif
    }
    
    public enum sbc_dec_err_code
    {
        SBC_DECODER_ERRORS = -128,
        SBC_DECODER_ERROR_BUFFER_OVERFLOW,          /**< buffer overflow       */
        SBC_DECODER_ERROR_SYNC_INCORRECT,           /**< synchronize incorrect */
        SBC_DECODER_ERROR_BITPOOL_OUT_BOUNDS,       /**< bitpool out of bounds */
        SBC_DECODER_ERROR_CRC8_INCORRECT,           /**< CRC8 check incorrect  */
        SBC_DECODER_ERROR_STREAM_EMPTY,             /**< stream empty          */
        SBC_DECODER_ERROR_INVALID_CTRL_CMD,         /**< invalid ctrl cmd      */
        SBC_DECODER_ERROR_INVALID_CTRL_ARG,         /**< invalid ctrl arg      */
        SBC_DECODER_ERROR_OK = 0,                   /**< no error              */
    }

    public struct sbc_dec_info
    {
        public sbc_frame_info frame;
        public sbyte   num_channels;              /**< channels number    */
        public byte  pcm_length;                /**< PCM length         */
        public ushort sample_rate;               /**< sample rate        */
        public byte  output_stereo_flag;
        public byte  output_pcm_width;
        public ushort reserved;
        public int[,]  pcm_sample;        /**< PCM frame buffer   2  128*/
        public int[,]  vfifo;             /**< FIFO V for subbands synthesis calculation.  2  170*/ 
        public int[,]  offset;             //2  16
    }

    public struct sbc_frame_info
    {
        public sbyte   blocks;                    /**< block number       */
        public sbyte   subbands;                  /**< subbands number    */
        public byte  join;                      /**< bit number x set means joint stereo has been used in sub-band x */
        public byte  bitpool;                   /**< indicate the size of the bit allocation pool that has been used for encoding the stream */
        public sbyte   channel_mode;              /**< channel mode       */
        public sbyte   sample_rate_index;         /**< sample rate index, 0:16000, 1:32000, 2:44100, 3:48000 */
        public sbyte   allocation_method;         /**< allocation method  */
        public sbyte   reserved8;                 /**< dummy, reserved for byte align */
        public sbyte[,] bits;                /**< calculate result by bit allocation. 2 8*/   
        public sbyte[,] scale_factor;        /**< only the lower 4 bits of every element are to be used 2 8*/
        public int[,] mem;                 /**< Memory used as bit need and levels 2 8*/
    }       
}