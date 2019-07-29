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
            DOWNSCALE downscaleMethod = DOWNSCALE.DEFAULT;
            DiffMethods.Method diffMethod;

            try
            {
                rangeMin = float.Parse(MatchFrom_txt.Text);
                rangeMax = float.Parse(MatchTo_txt.Text);
                sliderRangeMin = float.Parse(ChannelMixSliderFrom_txt.Text);
                sliderRangeMax = float.Parse(ChannelMixSliderTo_txt.Text);
                precision = float.Parse(Precision_txt.Text);
                workGamma = float.Parse(WorkGamma_txt.Text);
                testGamma = float.Parse(TestImageGamma_txt.Text);
                refGamma = float.Parse(ReferenceImageGamma_txt.Text);
                subdiv = int.Parse(Subdiv_txt.Text);
                resX = int.Parse(MatchResX_txt.Text);
                resY = int.Parse(MatchResY_txt.Text);
                if (useNNDownscale_radio.IsChecked == true)
                {
                    downscaleMethod = DOWNSCALE.NN;
                }
                else if(useDefaultDownscale_radio.IsChecked == true)
                {
                    downscaleMethod = DOWNSCALE.DEFAULT;
                } else
                {
                    downscaleMethod = DOWNSCALE.DEFAULT;
                }
                if (useRelativeDiff_radio.IsChecked == true)
                {
                    diffMethod = DiffMethods.Method.RELATIVE;
                }
                else if (useAbsoluteDiff_radio.IsChecked == true)
                {
                    diffMethod = DiffMethods.Method.ABSOLUTE;
                }
                else if (useSuperRelativeDiff_radio.IsChecked == true)
                {
                    diffMethod = DiffMethods.Method.SUPERRELATIVE;
                }
                else
                {
                    diffMethod = DiffMethods.Method.RELATIVE;
                }

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
                if (update.matching_range != null)
                {
                    SetSliderRanges(update.matching_range);
                }
                if (update.best_matrix != null)
                {
                    SetSlidersToMatrix(update.best_matrix);
                    RegradeImage(update.best_matrix);
                }
            });
            ColorMatchTask = Task.Run(() => DoColorMatch_Worker(progress,rangeMin,rangeMax,sliderRangeMin,sliderRangeMax,precision,workGamma,testGamma,refGamma,subdiv,resX,resY,testImage,referenceImage,downscaleMethod,diffMethod));
            setStatus("Started...");
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


        private CancellationTokenSource _cancelRegrade = new CancellationTokenSource();

        private async void  RegradeImage(float[,] matrix)
        {
            _cancelRegrade.Cancel();

            _cancelRegrade = new CancellationTokenSource();
            CancellationToken token = _cancelRegrade.Token;

            float workGamma, testGamma;
            try
            {
                workGamma = float.Parse(WorkGamma_txt.Text);
                testGamma = float.Parse(TestImageGamma_txt.Text);

            }
            catch (Exception blah)
            {

                setStatus("Make sure you only entered valid numbers.", true);
                return;
            }

            try
            {
                Bitmap tmp = new Bitmap(testImage);
                BitmapSource result = await Task.Run(() => DoRegrade_Worker(matrix, testGamma, workGamma, tmp, token));
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
        }

        private enum DOWNSCALE { DEFAULT,NN}

        // The actual colormatching.
        private void DoColorMatch_Worker(IProgress<MatchReport> progress, float rangeMin, float rangeMax, float sliderRangeMin, float sliderRangeMax, float precision, float workGamma, float testGamma, float refGamma, int subdiv, int resX, int resY, Bitmap testImage, Bitmap referenceImage, DOWNSCALE downscaleMethod, DiffMethods.Method diffMethod)
        {


            //TODO Sanity checks: rangeMax msut be > rangemin etc.

            //Resize both images to resX,resY
            //TODO: Do proper algorithm that ignores blown highlights
            // TODO: Add "default linear" downscaler that corrects gamma before downscaling
            // TODO: add special downscaler that picks only useful pixels
            // TODO Add second special downscaler that isn't really a downscaler but one that picks most important colors including a slight average.
            Bitmap resizedTestImage, resizedReferenceImage;
            switch(downscaleMethod)
            {
                case DOWNSCALE.NN:
                    resizedTestImage = Helpers.ResizeBitmapNN(testImage, resX, resY);
                    resizedReferenceImage = Helpers.ResizeBitmapNN(referenceImage, resX, resY);
                    break;
                case DOWNSCALE.DEFAULT:
                default:
                    resizedTestImage = new Bitmap(testImage, new Size(resX, resY));
                    resizedReferenceImage = new Bitmap(referenceImage, new Size(resX, resY));
                    break;
            }

            float[,,] testImgData = new float[resX, resY, 3];
            float[,,] refImgData = new float[resX, resY, 3];

            // Convert images into arrays for faster access (hopefully)
            for(var x = 0; x < resX; x++)
            {
                for (var y = 0; y < resX; y++)
                {
                    Color testPixel = resizedTestImage.GetPixel(x, y);
                    testImgData[x, y, R] = (float)(255*Math.Pow(((double)testPixel.R / 255d), testGamma/workGamma));
                    testImgData[x, y, G] = (float)(255*Math.Pow(((double)testPixel.G / 255d), testGamma/workGamma));
                    testImgData[x, y, B] = (float)(255*Math.Pow(((double)testPixel.B / 255d), testGamma/workGamma));

                    Color referencePixel = resizedReferenceImage.GetPixel(x, y);
                    refImgData[x, y, R] = (float)(255*Math.Pow(((double)referencePixel.R / 255d), refGamma/workGamma));
                    refImgData[x, y, G] = (float)(255*Math.Pow(((double)referencePixel.G / 255d), refGamma/workGamma));
                    refImgData[x, y, B] = (float)(255*Math.Pow(((double)referencePixel.B / 255d), refGamma/workGamma));
                }
            }

            // Step Size for individual sliders
            float stepSize = (rangeMax - rangeMin)/subdiv;
            float stepSizeNext = stepSize;


            // Nest all sliders (this is where it gets computationally intensive very quick.
            double count = 0;
            double skipped = 0;
            float[] testColor = new float[3];
            float[] refColor = new float[3];
            float[] testMatrixed = new float[3];
            //float multiplier,multiplierRef;
            double average_diff;
            double best_average_diff = double.PositiveInfinity;
            double? tmp_diff;
            float[,] current_matrix = new float[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            float[,] best_matrix = new float[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            float[,] matchRanges = new float[9, 2] {
                        { rangeMin, rangeMax},
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax },
                        { rangeMin, rangeMax } };

            int iteration = 0;
            while (stepSize > precision)
            {
                stepSize = stepSizeNext;

                progress.Report(new MatchReport(
                    "Colormatching... iteration "+(iteration+1).ToString()+", stepSize "+stepSize.ToString()+", desired precision "+precision.ToString(),
                    false,
                    null,
                    matchRanges
                    ));

                bool update_matrix = false;

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

                                                    double total_diff = 0;
                                                    Int64 count_diff = 0; //How many diff-values were added to total_diff, so that an average can be calculated

                                                    // Calculate matrix
                                                    current_matrix = new float[3, 3] {
                                                    { iRtoR*stepSize+matchRanges[0,0], iGtoR*stepSize+matchRanges[1,0], iBtoR*stepSize+matchRanges[2,0] },
                                                    { iRtoG*stepSize+matchRanges[3,0], iGtoG*stepSize+matchRanges[4,0], iBtoG*stepSize+matchRanges[5,0] },
                                                    { iRtoB*stepSize+matchRanges[6,0], iGtoB*stepSize+matchRanges[7,0], iBtoB*stepSize+matchRanges[8,0] } };
                                                    for (var x = 0; x < resX; x++)
                                                    {
                                                        for (var y = 0; y < resX; y++)
                                                        {

                                                            // TODO Alternate difference algorithm that doesn't calculate average difference, but peak difference instead. might help with a few things.

                                                            count++;

                                                            // Apply matrix
                                                            testMatrixed[R] = testImgData[x, y, R] * current_matrix[0, 0] + testImgData[x, y, G] * current_matrix[0, 1] + testImgData[x, y, B] * current_matrix[0, 2];
                                                            testMatrixed[G] = testImgData[x, y, R] * current_matrix[1, 0] + testImgData[x, y, G] * current_matrix[1, 1] + testImgData[x, y, B] * current_matrix[1, 2];
                                                            testMatrixed[B] = testImgData[x, y, R] * current_matrix[2, 0] + testImgData[x, y, G] * current_matrix[2, 1] + testImgData[x, y, B] * current_matrix[2, 2];


                                                            // Diff Methods.
                                                            switch (diffMethod)
                                                            {
                                                                case DiffMethods.Method.ABSOLUTE:

                                                                    tmp_diff = DiffMethods.DiffRelative(testMatrixed, refImgData, x, y);
                                                                    break;
                                                                case DiffMethods.Method.SUPERRELATIVE:

                                                                    tmp_diff = DiffMethods.DiffSuperRelative(testMatrixed, refImgData, x, y);
                                                                    break;
                                                                default:
                                                                case DiffMethods.Method.RELATIVE:

                                                                    tmp_diff = DiffMethods.DiffAbsolute(testMatrixed, refImgData, x, y);
                                                                    break;
                                                            }
                                                            if (!tmp_diff.HasValue)
                                                            {
                                                                skipped++;
                                                                continue;
                                                            }
                                                            else
                                                            {

                                                                total_diff += tmp_diff.Value;
                                                                count_diff++;
                                                            }

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
                                                        update_matrix = true;
                                                        //progress.Report(new MatchReport(count.ToString("#,##0") + ", best average diff: " + best_average_diff.ToString(), false, current_matrix));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                progress.Report(new MatchReport(count.ToString("#,##0") + ", best average diff: " + best_average_diff.ToString(),false,update_matrix ? best_matrix : null)); // Can't put it further in or it won't update at all. Dunno why. It gets called, but doesn't update UI. PC freezes tho, maybe performance/priority problem?
                                update_matrix = false;
                            }
                        }
                    }
                }
                iteration++;

                // For each new iteration, the current best_matrix +- current stepsize will be used, and that range again divided by subdiv.
                // TODO: Idea: Make individual stepsize for each of the 9 matrix elements. Why? Because that way whenever the best determined setting is on the outermost of the 4 parts, 
                //       we can keep a higher border/buffer for the next iteration. In other words, whenever the best value settles on one of the outer values, more buffer will be kept around that border,
                //       or the stepsize will remain constant for that one, so that it can settle towards something more reasonable.
                matchRanges = new float[9, 2] {
                        { best_matrix[0,0]-stepSize, best_matrix[0,0]+stepSize},
                        { best_matrix[0,1]-stepSize, best_matrix[0,1]+stepSize},
                        { best_matrix[0,2]-stepSize, best_matrix[0,2]+stepSize},
                        { best_matrix[1,0]-stepSize, best_matrix[1,0]+stepSize},
                        { best_matrix[1,1]-stepSize, best_matrix[1,1]+stepSize},
                        { best_matrix[1,2]-stepSize, best_matrix[1,2]+stepSize},
                        { best_matrix[2,0]-stepSize, best_matrix[2,0]+stepSize},
                        { best_matrix[2,1]-stepSize, best_matrix[2,1]+stepSize},
                        { best_matrix[2,2]-stepSize, best_matrix[2,2]+stepSize}};

                stepSizeNext = stepSize * 2 / subdiv;
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

        private void BtnLoadCHA_Click(object sender, win.RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Photoshop RGB Channel Mixer Preset (.cha)|*.cha";

            float[,] PSmatrix = new float[3,3];
            int[] useless = new int[3]; // I think these are for CMYK
            int[] constant = new int[3];

            if (ofd.ShowDialog() == true)
            {
                string filename = ofd.FileName;
                using(FileStream fs = File.Open(filename, FileMode.Open))
                {

                    using (BeBinaryReader binReader = new BeBinaryReader(fs))
                    {
                        
                        uint version = binReader.ReadUInt16();
                        uint monochrome = binReader.ReadUInt16();
                        PSmatrix[0, 0] = binReader.ReadInt16() / 100f;
                        PSmatrix[0, 1] = binReader.ReadInt16() / 100f;
                        PSmatrix[0, 2] = binReader.ReadInt16() / 100f;
                        useless[0] = binReader.ReadInt16();
                        constant[0] = binReader.ReadInt16();
                        PSmatrix[1, 0] = binReader.ReadInt16() / 100f;
                        PSmatrix[1, 1] = binReader.ReadInt16() / 100f;
                        PSmatrix[1, 2] = binReader.ReadInt16() / 100f;
                        useless[1] = binReader.ReadInt16();
                        constant[1] = binReader.ReadInt16();
                        PSmatrix[2, 0] = binReader.ReadInt16() / 100f;
                        PSmatrix[2, 1] = binReader.ReadInt16() / 100f;
                        PSmatrix[2, 2] = binReader.ReadInt16() / 100f;
                        useless[2] = binReader.ReadInt16();
                        constant[2] = binReader.ReadInt16();

                    }
                }

                setStatus(Helpers.matrixToString<float>(PSmatrix));
                SetSlidersToMatrix(PSmatrix);
                RegradeImage(PSmatrix); 
            }

        }

        private float[,] slidersToMatrix()
        {
            return new float[3, 3]
            {
                {(float)slide_RtoR.Value,(float)slide_GtoR.Value,(float)slide_BtoR.Value},
                {(float)slide_RtoG.Value,(float)slide_GtoG.Value,(float)slide_BtoG.Value },
                {(float)slide_RtoB.Value,(float)slide_GtoB.Value,(float)slide_BtoB.Value }
            };
        }


        // TODO Add automatic scaling if the user wants it.
        private void BtnSaveCHA_Click(object sender, win.RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Photoshop RGB Channel Mixer Preset (.cha)|*.cha";

            float[,] sliderMatrix = slidersToMatrix();


            // Check if any values are outside Photoshops -2 to 2 scope. (PS won't read a file if it's outside the scope)
            for(int i = 0; i<3; i++)
            {
                for(int ii = 0; ii < 3; ii++)
                {
                    if (sliderMatrix[i,ii] < -2 || sliderMatrix[i, ii]  > 2)
                    {
                        if (win.MessageBox.Show("Photoshop's Channel Mixer only supports values between -2 and 2 (-200% to 200%). Your current settings exceed those values. If you continue, those values will be clipped. Continue?","Attention!", win.MessageBoxButton.YesNo) != win.MessageBoxResult.Yes)
                        {
                            return;
                        } else
                        {
                            // Do clip the values, go!
                            goto FuckBeingElegant; // I didn't want to do this, C#, but you leave me no other choice. In PHP I could have written break 2;
                        }
                    }
                }
            }


            FuckBeingElegant:

            //sfd.FileName = ;
            if (sfd.ShowDialog() == true)
            {
                string filename = sfd.FileName;
                using (FileStream fs = File.Open(filename, FileMode.Create))
                {

                    using (BeBinaryWriter binWriter = new BeBinaryWriter(fs))
                    {

                        binWriter.Write((short)1);//Version
                        binWriter.Write((short)0);//Monochrome
                        binWriter.Write((short)Math.Max(-200, Math.Min(200,sliderMatrix[0,0]*100)));
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[0,1]*100)));
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[0,2]*100)));
                        binWriter.Write((short)0); //Useless (CMYK?)
                        binWriter.Write((short)0); //Constant
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[1, 0] * 100)));
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[1, 1] * 100)));
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[1, 2] * 100)));
                        binWriter.Write((short)0); //Useless (CMYK?)
                        binWriter.Write((short)0); //Constant
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[2, 0] * 100)));
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[2, 1] * 100)));
                        binWriter.Write((short)Math.Max(-200, Math.Min(200, sliderMatrix[2, 2] * 100)));
                        binWriter.Write((short)0); //Useless (CMYK?)
                        binWriter.Write((short)0); //Constant

                        // No idea what the next values mean, but they are necessary apparently:
                        binWriter.Write((short)0);
                        binWriter.Write((short)0);
                        binWriter.Write((short)0);
                        binWriter.Write((short)100);
                        binWriter.Write((short)0);


                    }
                }
                setStatus("Slider values were written into " + filename + ": " + Helpers.matrixToString<float>(sliderMatrix));
            }

        }
    }
}
