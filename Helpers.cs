using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace ColorMatch3D
{
    static class Helpers
    {

        static public BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        // from: https://martin.ankerl.com/2007/10/04/optimized-pow-approximation-for-java-and-c-c/
        // sadly garbage (doesn't work)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double BlitzPow(double a, double b)
        {
            int tmp = (int)(BitConverter.DoubleToInt64Bits(a) >> 32);
            int tmp2 = (int)(b * (tmp - 1072632447) + 1072632447);
            return BitConverter.Int64BitsToDouble(((long)tmp2) << 32);
        }


        public static ByteImage BitmapToByteArray(Bitmap bmp)
        {

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int stride = Math.Abs(bmpData.Stride);
            int bytes = stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            bmp.UnlockBits(bmpData);

            return new ByteImage(rgbValues, stride,bmp.Width,bmp.Height);
        }

        static public string matrixToString<T>(T[,] matrix)
        {
            return "{{" + matrix[0, 0].ToString() + "," + matrix[0, 1].ToString() + "," + matrix[0, 2].ToString() + "},{" + matrix[1, 0].ToString() + "," + matrix[1, 1].ToString() + "," + matrix[1, 2].ToString() + "},{" + matrix[2, 0].ToString() + "," + matrix[2, 1].ToString() + "," + matrix[2, 2].ToString() + "}}";
        }

        static public Bitmap ResizeBitmapNN(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(sourceBMP, 0, 0, width, height);
            }
            return result;
        }

        // Most of this color code is lifted from ColorMinePortable and adapted to work with Vector3
        static public Vector3 sRGBToCIELab(Vector3 sRGBInput)
        {
            return XYZToCIELab(sRGBToXYZ(sRGBInput));
        }

        //static private Matrix4x4 RGBtoXYZMatrix = new Matrix4x4(0.4124f,0.3576f,0.1805f,0,0.2126f,0.7152f,0.0722f,0,0.0193f,0.1192f,0.9505f,0,0,0,0,0);
        static private Matrix4x4 RGBtoXYZMatrix = new Matrix4x4(0.4124f,0.2126f,0.0193f,0,0.3576f,0.7152f,0.1192f,0,0.1805f,0.0722f,0.9505f,0,0,0,0,0);

        // TODO Optimize all these a bit.
        static public Vector3 sRGBToXYZ(Vector3 sRGBInput)
        {
            Vector3 helper = new Vector3();
            helper.X = PivotRgb(sRGBInput.X / 255.0f);
            helper.Y = PivotRgb(sRGBInput.Y / 255.0f);
            helper.Z = PivotRgb(sRGBInput.Z / 255.0f);

            // Observer. = 2°, Illuminant = D65
            /*
            sRGBInput.X = r * 0.4124f + g * 0.3576f + b * 0.1805f;
            sRGBInput.Y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
            sRGBInput.Z = r * 0.0193f + g * 0.1192f + b * 0.9505f;
            */
            sRGBInput = Vector3.Transform(helper,RGBtoXYZMatrix);

            return sRGBInput;
        }

        private static float PivotRgb(float n)
        {
            return (n > 0.04045f ? (float) Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92f) * 100.0f;
        }

        static public Vector3 XYZToCIELab(Vector3 XYZInput)
        {


            float x = PivotXyz(XYZInput.X / WhiteReference.X);
            float y = PivotXyz(XYZInput.Y / WhiteReference.Y);
            float z = PivotXyz(XYZInput.Z / WhiteReference.Z);

            XYZInput.X =  116f * y - 16f;
            XYZInput.Y = 500f * (x - y);
            XYZInput.Z = 200f * (y - z);

            return XYZInput;
        }

        static public Vector3 CIELabTosRGB(Vector3 CIELabInput) {
            return XYZtoRGB(CIELabToXYZ(CIELabInput));
        }

        private static float PivotXyz(float n)
        {
            return n > Epsilon ? CubicRoot(n) : (Kappa * n + 16) / 116;
        }


        private static float CubicRoot(float n)
        {
            return (float)Math.Pow(n, 1.0 / 3.0);
        }


        struct AverageData
        {
            //public double totalR,totalG,totalB;
            public Vector3 color;
            public float divisor;
        };

        static public ByteImage BlurImage(ByteImage inputImage, int radius, bool desaturate = true, float strength = 1)
        {
            byte[] inputImageData = inputImage.imageData;
            int width = inputImage.width;
            int height = inputImage.height;
            int stride = inputImage.stride;

            byte[] output = new byte[inputImageData.Length];
            int strideHere = 0;
            int strideThere = 0;
            int xX4, x2X4;
            int topMin, bottomMax, leftMin, rightMax;
            int lastLeftMin=0, lastRightMax=0;
            int offsetHere;
            float strengthNegative = 1 - strength;

            // Build up a matrix that serves as a lookup table for the euklidian distance of any particular distance from center pixel that is being blurred. Basically the kernel (?) thingie
            // Doing this bc Sqrt and multiplication are expensive when done millions of times.
            /*int matrixSideLength = radius * 2 + 1;
            float[,] euklidianMatrix = new float[matrixSideLength, matrixSideLength]; // Will use this as a quick lookup
            int distanceX, distanceY;
            for (int matrixY = 0; matrixY < matrixSideLength; matrixY++)
            {
                distanceY = Math.Abs(matrixY - radius);
                for (int matrixX=0; matrixX< matrixSideLength; matrixX++)
                {
                    distanceX = Math.Abs(matrixX - radius);
                    euklidianMatrix[matrixY, matrixX] = (float)Math.Max(0,Math.Sqrt(distanceY*distanceY+distanceX*distanceX));
                }
            }*/

            AverageData currentPixel = new AverageData();
            AverageData lastPixel = new AverageData();
            AverageData leftmostBlock = new AverageData();
            AverageData lastLeftmostBlock = new AverageData();
            AverageData rightmostBlock = new AverageData();
            byte tmpMonochrome;
            //Vector3 meanTotal = new Vector3();
            //float meanCount = 0;
            Vector3 tmpColor,tmpColor2;
            bool firstPassFinished = false;
            for(int y = 0; y < height; y++)
            {
                strideHere = stride * y;
                topMin = Math.Max(0, y - radius);
                bottomMax = Math.Min(height, y + radius);
                firstPassFinished = false;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHere = strideHere + xX4;

                    tmpColor2.X = inputImageData[offsetHere];
                    tmpColor2.Y = inputImageData[offsetHere + 1];
                    tmpColor2.Z = inputImageData[offsetHere + 2];

                    leftMin = Math.Max(0, x - radius);
                    rightMax = Math.Min(width, x + radius);

                    leftmostBlock.color.X = leftmostBlock.color.Y = leftmostBlock.color.Z = leftmostBlock.divisor = 0;
                    rightmostBlock.color.X = rightmostBlock.color.Y = rightmostBlock.color.Z = rightmostBlock.divisor = 0;

                    if (!firstPassFinished)
                    {

                        currentPixel.color = tmpColor2;
                        currentPixel.divisor = 1;

                        // Look around
                        for (int y2 = topMin, matrixY = 0; y2 < bottomMax; y2++, matrixY++)
                        {
                            strideThere = stride * y2;
                            for (int x2 = leftMin, matrixX = 0; x2 < rightMax; x2++, matrixX++)
                            {

                                x2X4 = x2 * 4;

                                tmpColor.X = inputImageData[offsetHere];
                                tmpColor.Y = inputImageData[offsetHere + 1];
                                tmpColor.Z = inputImageData[offsetHere + 2];
                                currentPixel.color += tmpColor;
                                currentPixel.divisor += 1;

                                if (x2 == leftMin)
                                {
                                    leftmostBlock.color += tmpColor;
                                    leftmostBlock.divisor += 1;
                                }
                                if (1 + x2 == rightMax)
                                {
                                    rightmostBlock.color += tmpColor;
                                    rightmostBlock.divisor += 1;
                                }
                            }
                        }
                    } else if (firstPassFinished)
                    {


                        currentPixel = lastPixel;

                        for (int y2 = topMin, matrixY = 0; y2 < bottomMax; y2++, matrixY++)
                        {
                            strideThere = stride * y2;
                            for (int x2 = leftMin, matrixX = 0; x2 < rightMax; x2++, matrixX+=(rightMax-leftMin-1))
                            {

                                x2X4 = x2 * 4;

                                tmpColor.X = inputImageData[offsetHere];
                                tmpColor.Y = inputImageData[offsetHere + 1];
                                tmpColor.Z = inputImageData[offsetHere + 2];
                                //currentPixel.color += tmpColor * tmpFactor;
                                //currentPixel.divisor += tmpFactor;

                                if (x2 == leftMin)
                                {
                                    leftmostBlock.color += tmpColor;
                                    leftmostBlock.divisor += 1;
                                }
                                if (1 + x2 == rightMax)
                                {
                                    rightmostBlock.color += tmpColor;
                                    rightmostBlock.divisor += 1;
                                }
                            }
                        }

                        if(lastLeftMin != leftMin)
                        {
                            currentPixel.color -= lastLeftmostBlock.color;
                            currentPixel.divisor -= lastLeftmostBlock.divisor;
                        }
                        if (lastRightMax != rightMax)
                        {
                            currentPixel.color += rightmostBlock.color;
                            currentPixel.divisor += rightmostBlock.divisor;
                        }
                    }

                    


                    //tmpColor = strength*(currentPixel.color / currentPixel.divisor) + strengthNegative * tmpColor2;
                    tmpColor = currentPixel.color / currentPixel.divisor;
                    lastPixel = currentPixel;

                    if (desaturate)
                    {
                        tmpMonochrome = (byte)((tmpColor.X + tmpColor.Y + tmpColor.Z) / 3);
                        output[offsetHere] = tmpMonochrome;
                        output[offsetHere + 1] = tmpMonochrome;
                        output[offsetHere + 2] = tmpMonochrome;
                    } else if (!desaturate)
                    {

                        output[offsetHere] = (byte)tmpColor.X;
                        output[offsetHere + 1] = (byte)tmpColor.Y;
                        output[offsetHere + 2] = (byte)tmpColor.Z;
                    }

                    firstPassFinished = true;

                    lastLeftMin = leftMin;
                    lastRightMax = rightMax;
                    lastLeftmostBlock.color = leftmostBlock.color;
                    lastLeftmostBlock.divisor = leftmostBlock.divisor;
                }
            }

            return new ByteImage(output,stride,width,height);
        }
        /*
        static public ByteImage BlurImage(ByteImage inputImage, int radius, float strength = 1)
        {
            byte[] inputImageData = inputImage.imageData;
            int width = inputImage.width;
            int height = inputImage.height;
            int stride = inputImage.stride;

            byte[] output = new byte[inputImageData.Length];
            int strideHere = 0;
            int strideThere = 0;
            int xX4, x2X4;
            int topMin, bottomMax, leftMin, rightMax;
            int offsetHere;
            float strengthNegative = 1 - strength;

            // Build up a matrix that serves as a lookup table for the euklidian distance of any particular distance from center pixel that is being blurred. Basically the kernel (?) thingie
            // Doing this bc Sqrt and multiplication are expensive when done millions of times.
            int matrixSideLength = radius * 2 + 1;
            float[,] euklidianMatrix = new float[matrixSideLength, matrixSideLength]; // Will use this as a quick lookup
            int distanceX, distanceY;
            for (int matrixY = 0; matrixY < matrixSideLength; matrixY++)
            {
                distanceY = Math.Abs(matrixY - radius);
                for (int matrixX = 0; matrixX < matrixSideLength; matrixX++)
                {
                    distanceX = Math.Abs(matrixX - radius);
                    euklidianMatrix[matrixY, matrixX] = (float)Math.Max(0, Math.Sqrt(distanceY * distanceY + distanceX * distanceX));
                }
            }

            AverageData currentPixel = new AverageData();
            AverageData lastPixel = new AverageData();
            AverageData leftmostBlock = new AverageData();
            AverageData rightmostBlock = new AverageData();
            //Vector3 meanTotal = new Vector3();
            //float meanCount = 0;
            Vector3 tmpColor, tmpColor2;
            float tmpFactor;
            bool firstPassFinished = false;
            for (int y = 0; y < height; y++)
            {
                strideHere = stride * y;
                topMin = Math.Max(0, y - radius);
                bottomMax = Math.Min(height, y + radius);
                firstPassFinished = false;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHere = strideHere + xX4;

                    tmpColor2.X = inputImageData[offsetHere];
                    tmpColor2.Y = inputImageData[offsetHere + 1];
                    tmpColor2.Z = inputImageData[offsetHere + 2];
                    currentPixel.color = tmpColor2;
                    currentPixel.divisor = 1;

                    leftMin = Math.Max(0, x - radius);
                    rightMax = Math.Min(width, x + radius);

                    leftmostBlock.color.X = leftmostBlock.color.Y = leftmostBlock.color.Z = leftmostBlock.divisor = 0;
                    rightmostBlock.color.X = rightmostBlock.color.Y = rightmostBlock.color.Z = rightmostBlock.divisor = 0;



                    // Look around
                    for (int y2 = topMin, matrixY = 0; y2 < bottomMax; y2++, matrixY++)
                    {
                        strideThere = stride * y2;
                        for (int x2 = leftMin, matrixX = 0; x2 < rightMax; x2++, matrixX++)
                        {
                            tmpFactor = euklidianMatrix[matrixY, matrixX];

                            x2X4 = x2 * 4;

                            tmpColor.X = inputImageData[offsetHere];
                            tmpColor.Y = inputImageData[offsetHere + 1];
                            tmpColor.Z = inputImageData[offsetHere + 2];
                            currentPixel.color += tmpColor * tmpFactor;
                            currentPixel.divisor += tmpFactor;

                            if (x2 == leftMin)
                            {
                                leftmostBlock.color += tmpColor * tmpFactor;
                                leftmostBlock.divisor += tmpFactor;
                            }
                            if (1 + x2 == rightMax)
                            {
                                rightmostBlock.color += tmpColor * tmpFactor;
                                rightmostBlock.divisor += tmpFactor;
                            }
                        }
                    }


                    //tmpColor = strength*(currentPixel.color / currentPixel.divisor) + strengthNegative * tmpColor2;
                    tmpColor = currentPixel.color / currentPixel.divisor;
                    lastPixel = currentPixel;

                    output[offsetHere] = (byte)tmpColor.X;
                    output[offsetHere + 1] = (byte)tmpColor.Y;
                    output[offsetHere + 2] = (byte)tmpColor.Z;

                    firstPassFinished = true;
                }
            }

            return new ByteImage(output, stride, width, height);
        }*/


        static public Vector3 WhiteReference = new Vector3
        {
                X = 95.047f,
                Y = 100.000f,
                Z = 108.883f
            };
        internal const float Epsilon = 0.008856f; // Intent is 216/24389
        internal const float Kappa = 903.3f; // Intent is 24389/27

        static public Vector3 CIELabToXYZ(Vector3 CIELabInput)
        {
            float y = (CIELabInput.X + 16.0f) / 116.0f;
            float x = CIELabInput.Y / 500.0f + y;
            float z = y - CIELabInput.Z / 200.0f;

            var white = WhiteReference;
            var x3 = x * x * x;
            var z3 = z * z * z;
            Vector3 output = new Vector3();
            output.X = white.X * (x3 > Epsilon ? x3 : (x - 16.0f / 116.0f) / 7.787f);
            output.Y = white.Y * (CIELabInput.X > (Kappa * Epsilon) ? (float) Math.Pow(((CIELabInput.X + 16.0) / 116.0), 3) : CIELabInput.X / Kappa);
            output.Z = white.Z * (z3 > Epsilon ? z3 : (z - 16.0f / 116.0f) / 7.787f);
            

            return output;
        }

        static private Matrix4x4 XYZtoRGBMatrix = new Matrix4x4(3.2406f, -0.9689f, 0.0557f,0, -1.5372f, 1.8758f, -0.2040f,0, -0.4986f, 0.0415f, 1.0570f,0,0,0,0,0);

        static public Vector3 XYZtoRGB(Vector3 XYZInput)
        {
            // (Observer = 2°, Illuminant = D65)
            /*float x = XYZInput.X / 100.0f;
            float y = XYZInput.Y / 100.0f;
            float z = XYZInput.Z / 100.0f;

            float r = x * 3.2406f + y * -1.5372f + z * -0.4986f;
            float g = x * -0.9689f + y * 1.8758f + z * 0.0415f;
            float b = x * 0.0557f + y * -0.2040f + z * 1.0570f;
            */

            XYZInput = Vector3.Transform(XYZInput / 100.0f, XYZtoRGBMatrix);

            XYZInput.X = XYZInput.X > 0.0031308f ? 1.055f * (float)Math.Pow(XYZInput.X, 1 / 2.4) - 0.055f : 12.92f * XYZInput.X;
            XYZInput.Y = XYZInput.Y > 0.0031308f ? 1.055f * (float)Math.Pow(XYZInput.Y, 1 / 2.4) - 0.055f : 12.92f * XYZInput.Y;
            XYZInput.Z = XYZInput.Z > 0.0031308f ? 1.055f * (float)Math.Pow(XYZInput.Z, 1 / 2.4) - 0.055f : 12.92f * XYZInput.Z;

            return XYZInput * 255.0f;
        }

    }
}
