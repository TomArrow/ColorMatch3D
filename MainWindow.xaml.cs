using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using win = System.Windows;
using System.Drawing;
using controls = System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using media = System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Threading;
using Be.IO;

namespace ColorMatch3D
{

    struct FloatColor
    {
        public float R, G, B;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : win.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        const int R = 0;
        const int G = 1;
        const int B = 2;

        private Bitmap testImage = null;
        private Bitmap regradedImage = null;
        private Bitmap referenceImage = null;

        float displayGamma = 2.2f;

        // Select test image
        private void SelectTest_Click(object sender, win.RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (.png,.jpg,.jpeg,.tif,.tiff,.tga)|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.tga";
            if(ofd.ShowDialog() == true)
            {
                string filename = ofd.FileName;
                testImage = (Bitmap) Bitmap.FromFile(filename);
                ImageTop.Source = Helpers.BitmapToImageSource(testImage);
            }
        }

        // Select reference image
        private void SelectReference_Click(object sender, win.RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (.png,.jpg,.jpeg,.tif,.tiff,.tga)|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.tga";
            if (ofd.ShowDialog() == true)
            {
                string filename = ofd.FileName;
                referenceImage = (Bitmap)Bitmap.FromFile(filename);
                ImageBottom.Source = Helpers.BitmapToImageSource(referenceImage);
            }
        }

        private void DoColorMatch_Click(object sender, win.RoutedEventArgs e)
        {
            DoColorMatch();
        }

        private Task ColorMatchTask;
        private FloatColor[,,] cube = null;
        
        // ColorMatch caller
        private async void DoColorMatch()
        {

            if (testImage == null || referenceImage == null)
            {
                setStatus("Need both a test image and a reference image to match colors.",true);
                return;
            }

            float rangeMin, rangeMax, sliderRangeMin, sliderRangeMax, precision, workGamma, testGamma, refGamma;
            int subdiv, resX, resY;

            try
            {
                // Get variables/config

            }
            catch (Exception blah)
            {

                setStatus("Make sure you only entered valid numbers.",true);
                return;
            }

            var progress = new Progress<MatchReport>(update =>
            {
                setStatus(update.message, update.error);
                if(update.cube != null)
                {
                    cube = update.cube;
                }

            });
            ColorMatchTask = Task.Run(() => DoColorMatch_Worker(progress,testImage,referenceImage));
            setStatus("Started...");
        }



        private CancellationTokenSource _cancelRegrade = new CancellationTokenSource();


        //defunct
        private async void  RegradeImage(float[,] matrix)
        {
            return;
            /*
            _cancelRegrade.Cancel();

            _cancelRegrade = new CancellationTokenSource();
            CancellationToken token = _cancelRegrade.Token;

            float workGamma, testGamma;
            try
            {
                // Get some config data or whatever

            }
            catch (Exception blah)
            {

                setStatus("Make sure you only entered valid numbers.", true);
                return;
            }

            try
            {
                Bitmap tmp = new Bitmap(testImage);
                BitmapSource result = await Task.Run(() => DoRegrade_Worker( tmp, token));
                    ImageTop.Source = result;
            }
            catch (OperationCanceledException)
            {
                //Nothing
            }
            */
            
        }

        // defunct
        private BitmapSource DoRegrade_Worker(float[,] matrix, float testGamma, float workGamma, Bitmap testImage,CancellationToken token)
        {
            return Helpers.BitmapToImageSource(testImage);
            Bitmap regradedImage = new Bitmap(testImage);
            int width = regradedImage.Width;
            int height = regradedImage.Height;

            float[] regradedImgData = new float[3];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    token.ThrowIfCancellationRequested();
                    Color pixelColor = regradedImage.GetPixel(x, y);

                    float[] workGammaPixel = new float[3]{
                        (float)(255 * Math.Pow((pixelColor.R / 255d), testGamma / workGamma)),
                        (float)(255 * Math.Pow((pixelColor.G / 255d), testGamma / workGamma)),
                        (float)(255 * Math.Pow((pixelColor.B / 255d), testGamma / workGamma))
                    };

                    regradedImgData[R] = Math.Max(0, Math.Min(255, workGammaPixel[R] * matrix[0, 0] + workGammaPixel[G] * matrix[0, 1] + workGammaPixel[B] * matrix[0, 2]));
                    regradedImgData[G] = Math.Max(0, Math.Min(255, workGammaPixel[R] * matrix[1, 0] + workGammaPixel[G] * matrix[1, 1] + workGammaPixel[B] * matrix[1, 2]));
                    regradedImgData[B] = Math.Max(0, Math.Min(255, workGammaPixel[R] * matrix[2, 0] + workGammaPixel[G] * matrix[2, 1] + workGammaPixel[B] * matrix[2, 2]));
                    regradedImage.SetPixel(x, y, Color.FromArgb(255, 
                        (int)(Math.Pow(regradedImgData[R] / 255d, workGamma / displayGamma) * 255), 
                        (int)(Math.Pow(regradedImgData[G] / 255d, workGamma / displayGamma) * 255), 
                        (int)(Math.Pow(regradedImgData[B] / 255d, workGamma / displayGamma) * 255)));
                }
            }
            token.ThrowIfCancellationRequested();
            BitmapSource result = Helpers.BitmapToImageSource(regradedImage);
            token.ThrowIfCancellationRequested();
            result.Freeze();
            return result;
        }

        private enum DOWNSCALE { DEFAULT,NN}

        int outputValueCount = 32;


        const int RCORD = 3;
        const int GCORD = 4;
        const int BCORD = 5;


        

        struct ColorPair
        {
            public int R, G, B, RCORD, GCORD, BCORD;
        };

        struct AverageData
        {
            public double totalR,totalG,totalB;
            public float divisor;
            public float valueR, valueG, valueB;
        };

        struct ColorPairData
        {
            public byte R, G, B, RCORD, GCORD, BCORD;
            public byte nearestQuadrantR, nearestQuadrantG, nearestQuadrantB;
        };

        /*internal unsafe struct Int32Buffer
        {
            private int _e00, _e02, _e03, _e04, _e05;

            public ref int this[int index]
            {
                get
                {
                    fixed (int* p = &_e00) return ref p[index];
                }
            }
        }*/



        // The actual colormatching.
        private void DoColorMatch_Worker(IProgress<MatchReport> progress,Bitmap testImage, Bitmap referenceImage)
        {

            var watch = System.Diagnostics.Stopwatch.StartNew();
            List<long> durations = new List<long>();

            
            //TODO Sanity checks: rangeMax msut be > rangemin etc.

            //Resize both images to resX,resY
            //TODO: Do proper algorithm that ignores blown highlights
            // TODO: Add "default linear" downscaler that corrects gamma before downscaling
            // TODO: add special downscaler that picks only useful pixels
            // TODO Add second special downscaler that isn't really a downscaler but one that picks most important colors including a slight average.
            Bitmap resizedTestImage, resizedReferenceImage;

            int resX = testImage.Width, resY = testImage.Height;

            byte[,,] testImgData = new byte[resX, resY, 3];
            byte[,,] refImgData = new byte[resX, resY, 3];

            // 3D Histogram.
            // Each possible color in a 256x256x256 RGB colorspace has one entry.
            // Each entry is a list of int[] arrays, each containing an RGB color
            // The [256,256,256] array represents the colors of the test image
            // The int[] arrays represent corresponding colors in the reference image that were found in an identical position.
            //List<int[]>[,,] histogram3D = new List<int[]>[256, 256, 256];

            // Got tip:
            /*
             * ```cs
                public struct Color
                {
                    public byte R;
                    public byte G;
                    public byte B;
    
                    // rest of logic
                }```
             * */


            resizedReferenceImage = new Bitmap(referenceImage, new Size(resX, resY));
            //Bitmap debugBitmap = new Bitmap(referenceImage, new Size(resX, resY));

            // Need to do new Bitmap() because it converts to ARGB, and we have consistency for the following loop, otherwise shit results.
            // No need to do for reference image because it was already resized and thus regenerated.
            ByteImage testBitmap = Helpers.BitmapToByteArray(new Bitmap(testImage));
            ByteImage referenceBitmap = Helpers.BitmapToByteArray(resizedReferenceImage);

            // Convert images into arrays for faster access (hopefully)
            for (var x = 0; x < resX; x++)
            {
                for (var y = 0; y < resY; y++)
                {
                    testImgData[x, y, B] = testBitmap[testBitmap.stride * y + x * 4];
                    testImgData[x, y, G] = testBitmap[testBitmap.stride * y + x * 4 + 1];
                    testImgData[x, y, R] = testBitmap[testBitmap.stride * y + x * 4 + 2];

                    refImgData[x, y, B] = referenceBitmap[referenceBitmap.stride * y + x * 4];
                    refImgData[x, y, G] = referenceBitmap[referenceBitmap.stride * y + x * 4 + 1];
                    refImgData[x, y, R] = referenceBitmap[referenceBitmap.stride * y + x * 4 + 2];

                    /*debugBitmap.SetPixel(x, y, Color.FromArgb(testBitmap[testBitmap.stride * y + x * 3],
                        refImgData[x, y, G] = testBitmap[testBitmap.stride * y + x * 3 + 1],
                        refImgData[x, y, R] = testBitmap[testBitmap.stride * y + x * 3 + 2]));*/


                }
            }

            //debugBitmap.Save("test2.png");

            durations.Add(watch.ElapsedMilliseconds);


            // this will save which cube parts the algo should even bother to loop through. will set bool to true for that segment if anything was found in there.
            // Most of the final cube will always be empty anyway, we can use this to our advantage.
            bool[,,] preCube = new bool[outputValueCount, outputValueCount, outputValueCount];



            int steps = outputValueCount - 1;
            float stepSize = 255 / (float)steps;
            byte stepR, stepG, stepB;
            int trueStepR, trueStepG, trueStepB;
            ColorPair thisPoint  = new ColorPair();
            ColorPairData thisPointLinear  = new ColorPairData();
            int rAround, gAround, bAround;



            int pixelCount = resX * resY;
            ColorPairData[] collectCubeLinear = new ColorPairData[pixelCount];

            int collectCubeLinearIndex = 0;
            // Build full histogram
            for (var x = 0; x < resX; x++)
            {
                for (var y = 0; y < resY; y++)
                {


                    // set preCube (massive speedup later)
                    stepR = (byte)Math.Round(testImgData[x, y, R] / stepSize);
                    stepG = (byte)Math.Round(testImgData[x, y, G] / stepSize);
                    stepB = (byte)Math.Round(testImgData[x, y, B] / stepSize);
                    
                    thisPointLinear.R = refImgData[x, y, R];
                    thisPointLinear.G = refImgData[x, y, G];
                    thisPointLinear.B = refImgData[x, y, B];
                    thisPointLinear.RCORD = testImgData[x, y, R];
                    thisPointLinear.GCORD = testImgData[x, y, G];
                    thisPointLinear.BCORD = testImgData[x, y, B];
                    thisPointLinear.nearestQuadrantR = stepR;
                    thisPointLinear.nearestQuadrantG = stepG;
                    thisPointLinear.nearestQuadrantB = stepB;
                    collectCubeLinear[collectCubeLinearIndex++] = thisPointLinear;
                    

                }

                progress.Report(new MatchReport("Building histogram [" + x + ", x], "));
            }

            durations.Add(watch.ElapsedMilliseconds);

            // Build 32x32x32 cube data ( TODO later make the precision flexible)
            AverageData[,,] tmpCube = new AverageData[outputValueCount, outputValueCount, outputValueCount];
            double count = 0;
            float weight;
            float tmp1, tmp2, tmp3;
            double[] sum;
            float divisor;
            int redQuadrant, greenQuadrant, blueQuadrant;
            float red, green, blue;
            List<ColorPair> collectCubeHere;

            float sqrtOf3 = (float)Math.Sqrt(3);
            AverageData tmpAverageData = new AverageData();

            foreach(ColorPairData collectCubeLinearHere in collectCubeLinear)
            {
                redQuadrant = collectCubeLinearHere.nearestQuadrantR;
                greenQuadrant = collectCubeLinearHere.nearestQuadrantG;
                blueQuadrant = collectCubeLinearHere.nearestQuadrantB;
                                
                

                count++;
                // This loop is currently the bottleneck.
                for (rAround = -1; rAround <= 1; rAround++)
                {
                    for (gAround = -1; gAround <= 1; gAround++)
                    {
                        for (bAround = -1; bAround <= 1; bAround++)
                        {
                            trueStepR = Math.Max(0, Math.Min(steps, redQuadrant + rAround));
                            trueStepG = Math.Max(0, Math.Min(steps, greenQuadrant + gAround));
                            trueStepB = Math.Max(0, Math.Min(steps, blueQuadrant + bAround));

                            // 5- Euklidian distance self-multiply
                            tmp1 = (trueStepR * stepSize - collectCubeLinearHere.RCORD) / stepSize;
                            tmp2 = (trueStepG * stepSize - collectCubeLinearHere.GCORD) / stepSize;
                            tmp3 = (trueStepB * stepSize - collectCubeLinearHere.BCORD) / stepSize;
                            weight = Math.Max(0, sqrtOf3 - (float)Math.Sqrt(
                                (tmp1 * tmp1
                                + tmp2 * tmp2
                                + tmp3 * tmp3)
                                ));


                            tmpAverageData = tmpCube[trueStepR, trueStepG, trueStepB];

                            tmpAverageData.totalR += collectCubeLinearHere.R * weight;
                            tmpAverageData.totalG += collectCubeLinearHere.G * weight;
                            tmpAverageData.totalB += collectCubeLinearHere.B * weight;
                            tmpAverageData.divisor += weight;
                            tmpAverageData.valueR = (float)tmpAverageData.totalR / tmpAverageData.divisor;
                            tmpAverageData.valueG = (float)tmpAverageData.totalG / tmpAverageData.divisor;
                            tmpAverageData.valueB = (float)tmpAverageData.totalB / tmpAverageData.divisor;

                            tmpCube[trueStepR, trueStepG, trueStepB] = tmpAverageData;
                        }
                    }
                }
            }


            durations.Add(watch.ElapsedMilliseconds);



            FloatColor[,,] cube = new FloatColor[outputValueCount, outputValueCount, outputValueCount];
            FloatColor tmpFloatColor;

            // transfer tmpCube to normal cube
            // May seem slow from the code but actually only takes about 1 ms, it's ridiculously fast.
            for (redQuadrant = 0; redQuadrant<outputValueCount; redQuadrant++)
            {
                for (greenQuadrant = 0; greenQuadrant < outputValueCount; greenQuadrant++)
                {
                    for (blueQuadrant = 0; blueQuadrant < outputValueCount; blueQuadrant++)
                    {
                        tmpAverageData = tmpCube[redQuadrant, greenQuadrant, blueQuadrant];
                        tmpFloatColor.R = tmpAverageData.valueR;
                        tmpFloatColor.G = tmpAverageData.valueG;
                        tmpFloatColor.B = tmpAverageData.valueB;
                        cube[redQuadrant, greenQuadrant, blueQuadrant] = tmpFloatColor;
                    }
                }
            }


            durations.Add(watch.ElapsedMilliseconds);

            watch.Stop();

            string durationString = "";
            foreach(long duration in durations)
            {
                durationString += duration.ToString() + " ms ";
            }

            progress.Report(new MatchReport(count.ToString("#,##0")+" iters, "+durationString,false,cube));

        }



        private media.Brush redBackground = new media.SolidColorBrush(media.Color.FromRgb(255,0,0));
        private media.Brush transparentBackground = new media.SolidColorBrush(media.Color.FromArgb(0,0,0,0));
        private media.Brush whiteText = new media.SolidColorBrush(media.Color.FromRgb(255,255,255));
        private media.Brush blackText = new media.SolidColorBrush(media.Color.FromRgb(0,0,0));

        private void setStatus(string status, bool error=false)
        {
            Status_txt.Text = status;
            if (error)
            {
                Status_txt.Background = redBackground;
                Status_txt.Foreground = whiteText;
            } else
            {

                Status_txt.Background = transparentBackground;
                Status_txt.Foreground = blackText;
            }
        }

        private void BtnMakeLUT_Click(object sender, win.RoutedEventArgs e)
        {
            
            if(cube == null)
            {
                setStatus("no LUT was generated yet.",true);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CUBE 3D LUT (.cube)|*.cube";
            
            int steps = outputValueCount - 1;
            float stepSize = 255 / steps;

            float red, green, blue;

            //sfd.FileName = ;
            if (sfd.ShowDialog() == true)
            {
                string luttext = "LUT_3D_SIZE "+ outputValueCount + "\n";

                for (int b = 0; b < outputValueCount; b++)
                {
                    for (int g = 0; g < outputValueCount; g++)
                    {
                        for (int r = 0; r < outputValueCount; r++)
                        {
                            /*red = float.IsNaN(cube[r, g, b][R]) ? r*stepSize/255 : cube[r, g, b][R]/255;
                            green = float.IsNaN(cube[r, g, b][G]) ? g * stepSize/255 : cube[r, g, b][G]/255;
                            blue = float.IsNaN(cube[r, g, b][B]) ? b * stepSize/255 : cube[r, g, b][B]/255;*/
                            red = float.IsNaN(cube[r, g, b].R) ? 0 : cube[r, g, b].R/255;
                            green = float.IsNaN(cube[r, g, b].G) ?0 : cube[r, g, b].G/255;
                            blue = float.IsNaN(cube[r, g, b].B) ? 0 : cube[r, g, b].B/255;
                            luttext += red + " " + green + " " + blue+"\n";
                        }
                    }
                }

                File.WriteAllText(sfd.FileName, luttext);
            }
        }


        /*private void updateIter()
        {
            try
            {
                int resX = int.Parse(MatchResX_txt.Text);
                int resY = int.Parse(MatchResY_txt.Text);
                int subdiv = int.Parse(Subdiv_txt.Text);

                double iterSteps = (double)Math.Pow(subdiv, 9) * resX * resY;
                IterSteps_txt.Text = iterSteps.ToString("#,##0");
            }
            catch (Exception blah)
            {
                try
                {
                    IterSteps_txt.Text = "N/A";
                }
                catch (Exception blah2)
                {
                    // nothing
                }
            }
        }*/



    }
}
