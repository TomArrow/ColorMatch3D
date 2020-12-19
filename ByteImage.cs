using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorMatch3D
{
    class ByteImage
    {
        public byte[] imageData;
        public int stride;
        public int width, height;
        public PixelFormat pixelFormat;

        public ByteImage(byte[] imageDataA, int strideA,int widthA, int heightA, PixelFormat pixelFormatA)
        {
            imageData = imageDataA;
            stride = strideA;
            width = widthA;
            height = heightA;
            pixelFormat = pixelFormatA;
        }

        public int Length
        {
            get { return imageData.Length; }
        }

        public byte this[int index]
        {
            get
            {
                return imageData[index];
            }

            set
            {
                imageData[index] = value;
            }
        }
    }
}
