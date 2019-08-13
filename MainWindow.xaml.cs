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
        private float[,,][] cube = null;
        
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

        /*private async void  RegradeImage(float[,] matrix)
        {
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
            
        }

        private BitmapSource DoRegrade_Worker(float[,] matrix, float testGamma, float workGamma, Bitmap testImage,CancellationToken token)
        {
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
        }*/

        private enum DOWNSCALE { DEFAULT,NN}

        int outputValueCount = 32;

        // The actual colormatching.
        private void DoColorMatch_Worker(IProgress<MatchReport> progress,Bitmap testImage, Bitmap referenceImage)
        {


            //TODO Sanity checks: rangeMax msut be > rangemin etc.

            //Resize both images to resX,resY
            //TODO: Do proper algorithm that ignores blown highlights
            // TODO: Add "default linear" downscaler that corrects gamma before downscaling
            // TODO: add special downscaler that picks only useful pixels
            // TODO Add second special downscaler that isn't really a downscaler but one that picks most important colors including a slight average.
            Bitmap resizedTestImage, resizedReferenceImage;

            int resX = testImage.Width, resY = testImage.Height;

            int[,,] testImgData = new int[resX, resY, 3];
            int[,,] refImgData = new int[resX, resY, 3];

            // 3D Histogram.
            // Each possible color in a 256x256x256 RGB colorspace has one entry.
            // Each entry is a list of int[] arrays, each containing an RGB color
            // The [256,256,256] array represents the colors of the test image
            // The int[] arrays represent corresponding colors in the reference image that were found in an identical position.
            List<int[]>[,,] histogram3D = new List<int[]>[256, 256, 256];

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

            // Convert images into arrays for faster access (hopefully)
            for (var x = 0; x < resX; x++)
            {
                for (var y = 0; y < resY; y++)
                {
                    Color testPixel = testImage.GetPixel(x, y);
                    testImgData[x, y, R] = testPixel.R;
                    testImgData[x, y, G] = testPixel.G;
                    testImgData[x, y, B] = testPixel.B;

                    Color referencePixel = resizedReferenceImage.GetPixel(x, y);
                    refImgData[x, y, R] = referencePixel.R;
                    refImgData[x, y, G] = referencePixel.G;
                    refImgData[x, y, B] = referencePixel.B;
                }
            }


            // this will save which cube parts the algo should even bother to loop through. will set bool to true for that segment if anything was found in there.
            // Most of the final cube will always be empty anyway, we can use this to our advantage.
            bool[,,] preCube = new bool[outputValueCount, outputValueCount, outputValueCount];



            int steps = outputValueCount - 1;
            float stepSize = 255 / (float)steps;
            int stepR, stepG, stepB;
            int trueStepR, trueStepG, trueStepB;

            // Build full histogram
            for (var x = 0; x < resX; x++)
            {
                for (var y = 0; y < resY; y++)
                {
                    int[] value = new int[3] { refImgData[x, y, R], refImgData[x, y, G], refImgData[x, y, B] };
                    if(histogram3D[testImgData[x, y, R], testImgData[x, y, G], testImgData[x, y, B]] == null)
                    {
                        histogram3D[testImgData[x, y, R], testImgData[x, y, G], testImgData[x, y, B]] = new List<int[]>();
                    }
                    histogram3D[testImgData[x, y, R], testImgData[x, y, G], testImgData[x, y, B]].Add(value);


                    // set preCube (massive speedup later)
                    stepR = (int)Math.Round(testImgData[x, y, R] / stepSize);
                    stepG = (int)Math.Round(testImgData[x, y, G] / stepSize);
                    stepB = (int)Math.Round(testImgData[x, y, B] / stepSize);
                    for(int rAround = -1; rAround <= 1; rAround++)
                    {
                        for (int gAround = -1; gAround <= 1; gAround++)
                        {
                            for (int bAround = -1; bAround <= 1; bAround++)
                            {
                                trueStepR = Math.Max(0, Math.Min(steps, stepR + rAround));
                                trueStepG = Math.Max(0, Math.Min(steps, stepG + gAround));
                                trueStepB = Math.Max(0, Math.Min(steps, stepB + bAround));
                                preCube[trueStepR,trueStepG,trueStepB] = true;
                            }
                        }
                    }
                }

                progress.Report(new MatchReport("Building histogram [" + x + ", x], "));
            }


            // Build 32x32x32 cube data ( TODO later make the precision flexible)
            float[,,][] cube = new float[outputValueCount, outputValueCount, outputValueCount][];
            double count = 0;
            float weight;
            float tmp1, tmp2, tmp3;
            int histogramCount;
            double[] sum, tmpsum;
            float divisor;
            int redQuadrant, greenQuadrant, blueQuadrant;

            for(float red = 0;  ((int)red) <= 255; red+= stepSize)
            {
                redQuadrant = (int)Math.Round(red / stepSize);
                for (float green = 0; ((int)green) <= 255; green += stepSize)
                {
                    greenQuadrant = (int)Math.Round(green / stepSize);
                    for (float blue = 0; ((int)blue) <= 255; blue += stepSize)
                    {
                        blueQuadrant = (int)Math.Round(blue / stepSize);

                        // Skip the inner loop if there are no values in that area of the histogram anyway.

                        if (preCube[redQuadrant, greenQuadrant, blueQuadrant] != true)
                        {

                            cube[redQuadrant, greenQuadrant, blueQuadrant] = new float[3] { float.NaN, float.NaN, float.NaN };
                            continue;
                        }

                        // Now go through all surrounding values in the 3D histogram and calculate a weighed average (or later median maybe). If the distance is stepSize, weight is 0, if distance is 0, weight is 1
                        // Interpolation will be an average of the linear 1-dimensional distances. (not sure if that's ideal tbh)
                        // Is that biliniear? I'm not sure

                        sum = new double[3] { 0, 0, 0 };
                        divisor = 0;

                        for (float redHist = Math.Max(0,red-stepSize+1); redHist < Math.Min(255,red+stepSize); redHist ++)
                        {
                            for (float greenHist = Math.Max(0,green-stepSize+1); greenHist < Math.Min(255,green+stepSize); greenHist ++)
                            {
                                for (float blueHist = Math.Max(0,blue-stepSize+1); blueHist < Math.Min(255,blueHist+stepSize); blueHist ++)
                                {


                                    // This could be way shorter but I wanted to try optimize the speed a little.
                                    if (histogram3D[(int)redHist, (int)greenHist, (int)blueHist] != null)
                                    {

                                        // 1 - pseudodistance
                                        //float weight = 1- (Math.Abs(red - redHist)/stepSize + Math.Abs(green - greenHist)/stepSize + Math.Abs(blue - blueHist)/stepSize)/3;

                                        // 2 - euklidian distance
                                        // This actually fixes major artifacts compared to the pseudodistance above.
                                        //weight = Math.Max(0,1-(float)Math.Sqrt( (Math.Pow((red - redHist)/stepSize,2) + Math.Pow((green - greenHist)/stepSize,2) + Math.Pow((blue - blueHist)/stepSize,2))));

                                        // 3 - euklidian distance - hacky optimized
                                        // GARBAGE
                                        //int myPower = 12;
                                        //int myPowerOfTwo = 1 << myPower;
                                        // weight = Math.Max(0,1-(float)Math.Sqrt((1 << (int)((red - redHist)/stepSize)) + 1 << ((int)((green - greenHist)/stepSize)) + 1 << ((int)((blue - blueHist)/stepSize))));

                                        // 4 - euklidian distance Blitzpow
                                        //GARBAGE TOO
                                        //weight = Math.Max(0,1-(float)Math.Sqrt( (Helpers.BlitzPow((red - redHist)/stepSize,2) + Helpers.BlitzPow((green - greenHist)/stepSize,2) + Helpers.BlitzPow((blue - blueHist)/stepSize,2))));

                                        // 5- Euklidian distance self-multiply
                                        tmp1 = (red - redHist) / stepSize;
                                        tmp2 = (green - greenHist) / stepSize;
                                        tmp3 = (blue - blueHist) / stepSize;
                                        weight = Math.Max(0, 1 - (float)Math.Sqrt(
                                            (tmp1 * tmp1
                                            + tmp2 * tmp2
                                            + tmp3 * tmp3)
                                            ));

                                        histogramCount = histogram3D[(int)redHist, (int)greenHist, (int)blueHist].Count;
                                        if (histogramCount == 1)
                                        {
                                            
                                            sum[R] += histogram3D[(int)redHist, (int)greenHist, (int)blueHist][0][R] * weight;
                                            sum[G] += histogram3D[(int)redHist, (int)greenHist, (int)blueHist][0][G] * weight;
                                            sum[B] += histogram3D[(int)redHist, (int)greenHist, (int)blueHist][0][B] * weight;
                                            divisor += weight;
                                        } else
                                        {
                                            tmpsum = new double[3] { 0, 0, 0 };
                                            foreach (int[] referenceValue in histogram3D[(int)redHist, (int)greenHist, (int)blueHist])
                                            {

                                                tmpsum[R] += referenceValue[R];
                                                tmpsum[G] += referenceValue[G];
                                                tmpsum[B] += referenceValue[B];
                                            }
                                            sum[R] += tmpsum[R] * weight;
                                            sum[G] += tmpsum[G] * weight;
                                            sum[B] += tmpsum[B] * weight;
                                            divisor += weight * histogramCount;
                                        }
                                        
                                    }
                                    


                                    count++;
                                }
                            }
                        }

                        cube[redQuadrant, greenQuadrant, blueQuadrant] = new float[3] { (float)sum[R]/divisor, (float)sum[G]/divisor, (float)sum[B]/divisor };

                    }

                    progress.Report(new MatchReport("Building cube [" + red + "," + green+", x], "+count.ToString("#,##0")));
                }
            }


            progress.Report(new MatchReport(count.ToString("#,##0"),false,cube));

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
                            red = float.IsNaN(cube[r, g, b][R]) ? 0 : cube[r, g, b][R]/255;
                            green = float.IsNaN(cube[r, g, b][G]) ?0 : cube[r, g, b][G]/255;
                            blue = float.IsNaN(cube[r, g, b][B]) ? 0 : cube[r, g, b][B]/255;
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
