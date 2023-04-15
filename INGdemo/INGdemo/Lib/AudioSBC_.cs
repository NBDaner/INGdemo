using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace INGdemo.Lib
{
    public class SBCDecoder1
    {
        //Queue<byte> inputStream = new Queue<byte>();  //初始化输入队列
        private int inputSize = 24;
        private int outputSize = 128;
        private int Readindex;
        private int WriteIndex;
        private byte[] inputStream;
        private byte[] outputStream;
        private int inp = 0;  //解码器输入数组位置指示器,初始化为0
        private int outp = 0;
        private int decoded;
        private bool dec_lock = true;
        public static sbc_dec_info sbc;

        public SBCDecoder1()
        {
            Reset();
        }

        public event EventHandler<byte []> SBCOutput;

        public void Reset()
        {
            inputStream = new byte[inputSize];
            outputStream = new byte[outputSize];
            Readindex = 0;
            WriteIndex = 0;

            //Structure object initialization
            sbc.pcm_sample = new int[2,16*8];
            sbc.vfifo = new int[2,170];
            sbc.offset = new int[2,16];

            sbc.frame.bits = new sbyte[2,8];
            sbc.frame.scale_factor = new sbyte[2,8];
            sbc.frame.mem = new int[2,8];
        }

        public void Decode(byte data)
        {
            inputStream[Readindex++] = data;
            System.Diagnostics.Debug.WriteLine("data[0] = {0}",data);
            if  (Readindex >= inputSize)
            {
                Readindex = 0;
                //调用SBC解码
                // WriteIndex +=sbc_decode(inputStream, inputSize, 
                //                             outputStream, outputSize, decoded);
                if(WriteIndex >= outputSize)
                {
                    SBCOutput.Invoke(this,outputStream);
                    WriteIndex = 0;
                }
            }         
        }

        //与ADPCM解码不同
        //数据需要达到一定长度之后才能进行解码
        public void Decode(byte[] data)
        {
            foreach(var x in data) Decode(x);
        }
     
    }
}