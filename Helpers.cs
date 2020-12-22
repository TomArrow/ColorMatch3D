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

            return new ByteImage(rgbValues, stride,bmp.Width,bmp.Height,bmp.PixelFormat);
        }

        public static Bitmap ByteArrayToBitmap(ByteImage byteImage)
        {
            Bitmap myBitmap = new Bitmap(byteImage.width, byteImage.height,byteImage.pixelFormat);
            Rectangle rect = new Rectangle(0, 0, myBitmap.Width, myBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                myBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                myBitmap.PixelFormat);

            bmpData.Stride = byteImage.stride;

            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(byteImage.imageData,0,ptr, byteImage.imageData.Length);

            myBitmap.UnlockBits(bmpData);
            return myBitmap;

        }



        struct AverageData1D
        {
            //public double totalR,totalG,totalB;
            public float color;
            public float divisor;
        };


        // Simple 1D Greyscale regrade of one image to another. Basically just to match brightness, that's all.
        static public FloatImage Regrade1DHistogram(ByteImage testImage, ByteImage referenceImage, int percentileSubdivisions = 100)
        {

            byte[] testImageData = testImage.imageData;
            byte[] referenceImageData = referenceImage.imageData;
            float[] output = new float[testImage.Length];

            // Work in 16 bit for more precision in the percentile thing.
            int sixteenBitCount = 256 * 256;
            int sixteenBitMax = sixteenBitCount - 1;

            // Build histogram
            int[] histogramTest = new  int[sixteenBitCount];
            int[] histogramRef = new int[sixteenBitCount];

            int width = testImage.width;
            int height = testImage.height;
            int strideTest = testImage.stride;
            int strideRef = referenceImage.stride;

            int strideHereTest, strideHereRef = 0;
            int xX4;
            int offsetHereTest, offsetHereRef;

            int testLuma, refLuma;
            float hereFactor;

            // 1. BUILD HISTOGRAMS
            for (int y = 0; y < height; y++)
            {
                strideHereTest = strideTest * y;
                strideHereRef = strideRef * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHereTest = strideHereTest + xX4;
                    offsetHereRef = strideHereRef + xX4;

                    testLuma = (int) Math.Max(0,Math.Min(sixteenBitMax, ( ((int)testImageData[offsetHereTest] << 8)* 0.11f + 0.59f * ((int)testImageData[offsetHereTest + 1] << 8) + 0.3f * ((int)testImageData[offsetHereTest + 2] << 8))));
                    refLuma = (int)Math.Max(0, Math.Min(sixteenBitMax, ( ((int)referenceImageData[offsetHereRef] << 8) * 0.11f + 0.59f * ((int)referenceImageData[offsetHereRef + 1] << 8) + 0.3f * ((int)referenceImageData[offsetHereRef + 2] << 8))));

                    histogramTest[testLuma]++;
                    histogramRef[refLuma]++;

                    /*output[offsetHereTest] = tmpLuma;
                    output[offsetHereTest + 1] = tmpLuma;
                    output[offsetHereTest + 2] = tmpLuma;
                    output[offsetHereTest + 3] = testImageData[offsetHere + 3];*/
                }
            }

            // Info: The subdivision count is the amount of "boxes" I divide the brightness spectrum into. But these arrays do not represent the boxes, but rather the splits between the boxes, so it's -1. The splits define exactly WHERE the boxes end.
            FloatIssetable[] percentilesTest = new FloatIssetable[percentileSubdivisions-1];
            FloatIssetable[] percentilesRef = new FloatIssetable[percentileSubdivisions-1];

            float onePercentile = width * height / percentileSubdivisions; // This is a bit messy I guess but it should work out.


            // 2. BUILD PERCENTILE ARRAYS
            // Fill percentile-arrays, basically saying at which brightness a certain percentile starts, by the amount of pixels that have a certain brightness
            int countTest = 0;
            int currentPercentileTest = 0, lastPercentileTest = 0;
            float lastTestPercentileJumpValue = 0;
            int countRef = 0;
            int currentPercentileRef = 0, lastPercentileRef = 0;
            float lastRefPercentileJumpValue = 0;

            for (int i = 0; i < sixteenBitCount; i++)
            {
                countTest += histogramTest[i];
                currentPercentileTest = (int)Math.Min(percentileSubdivisions-1,Math.Floor((double)countTest / onePercentile));

                if(currentPercentileTest != lastPercentileTest)
                {
                    percentilesTest[currentPercentileTest-1].value = i;
                    percentilesTest[currentPercentileTest-1].isSet = true;

                    // Fill holes, if there are any.
                    for(int a = lastPercentileTest + 1; a < currentPercentileTest; a++)
                    {
                        percentilesTest[a - 1].value = lastTestPercentileJumpValue + (i-lastTestPercentileJumpValue) * ((a - lastPercentileTest)/((float)currentPercentileTest-lastPercentileTest));
                        percentilesTest[a - 1].isSet = true;
                    }
                    lastTestPercentileJumpValue = i;
                }

                lastPercentileTest = currentPercentileTest;

                countRef += histogramRef[i];
                currentPercentileRef = (int)Math.Min(percentileSubdivisions - 1, Math.Floor((double)countRef / onePercentile));

                if (currentPercentileRef != lastPercentileRef)
                {
                    percentilesRef[currentPercentileRef-1].value = i;
                    percentilesRef[currentPercentileRef-1].isSet = true;

                    // Fill holes, if there are any.
                    for (int a = lastPercentileRef + 1; a < currentPercentileRef; a++)
                    {
                        //percentilesRef[a - 1].value = lastRefPercentileJumpValue + (i - lastRefPercentileJumpValue) * (currentPercentileRef - lastPercentileRef) / a;
                        percentilesRef[a - 1].value = lastRefPercentileJumpValue + (i - lastRefPercentileJumpValue) * ((a - currentPercentileRef) / ((float)currentPercentileRef - lastPercentileRef));
                        percentilesRef[a - 1].isSet = true;
                    }
                    lastRefPercentileJumpValue = i;
                }

                lastPercentileRef = currentPercentileRef;
            }



            // 3. BUILD LUT FROM PERCENTILE ARRAYS
            FloatIssetable[] factorsLUT = new FloatIssetable[256];
            for (int i=0; i < percentilesTest.Length; i++)
            {
                factorsLUT[(int)Math.Floor(percentilesTest[i].value / 256)].value = percentilesTest[i].value == 0 ? 1 : percentilesRef[i].value / percentilesTest[i].value;
                factorsLUT[(int)Math.Floor(percentilesTest[i].value / 256)].isSet = true;
            }

            // 4. FILL HOLES IN LUT
            bool lastExists = false, nextExists = false;
            int lastExisting = 0, nextExisting = 0;
            float linearInterpolationFactor =0;
            for(int i = 0; i < 256; i++)
            {
                if(factorsLUT[i].isSet == false)
                {
                    // Find next set value, if it exisst
                    nextExists = false;
                    for (int a = i+1; a < 256; a++)
                    {
                        if (factorsLUT[a].isSet)
                        {
                            nextExists = true;
                            nextExisting = a;
                            break;
                        }
                    }

                    if (nextExists && lastExists)
                    {
                        linearInterpolationFactor = ((float)i - lastExisting)/ (nextExisting - lastExisting) ;
                        factorsLUT[i].value = factorsLUT[lastExisting].value + linearInterpolationFactor * (factorsLUT[nextExisting].value - factorsLUT[lastExisting].value);
                        factorsLUT[i].isSet = true;

                        lastExists = true;
                        lastExisting = i;
                    }
                    else if (lastExists)
                    {

                        factorsLUT[i].value = factorsLUT[lastExisting].value;
                        factorsLUT[i].isSet = true;

                        lastExists = true;
                        lastExisting = i;
                    }
                    else if (nextExists)
                    {

                        factorsLUT[i].value = factorsLUT[nextExisting].value;
                        factorsLUT[i].isSet = true;

                        lastExists = true;
                        lastExisting = i;
                    }
                    else if (!nextExists && !lastExists)
                    {
                        // Kinda impossible but ok
                        // I think we're kinda fucced then. Let's just assume this never happens
                    }
                } else
                {
                    // Do nothing, all good
                    lastExists = true;
                    lastExisting = i;
                }
            }

            // 5. APPLY LUT
            for (int y = 0; y < height; y++)
            {
                strideHereTest = strideTest * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHereTest = strideHereTest + xX4;

                    testLuma = (int)Math.Max(0, Math.Min(sixteenBitMax, (((float)testImageData[offsetHereTest]) * 0.11f + 0.59f * ((float)testImageData[offsetHereTest + 1]) + 0.3f * ((float)testImageData[offsetHereTest + 2]))));

                    hereFactor = factorsLUT[(int)testLuma].value;

                    output[offsetHereTest] = (float)testImageData[offsetHereTest] * hereFactor;
                    output[offsetHereTest + 1] = (float)testImageData[offsetHereTest + 1] * hereFactor;
                    output[offsetHereTest + 2] = (float)testImageData[offsetHereTest + 2] * hereFactor;
                    output[offsetHereTest + 3] = (float)testImageData[offsetHereTest + 3];
                }
            }


            return new FloatImage(output, strideTest, width, height, testImage.pixelFormat);
        }

        struct FloatIssetable
        {
            public float value;
            public bool isSet;
        }


        // Simple 1D Greyscale regrade of one image to another. Basically just to match brightness, that's all.
        static public FloatImage Regrade1DSimpleLUT(ByteImage testImage, ByteImage referenceImage)
        {

            byte[] testImageData = testImage.imageData;
            byte[] referenceImageData = referenceImage.imageData;
            float[] output = new float[testImage.Length];
            // Build histogram
            AverageData1D[] FactorLUT = new AverageData1D[256];

            int width = testImage.width;
            int height = testImage.height;
            int strideTest = testImage.stride;
            int strideRef = referenceImage.stride;

            int strideHereTest,strideHereRef = 0;
            int xX4;
            int offsetHereTest,offsetHereRef;

            float testLuma,refLuma;
            float hereFactor;

            for (int y = 0; y < height; y++)
            {
                strideHereTest = strideTest * y;
                strideHereRef = strideRef * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHereTest = strideHereTest + xX4;
                    offsetHereRef = strideHereRef + xX4;

                    testLuma = testImageData[offsetHereTest] * 0.11f + 0.59f * testImageData[offsetHereTest + 1] + 0.3f * testImageData[offsetHereTest + 2];
                    refLuma = referenceImageData[offsetHereRef] * 0.11f + 0.59f * referenceImageData[offsetHereRef + 1] + 0.3f * referenceImageData[offsetHereRef + 2];

                    FactorLUT[(int)testLuma].color += refLuma / testLuma;
                    FactorLUT[(int)testLuma].divisor++;


                    /*output[offsetHereTest] = tmpLuma;
                    output[offsetHereTest + 1] = tmpLuma;
                    output[offsetHereTest + 2] = tmpLuma;
                    output[offsetHereTest + 3] = testImageData[offsetHere + 3];*/
                }
            }

            // Evaluate histogram
            // No interpolation bc it only has to apply to this one image.
            for(int i = 0; i < 256; i++)
            {
                if(FactorLUT[i].divisor != 0)
                {

                    FactorLUT[i].color = FactorLUT[i].color / FactorLUT[i].divisor;
                }
            }

            // Apply histogram
            for (int y = 0; y < height; y++)
            {
                strideHereTest = strideTest * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHereTest = strideHereTest + xX4;

                    testLuma = testImageData[offsetHereTest] * 0.11f + 0.59f * testImageData[offsetHereTest + 1] + 0.3f * testImageData[offsetHereTest + 2];


                    hereFactor = FactorLUT[(int)testLuma].color;

                    output[offsetHereTest] = testImageData[offsetHereTest] * hereFactor;
                    output[offsetHereTest + 1] = testImageData[offsetHereTest + 1] * hereFactor;
                    output[offsetHereTest + 2] = testImageData[offsetHereTest + 2] * hereFactor;
                    output[offsetHereTest + 3] = testImageData[offsetHereTest + 3];
                }
            }

            return new FloatImage(output, strideTest, width, height, testImage.pixelFormat);
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


        static public Bitmap ResizeBitmapHQ(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
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

        // Very simple and inadequate greyscale conversion, but it'll do
        static public ByteImage ToGreyscale(ByteImage inputImage,bool alpha100=true)
        {
            byte[] inputImageData = inputImage.imageData;
            int width = inputImage.width;
            int height = inputImage.height;
            int stride = inputImage.stride;

            byte[] output = new byte[inputImageData.Length];
            int strideHere = 0;
            int xX4;
            int offsetHere;

            byte tmpLuma;

            for (int y = 0; y < height; y++)
            {
                strideHere = stride * y;
                for (int x = 0; x < width; x++) // 4 bc RGBA
                {
                    xX4 = x * 4;
                    offsetHere = strideHere + xX4;

                    tmpLuma = (byte)(inputImageData[offsetHere] * 0.11 + 0.59 * inputImageData[offsetHere + 1] + 0.3 * inputImageData[offsetHere + 2]);

                    output[offsetHere] = tmpLuma;
                    output[offsetHere + 1] = tmpLuma;
                    output[offsetHere + 2] = tmpLuma;
                    output[offsetHere+3] = alpha100 ? (byte)255 : inputImageData[offsetHere +3];
                }
            }

            return new ByteImage(output, stride, width, height,inputImage.pixelFormat);
        }

        /*
        // don't use this, it's complete garbage and slow and doesn't even work remotely.
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

                        output[offsetHere] = (byte)Math.Min(255,Math.Max(0,tmpColor.X));
                        output[offsetHere + 1] = (byte)Math.Min(255, Math.Max(0, tmpColor.Y));
                        output[offsetHere + 2] = (byte)Math.Min(255, Math.Max(0, tmpColor.Z));
                    }
                    output[offsetHere + 3] = 255;

                    firstPassFinished = true;

                    lastLeftMin = leftMin;
                    lastRightMax = rightMax;
                    lastLeftmostBlock.color = leftmostBlock.color;
                    lastLeftmostBlock.divisor = leftmostBlock.divisor;
                }
            }

            return new ByteImage(output,stride,width,height);
        }
        */
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
