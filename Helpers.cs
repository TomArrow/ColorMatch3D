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

            return new ByteImage(rgbValues, stride);
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
        /*static public Vector3 sRGBToCIELab(Vector3 sRGBInput)
        {

        }

        static public Vector3 CIELabTosRGB(Vector3 CIELabInput) {

        }
        */
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

        static public Vector3 XYZtoRGB(Vector3 XYZInput)
        {
            // (Observer = 2°, Illuminant = D65)
            float x = XYZInput.X / 100.0f;
            float y = XYZInput.Y / 100.0f;
            float z = XYZInput.Z / 100.0f;

            float r = x * 3.2406f + y * -1.5372f + z * -0.4986f;
            float g = x * -0.9689f + y * 1.8758f + z * 0.0415f;
            float b = x * 0.0557f + y * -0.2040f + z * 1.0570f;

            XYZInput.X = r > 0.0031308f ? 1.055f * (float)Math.Pow(r, 1 / 2.4) - 0.055f : 12.92f * r;
            XYZInput.Y = g > 0.0031308f ? 1.055f * (float)Math.Pow(g, 1 / 2.4) - 0.055f : 12.92f * g;
            XYZInput.Z = b > 0.0031308f ? 1.055f * (float)Math.Pow(b, 1 / 2.4) - 0.055f : 12.92f * b;

            return XYZInput * 255.0f;
        }

    }
}
