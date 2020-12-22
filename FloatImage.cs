using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorMatch3D
{
    class FloatImage
    {
        public float[] imageData = new float[0];
        public int stride=0;
        public int width=0, height=0;
        public PixelFormat pixelFormat;

        public FloatImage(float[] imageDataA, int strideA, int widthA, int heightA, PixelFormat pixelFormatA)
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

        public float this[int index]
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

        public ByteImage ToByteImage()
        {
            float[] inputImageData = imageData;

            byte[] output = new byte[inputImageData.Length];
            int strideHere = 0;
            int xX4;
            int offsetHere;

            for (int y = 0; y < height; y++)
            {
                strideHere = stride * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHere = strideHere + xX4;

                    output[offsetHere] = (byte)Math.Max(0,Math.Min(255,inputImageData[offsetHere]));
                    output[offsetHere + 1] = (byte)Math.Max(0, Math.Min(255, inputImageData[offsetHere +1 ]));
                    output[offsetHere + 2] = (byte)Math.Max(0, Math.Min(255, inputImageData[offsetHere + 2]));
                    output[offsetHere + 3] = (byte)Math.Max(0, Math.Min(255, inputImageData[offsetHere + 3]));
                }
            }

            return new ByteImage(output, stride, width, height, pixelFormat);
        }

        static public FloatImage FromByteImage(ByteImage inputImage)
        {
            byte[] inputImageData = inputImage.imageData;
            int width = inputImage.width, height = inputImage.height, stride = inputImage.stride;
            PixelFormat pixelFormat = inputImage.pixelFormat;

            float[] output = new float[inputImageData.Length];
            int strideHere = 0;
            int xX4;
            int offsetHere;

            for (int y = 0; y < height; y++)
            {
                strideHere = stride * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHere = strideHere + xX4;

                    output[offsetHere] = (float)inputImageData[offsetHere];
                    output[offsetHere + 1] = (float)inputImageData[offsetHere +1];
                    output[offsetHere + 2] = (float)inputImageData[offsetHere +2 ];
                    output[offsetHere + 3] = (float)inputImageData[offsetHere +3];
                }
            }

            return new FloatImage(output, stride, width, height, pixelFormat);
        }
    }
}
