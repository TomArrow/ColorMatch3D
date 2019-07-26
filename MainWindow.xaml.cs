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

namespace ChannelMixMatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : win.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            updateIter();
        }

        const int R = 0;
        const int G = 1;
        const int B = 2;

        private Bitmap testImage = null;
        private Bitmap regradedImage = null;
        private Bitmap referenceImage = null;

        // Select test image
        private void SelectTest_Click(object sender, win.RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PNG Files (.png)|*.png";
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
            ofd.Filter = "PNG Files (.png)|*.png";
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

        // ColorMatch caller
        private async void DoColorMatch()
        {

            if (testImage == null || referenceImage == null)
            {
                setStatus("Need both a test image and a reference image to match colors.",true);
                return;
            }

            float rangeMin, rangeMax, sliderRangeMin, sliderRangeMax, precision;
            int subdiv, resX, resY;

            try
            {
                rangeMin = float.Parse(MatchFrom_txt.Text);
                rangeMax = float.Parse(MatchTo_txt.Text);
                sliderRangeMin = float.Parse(ChannelMixSliderFrom_txt.Text);
                sliderRangeMax = float.Parse(ChannelMixSliderTo_txt.Text);
                precision = float.Parse(Precision_txt.Text);
                subdiv = int.Parse(Subdiv_txt.Text);
                resX = int.Parse(MatchResX_txt.Text);
                resY = int.Parse(MatchResY_txt.Text);

            }
            catch (Exception blah)
            {

                setStatus("Make sure you only entered valid numbers.",true);
                return;
            }

            var progress = new Progress<MatchReport>(update =>
            {
                setStatus(update.message,update.error);
                // Update matrix
                if(update.best_matrix != null)
                {
                    RegradeImage(update.best_matrix);
                    SetSlidersToMatrix(update.best_matrix);
                }
                if(update.matching_range != null)
                {
                    SetSliderRanges(update.matching_range);
                }
            });
            Task.Run(() => DoColorMatch_Worker(progress,rangeMin,rangeMax,sliderRangeMin,sliderRangeMax,precision,subdiv,resX,resY,testImage,referenceImage));
            setStatus("test");
        }

        private void SetSliderRanges(float[,] ranges)
        {
            slide_RtoR.IsSelectionRangeEnabled = true;
            slide_GtoR.IsSelectionRangeEnabled = true;
            slide_BtoR.IsSelectionRangeEnabled = true;
            slide_RtoG.IsSelectionRangeEnabled = true;
            slide_GtoG.IsSelectionRangeEnabled = true;
            slide_BtoG.IsSelectionRangeEnabled = true;
            slide_RtoB.IsSelectionRangeEnabled = true;
            slide_GtoB.IsSelectionRangeEnabled = true;
            slide_BtoB.IsSelectionRangeEnabled = true;
            slide_RtoR.SelectionStart = ranges[0, 0];
            slide_RtoR.SelectionEnd = ranges[0, 1];
            slide_GtoR.SelectionStart = ranges[1, 0];
            slide_GtoR.SelectionEnd = ranges[1, 1];
            slide_BtoR.SelectionStart = ranges[2, 0];
            slide_BtoR.SelectionEnd = ranges[2, 1];
            slide_RtoG.SelectionStart = ranges[3, 0];
            slide_RtoG.SelectionEnd = ranges[3, 1];
            slide_GtoG.SelectionStart = ranges[4, 0];
            slide_GtoG.SelectionEnd = ranges[4, 1];
            slide_BtoG.SelectionStart = ranges[5, 0];
            slide_BtoG.SelectionEnd = ranges[5, 1];
            slide_RtoB.SelectionStart = ranges[6, 0];
            slide_RtoB.SelectionEnd = ranges[6, 1];
            slide_GtoB.SelectionStart = ranges[7, 0];
            slide_GtoB.SelectionEnd = ranges[7, 1];
            slide_BtoB.SelectionStart = ranges[8, 0];
            slide_BtoB.SelectionEnd = ranges[8, 1];
        }

        private void SetSlidersToMatrix(float[,] matrix)
        {
            slide_RtoR.Value = matrix[0, 0];
            slide_GtoR.Value = matrix[0, 1];
            slide_BtoR.Value = matrix[0, 2];
            slide_RtoG.Value = matrix[1, 0];
            slide_GtoG.Value = matrix[1, 1];
            slide_BtoG.Value = matrix[1, 2];
            slide_RtoB.Value = matrix[2, 0];
            slide_GtoB.Value = matrix[2, 1];
            slide_BtoB.Value = matrix[2, 2];
        }

        private void RegradeImage(float[,] matrix)
        {
            regradedImage = new Bitmap(testImage);
            int width = regradedImage.Width;
            int height = regradedImage.Height;

            float[] regradedImgData = new float[3];
            for(int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixelColor = regradedImage.GetPixel(x, y);
                    regradedImgData[R] = Math.Max(0, Math.Min(255,pixelColor.R * matrix[0, 0] + pixelColor.G * matrix[0, 1] + pixelColor.B * matrix[0, 2]));
                    regradedImgData[G] = Math.Max(0, Math.Min(255, pixelColor.R * matrix[1, 0] + pixelColor.G * matrix[1, 1] + pixelColor.B * matrix[1, 2]));
                    regradedImgData[B] = Math.Max(0, Math.Min(255, pixelColor.R * matrix[2, 0] + pixelColor.G * matrix[2, 1] + pixelColor.B * matrix[2, 2]));
                    regradedImage.SetPixel(x, y, Color.FromArgb(255,(int)regradedImgData[R], (int)regradedImgData[G], (int)regradedImgData[B]));
                }
            }
            ImageTop.Source = Helpers.BitmapToImageSource(regradedImage);
        }


        // The actual colormatching.
        private async void  DoColorMatch_Worker(IProgress<MatchReport> progress, float rangeMin, float rangeMax, float sliderRangeMin, float sliderRangeMax, float precision, int subdiv, int resX, int resY, Bitmap testImage, Bitmap referenceImage)
        {
            

            //TODO Sanity checks: rangeMax msut be > rangemin etc.

            //Resize both images to resX,resY
            //TODO: Do proper algorithm that ignores blown highlights
            Bitmap resizedTestImage = new Bitmap(testImage,new Size(resX,resY));
            Bitmap resizedReferenceImage = new Bitmap(referenceImage,new Size(resX,resY));
            
            int[,,] testImgData = new int[resX, resY, 3];
            int[,,] refImgData = new int[resX, resY, 3];

            // Convert images into arrays for faster access (hopefully)
            for(var x = 0; x < resX; x++)
            {
                for (var y = 0; y < resX; y++)
                {
                    Color testPixel = resizedTestImage.GetPixel(x, y);
                    testImgData[x, y, R] = testPixel.R;
                    testImgData[x, y, G] = testPixel.G;
                    testImgData[x, y, B] = testPixel.B;

                    Color referencePixel = resizedTestImage.GetPixel(x, y);
                    refImgData[x, y, R] = referencePixel.R;
                    refImgData[x, y, G] = referencePixel.G;
                    refImgData[x, y, B] = referencePixel.B;
                }
            }

            // Step Size for individual sliders
            float stepSize = (rangeMax - rangeMin)/subdiv;



            // Nest all sliders (this is where it gets computationally intensive very quick.
            double count = 0;
            float[] testColor = new float[3];
            float[] refColor = new float[3];
            float[] testMatrixed = new float[3];
            float multiplier,multiplierRef;
            float average_diff;
            float best_average_diff = 255;
            float[,] current_matrix = new float[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            float[,] best_matrix = new float[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

            int iteration = 0;
            while (stepSize > precision && iteration == 0)
            {
                progress.Report(new MatchReport(
                    "Colormatching... iteration "+(iteration+1).ToString()+", stepSize "+stepSize.ToString()+", desired precision "+precision.ToString(),
                    false,
                    null,
                    new float[9, 2] { 
                        { rangeMin, rangeMax}, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax }, 
                        { rangeMin, rangeMax } }
                    ));

                for (int iRtoR = 0; iRtoR < subdiv; iRtoR++)
                {
                    for (int iGtoR = 0; iGtoR < subdiv; iGtoR++)
                    {
                        for (int iBtoR = 0; iBtoR < subdiv; iBtoR++)
                        {
                            for (int iRtoG = 0; iRtoG < subdiv; iRtoG++)
                            {
                                for (int iGtoG = 0; iGtoG < subdiv; iGtoG++)
                                {
                                    for (int iBtoG = 0; iBtoG < subdiv; iBtoG++)
                                    {
                                        for (int iRtoB = 0; iRtoB < subdiv; iRtoB++)
                                        {
                                            for (int iGtoB = 0; iGtoB < subdiv; iGtoB++)
                                            {
                                                for (int iBtoB = 0; iBtoB < subdiv; iBtoB++)
                                                {

                                                    float total_diff = 0;
                                                    float count_diff = 0; //How many diff-values were added to total_diff, so that an average can be calculated

                                                    // Calculate matrix
                                                    current_matrix = new float[3, 3] {
                                                    { iRtoR*stepSize+rangeMin, iGtoR*stepSize+rangeMin, iBtoR*stepSize+rangeMin },
                                                    { iRtoG*stepSize+rangeMin, iGtoG*stepSize+rangeMin, iBtoG*stepSize+rangeMin },
                                                    { iRtoB*stepSize+rangeMin, iGtoB*stepSize+rangeMin, iBtoB*stepSize+rangeMin } };
                                                    for (var x = 0; x < resX; x++)
                                                    {
                                                        for (var y = 0; y < resX; y++)
                                                        {

                                                            // Apply matrix
                                                            testMatrixed[R] = testImgData[x, y, R] * current_matrix[0, 0] + testImgData[x, y, G] * current_matrix[0, 1] + testImgData[x, y, B] * current_matrix[0, 2];
                                                            testMatrixed[G] = testImgData[x, y, R] * current_matrix[1, 0] + testImgData[x, y, G] * current_matrix[1, 1] + testImgData[x, y, B] * current_matrix[1, 2];
                                                            testMatrixed[B] = testImgData[x, y, R] * current_matrix[2, 0] + testImgData[x, y, G] * current_matrix[2, 1] + testImgData[x, y, B] * current_matrix[2, 2];

                                                            // Normalize test img. We want only the absolute relations of colors.
                                                            // Find channel with highest value, set it to 255, then scale otehr channels accordingly
                                                            // 
                                                            if (testMatrixed[R] >= testMatrixed[G] && testMatrixed[R] >= testMatrixed[B])
                                                            {
                                                                multiplier = 255f / (float)testMatrixed[R];
                                                                multiplierRef = 255f / (float)refImgData[x, y, R];
                                                                testColor[R] = 255;
                                                                testColor[G] = multiplier * testMatrixed[G];
                                                                testColor[B] = multiplier * testMatrixed[B];
                                                                refColor[R] = 255;
                                                                refColor[G] = multiplierRef * refImgData[x, y, G];
                                                                refColor[B] = multiplierRef * refImgData[x, y, B];
                                                            }
                                                            else if (testMatrixed[G] >= testMatrixed[R] && testMatrixed[G] >= testMatrixed[B])
                                                            {

                                                                multiplier = 255f / (float)testMatrixed[G];
                                                                multiplierRef = 255f / (float)refImgData[x, y, G];
                                                                testColor[R] = multiplier * testMatrixed[R];
                                                                testColor[G] = 255;
                                                                testColor[B] = multiplier * testMatrixed[B];
                                                                refColor[R] = multiplierRef * refImgData[x, y, R];
                                                                refColor[G] = 255;
                                                                refColor[B] = multiplierRef * refImgData[x, y, B];
                                                            }
                                                            else if (testMatrixed[B] >= testMatrixed[R] && testMatrixed[B] >= testMatrixed[G])
                                                            {

                                                                multiplier = 255f / (float)testMatrixed[B];
                                                                multiplierRef = 255f / (float)refImgData[x, y, B];
                                                                testColor[R] = multiplier * testMatrixed[R];
                                                                testColor[G] = multiplier * testMatrixed[G];
                                                                testColor[B] = 255;
                                                                refColor[R] = multiplier * refImgData[x, y, R];
                                                                refColor[G] = multiplier * refImgData[x, y, G];
                                                                refColor[B] = 255;
                                                            }

                                                            total_diff += Math.Abs(testColor[R] - refColor[R]) + Math.Abs(testColor[G] - refColor[G]) + Math.Abs(testColor[B] - refColor[B]);
                                                            count_diff++;

                                                            count++;
                                                        }
                                                    }
                                                    // Calculate average diff for this particular matrix
                                                    // If it's better than the one saved as best_average_diff,
                                                    // it overwrites it and this matrix takes the place of best_matrix
                                                    average_diff = total_diff / count_diff;
                                                    if (average_diff < best_average_diff)
                                                    {
                                                        best_average_diff = average_diff;
                                                        best_matrix = current_matrix;
                                                        progress.Report(new MatchReport(count.ToString("#,##0") + ", best average diff: " + best_average_diff.ToString(), false, current_matrix));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                progress.Report(new MatchReport(count.ToString("#,##0") + ", best average diff: " + best_average_diff.ToString())); // Can't put it further in or it won't update at all. Dunno why. It gets called, but doesn't update UI. PC freezes tho, maybe performance/priority problem?

                            }
                        }
                    }
                }
                iteration++;
            }

            progress.Report(new MatchReport(count.ToString("#,##0") + ", best average diff: " + best_average_diff.ToString()));

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

        private void updateIter_Change(object sender, win.RoutedEventArgs e)
        {


            updateIter();

        }

        private void updateIter()
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
        }

        
    }
}
