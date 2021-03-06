﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using win = System.Windows;
using System.Drawing;
using media = System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Threading;
using System.Numerics;
using Win = System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Text;

namespace ColorMatch3D
{

    struct FloatColor
    {
        public Vector3 color;
        //public float R, G, B;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : win.Window
    {
        
        public MainWindow()
        {
            InitializeComponent();


            //win.Forms.MessageBox.Show(Helpers.CIELChabTosRGB((Helpers.sRGBToCIELChab(new Vector3 { X=128, Y=128, Z=128 }))).ToString());
            //Status_txt.Text = "SIMD: " + Vector.IsHardwareAccelerated.ToString();
        }

        private void readGUISettings(string nameOfElement = "")
        {


            try {

                switch (nameOfElement)
                {
                    case "batchTestSuffix_text":
                        batchTestSuffix = batchTestSuffix_text.Text.Trim();
                        break;
                    case "batchReferenceSuffix_text":
                        batchReferenceSuffix = batchReferenceSuffix_text.Text.Trim();
                        break;


                    case "lowpassEqualizeBlurRadius_Text":
                        lowPassEqualizeBlurRadius = float.Parse(lowpassEqualizeBlurRadius_Text.Text);
                        break;
                    case "lowPassPercentileSubdivisions_Text":
                        lowPass1DRegradePercentileSubdivisions = int.Parse(lowPassPercentileSubdivisions_Text.Text);
                        break;
                    case "lowPassSmoothingIntensity_Text":
                        lowPass1DRegradeSmoothingIntensity = float.Parse(lowPassSmoothingIntensity_Text.Text);
                        break;
                    case "lowPassHistoMatchSmoothRadius_Text":
                        lowPass1DRegradeSmoothingRadius = int.Parse(lowPassHistoMatchSmoothRadius_Text.Text);
                        break;


                    case "boxBlur3dRadius_text":
                        postMatchSmoothing3DBoxBlurRadius = int.Parse(boxBlur3dRadius_text.Text);
                        break;
                    case "boxBlur3dProtectLuminance_text":
                        postMatchSmoothing3DBoxBlurLuminanceProtection = float.Parse(boxBlur3dProtectLuminance_text.Text);
                        break;
                    case "boxBlur3dStrength_text":
                        postMatchSmoothing3DBoxBlurStrength = float.Parse(boxBlur3dStrength_text.Text);
                        break;
                    case "boxBlur3dDisabled_radio":
                        postMatchSmoothing = PostMatchSmoothing.NONE;
                        break;
                    case "boxBlur3dActive_radio":
                        postMatchSmoothing = PostMatchSmoothing.BOXBLUR3D;
                        break;

                    case "lowPassHistoMatchYes_radio":
                        lowPass1DRegrade = LowPass1DRegrade.HISTOGRAM;
                        break;
                    case "lowPassHistoMatchNo_radio":
                        lowPass1DRegrade = LowPass1DRegrade.NONE;
                        break;
                    case "lowPassMatchNone_radio":
                        lowPassMatching = LowPassMatching.NONE;
                        break;
                    case "lowPassMatchReferenceToSource_radio":
                        lowPassMatching = LowPassMatching.REFERENCETOTEST;
                        break;
                    case "useSRGBAggrSpace_radio":
                        aggregateColorSpace = AggregateColorSpace.SRGB;
                        break;
                    case "useSRGBLinearAggrSpace_radio":
                        aggregateColorSpace = AggregateColorSpace.SRGBLINEAR;
                        break;
                    case "useXYZAggrSpace_radio":
                        aggregateColorSpace = AggregateColorSpace.XYZ;
                        break;
                    case "useCIELabAggrSpace_radio":
                        aggregateColorSpace = AggregateColorSpace.CIELAB;
                        break;
                    case "useCIELChabAggrSpace_radio":
                        aggregateColorSpace = AggregateColorSpace.CIELCHAB;
                        break;
                    case "aggrAbsolute_radio":
                        aggregateWhat = AggregateVariable.ABSOLUTE;
                        break;
                    case "aggrVector_radio":
                        aggregateWhat = AggregateVariable.VECTOR;
                        break;
                    case "interpNone_radio":
                        interpolationType = InterpolationType.NONE;
                        break;
                    case "interpSingleLinear_radio":
                        interpolationType = InterpolationType.SINGLELINEAR;
                        break;
                    case "interpDualLinear_radio":
                        interpolationType = InterpolationType.DUALLINEAR;
                        break;
                    default:
                        break;
                }


            }
            catch (Exception e) { //fuck, it's just called too early and the objects dont exist. whatever.

                win.MessageBox.Show("Caution. Invalid values entered. Will prevent proper setting of data. "+e.Message+" "+e.StackTrace);
            }
            //Win.MessageBox.Show(aggregateWhat.ToString());
        }


        string batchTestSuffix = "-test";
        string batchReferenceSuffix = "-ref";
        string batchTestFolder = ".";
        string batchReferenceFolder = ".";
        string batchOutputFolder = ".";
        bool batchTestFolderIsset = false, batchReferenceFolderIsset = false, batchOutputFolderIsset = false;
        bool batchTestFolderSoftIsset = false, batchReferenceFolderSoftIsset = false, batchOutputFolderSoftIsset = false;


        enum AggregateVariable { ABSOLUTE, VECTOR};
        AggregateVariable aggregateWhat = AggregateVariable.VECTOR;

        enum InterpolationType { NONE, SINGLELINEAR, DUALLINEAR};
        InterpolationType interpolationType = InterpolationType.SINGLELINEAR;

        enum AggregateColorSpace { SRGB, SRGBLINEAR, XYZ, CIELAB, CIELCHAB };
        AggregateColorSpace aggregateColorSpace = AggregateColorSpace.CIELAB;

        enum LowPassMatching { NONE, REFERENCETOTEST, TESTTOREFERENCE};
        LowPassMatching lowPassMatching = LowPassMatching.NONE;

        float lowPassEqualizeBlurRadius = 10f;

        enum LowPass1DRegrade { NONE, HISTOGRAM }
        LowPass1DRegrade lowPass1DRegrade = LowPass1DRegrade.HISTOGRAM;

        float lowPass1DRegradeSmoothingIntensity = 1f;
        int lowPass1DRegradeSmoothingRadius = 20;
        int lowPass1DRegradePercentileSubdivisions = 100;


        enum PostMatchSmoothing { NONE, BOXBLUR3D};
        PostMatchSmoothing postMatchSmoothing = PostMatchSmoothing.NONE;
        int postMatchSmoothing3DBoxBlurRadius = 2;
        float postMatchSmoothing3DBoxBlurStrength = 1.0f;
        float postMatchSmoothing3DBoxBlurLuminanceProtection = 1.0f;


        const int R = 0;
        const int G = 1;
        const int B = 2;

        private Bitmap testImage = null;
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

        private void DoColorMatchBatch_Click(object sender, win.RoutedEventArgs e)
        {
            DoColorMatchBatch();
        }

        private Task ColorMatchTask;
        private FloatColor[,,] globalCube = null;
        
        // ColorMatch caller
        private async void DoColorMatch()
        {

            if (testImage == null || referenceImage == null)
            {
                setStatus("Need both a test image and a reference image to match colors.",true);
                return;
            }


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
                    globalCube = update.cube;
                }

            });
            ColorMatchTask = Task.Run(() => DoColorMatch_Worker(progress,testImage,referenceImage,aggregateWhat));
            setStatus("Started...");
        }


        struct BatchTriple
        {
            public string testFile;
            public string refFile;
            public string LUTFile;
        }

        private async void DoColorMatchBatch()
        {


            string[] testfiles_unfiltered = Directory.GetFiles(batchTestFolder);
            string[] reffiles_unfiltered = Directory.GetFiles(batchReferenceFolder);

            List<BatchTriple> batchTriples = new List<BatchTriple>();
            int set_index = 0;

            BatchTriple tmpBatchTriple = new BatchTriple();


            // Find image pairs
            for(int k = 0; k < testfiles_unfiltered.Length; k++)
            {
                string baseFileName = Path.GetFileName(testfiles_unfiltered[k]);
                string fullFileName = Path.GetFullPath(testfiles_unfiltered[k]);

                string folder = Path.GetDirectoryName(fullFileName);
                string baseFileNameNoExt = Path.GetFileNameWithoutExtension(fullFileName);
                string extension = Path.GetExtension(fullFileName);

                bool is_testfile = baseFileNameNoExt.EndsWith(batchTestSuffix);

                if (is_testfile)
                {
                    string corresponding_basename = baseFileNameNoExt.Substring(0,baseFileNameNoExt.Length-batchTestSuffix.Length) + batchReferenceSuffix;

                    // Give meaningful name to cube file
                    string target_cubename = baseFileNameNoExt.Substring(0, baseFileNameNoExt.Length - batchTestSuffix.Length) +
                        (lowPassMatching == LowPassMatching.REFERENCETOTEST ?"-lowpassreftotest("+lowPassEqualizeBlurRadius+"px"+
                        (lowPass1DRegrade == LowPass1DRegrade.HISTOGRAM ? ",histogram1Dregrade("+lowPass1DRegradePercentileSubdivisions+"subdiv,"+lowPass1DRegradeSmoothingRadius+"smthrad,"+lowPass1DRegradeSmoothingIntensity+"smth)" : "") +
                            ")" : "")+
                        "-"+aggregateColorSpace.ToString()+
                        "-" + aggregateWhat.ToString()+
                        "-" + interpolationType.ToString()+
                        (postMatchSmoothing == PostMatchSmoothing.BOXBLUR3D ? "-postsmoothboxblur3d("+postMatchSmoothing3DBoxBlurRadius+"u,"+postMatchSmoothing3DBoxBlurStrength+"str,"+postMatchSmoothing3DBoxBlurLuminanceProtection+"lumprt)" : "") + 
                        ".cube";
                    target_cubename = batchOutputFolder.Trim(new char[] {  "/"[0] }) + Path.DirectorySeparatorChar + target_cubename;

                    for (int l = 0; l < reffiles_unfiltered.Length; l++)
                    {
                        string baseFileName_ref = Path.GetFileName(reffiles_unfiltered[l]);
                        string fullFileName_ref = Path.GetFullPath(reffiles_unfiltered[l]);

                        string folder_ref = Path.GetDirectoryName(fullFileName_ref);
                        string baseFileNameNoExt_ref = Path.GetFileNameWithoutExtension(fullFileName_ref);
                        string extension_ref = Path.GetExtension(fullFileName_ref);

                        bool is_corresponding_file = baseFileNameNoExt_ref == corresponding_basename;

                        if (is_corresponding_file)
                        {
                            tmpBatchTriple.testFile = fullFileName;
                            tmpBatchTriple.refFile = fullFileName_ref;
                            tmpBatchTriple.LUTFile = target_cubename;
                            batchTriples.Add(tmpBatchTriple);
                        }
                    }

                }
            }





            BatchProgress progressBox = new BatchProgress();
            progressBox.Show();


            for(int i = 0; i < batchTriples.Count; i++)
            {
                progressBox.AddOrUpdateProgressItem(i); // This isn't necessary but it makes sure that the order of the items in the batch progress is correct
            }


            Parallel.ForEach(batchTriples, (currentTriple,state,index) => {

                Task ColorMatchTask;
                Bitmap testImage = (Bitmap)Bitmap.FromFile(currentTriple.testFile);
                Bitmap referenceImage = (Bitmap)Bitmap.FromFile(currentTriple.refFile);

                var progress = new Progress<MatchReport>(update =>
                {
                    progressBox.AddOrUpdateProgressItem((int)index,update.message);
                    //setStatus(update.message, update.error);
                    if (update.cube != null)
                    {
                        MakeLUT(update.cube, currentTriple.LUTFile);
                        //globalCube = update.cube;
                    }

                });
                ColorMatchTask = Task.Run(() => DoColorMatch_Worker(progress, testImage, referenceImage, aggregateWhat));

            });



            setStatus("Started...");
        }



        private CancellationTokenSource _cancelRegrade = new CancellationTokenSource();


        //defunct
        /*
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
            
        //}*/
    

        // defunct
        /*
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
        }*/

        private enum DOWNSCALE { DEFAULT,NN}

        int outputValueCount = 32;


        const int RCORD = 3;
        const int GCORD = 4;
        const int BCORD = 5;


        

        struct AverageData
        {
            //public double totalR,totalG,totalB;
            public Vector3 color;
            public float divisor;
        };

        struct ColorPairData
        {
            //public byte R, G, B, RCORD, GCORD, BCORD;
            public Vector3 color, cord, cordConverted;
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
        private void DoColorMatch_Worker(IProgress<MatchReport> progress,Bitmap testImage, Bitmap referenceImage, AggregateVariable aggregateWhat)
        {

            var watch = System.Diagnostics.Stopwatch.StartNew();
            List<long> durations = new List<long>();

            
            //TODO Sanity checks: rangeMax msut be > rangemin etc.

            //Resize both images to resX,resY
            //TODO: Do proper algorithm that ignores blown highlights
            // TODO: Add "default linear" downscaler that corrects gamma before downscaling
            // TODO: add special downscaler that picks only useful pixels
            // TODO Add second special downscaler that isn't really a downscaler but one that picks most important colors including a slight average.
            Bitmap resizedReferenceImage;

            int resX = testImage.Width, resY = testImage.Height;

            float[,,] testImgData = new float[resX, resY, 3];
            float[,,] refImgData = new float[resX, resY, 3];

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


            durations.Add(watch.ElapsedMilliseconds);

            // Convert images into arrays for faster access (hopefully)
            // If lowpass matching is enabled, do that in the same go.
            if (lowPassMatching == LowPassMatching.REFERENCETOTEST)
            {

                float blurRadius = lowPassEqualizeBlurRadius;
                int blurSizeX = (int)(resX / blurRadius);
                int blurSizeY = (int)(resY / blurRadius);

                ByteImage testBitmapBlurred = Helpers.ToGreyscale(Helpers.BitmapToByteArray(Helpers.ResizeBitmapHQ(Helpers.ResizeBitmapHQ(testImage, blurSizeX, blurSizeY),resX, resY)));
                ByteImage referenceBitmapBlurred = Helpers.ToGreyscale(Helpers.BitmapToByteArray(Helpers.ResizeBitmapHQ(Helpers.ResizeBitmapHQ(referenceImage, blurSizeX, blurSizeY),resX, resY)));

                FloatImage testBitmapBlurred1DRegradeToreferenceBitmap = new FloatImage(new float[0], 0, 0, 0, System.Drawing.Imaging.PixelFormat.DontCare); // just initializing so VS doesn't moan and bitch
                if (lowPass1DRegrade == LowPass1DRegrade.HISTOGRAM)
                {
                    testBitmapBlurred1DRegradeToreferenceBitmap = Helpers.Regrade1DHistogram(testBitmapBlurred, referenceBitmapBlurred, lowPass1DRegradePercentileSubdivisions,lowPass1DRegradeSmoothingRadius,lowPass1DRegradeSmoothingIntensity);
                } else if (lowPass1DRegrade == LowPass1DRegrade.NONE)
                {
                    testBitmapBlurred1DRegradeToreferenceBitmap = FloatImage.FromByteImage(testBitmapBlurred);
                }

                /*progress.Report(new MatchReport("Blurring test image...."));
                ByteImage testBitmapBlurred = Helpers.BlurImage(testBitmap,50);
                progress.Report(new MatchReport("Blurring reference image...."));
                ByteImage referenceBitmapBlurred = Helpers.BlurImage(referenceBitmap, 50);*/

                Helpers.ByteArrayToBitmap(testBitmapBlurred).Save("test1-test.png");
                Helpers.ByteArrayToBitmap(referenceBitmapBlurred).Save("test2-ref.png");
                Helpers.ByteArrayToBitmap(testBitmapBlurred1DRegradeToreferenceBitmap.ToByteImage()).Save("test3-1dregrade.png");



                progress.Report(new MatchReport("Lowpass matching...."));
                for (var x = 0; x < resX; x++)
                {
                    for (var y = 0; y < resY; y++)
                    {
                        testImgData[x, y, B] = testBitmap[testBitmap.stride * y + x * 4];
                        testImgData[x, y, G] = testBitmap[testBitmap.stride * y + x * 4 + 1];
                        testImgData[x, y, R] = testBitmap[testBitmap.stride * y + x * 4 + 2];

                        // The -0.1 stuff is to avoid division by zero.
                        refImgData[x, y, B] = -0.1f+ (((float)referenceBitmap[referenceBitmap.stride * y + x * 4]+0.1f) / ((float)referenceBitmapBlurred[referenceBitmapBlurred.stride * y + x * 4] + 0.1f) * (testBitmapBlurred1DRegradeToreferenceBitmap[testBitmapBlurred1DRegradeToreferenceBitmap.stride * y + x * 4] + 0.1f));
                        refImgData[x, y, G] = -0.1f + (((float)referenceBitmap[referenceBitmap.stride * y + x * 4 + 1] + 0.1f) / ((float)referenceBitmapBlurred[referenceBitmapBlurred.stride * y + x * 4 + 1] + 0.1f) * (testBitmapBlurred1DRegradeToreferenceBitmap[testBitmapBlurred1DRegradeToreferenceBitmap.stride * y + x * 4 + 1] + 0.1f));
                        refImgData[x, y, R] = -0.1f + (((float)referenceBitmap[referenceBitmap.stride * y + x * 4 + 2] + 0.1f) / ((float)referenceBitmapBlurred[referenceBitmapBlurred.stride * y + x * 4 + 2] + 0.1f) * (testBitmapBlurred1DRegradeToreferenceBitmap[testBitmapBlurred1DRegradeToreferenceBitmap.stride * y + x * 4 + 2] + 0.1f));

                        /*debugBitmap.SetPixel(x, y, Color.FromArgb(testBitmap[testBitmap.stride * y + x * 3],
                            refImgData[x, y, G] = testBitmap[testBitmap.stride * y + x * 3 + 1],
                            refImgData[x, y, R] = testBitmap[testBitmap.stride * y + x * 3 + 2]));*/


                    }
                }
            }
            else if (lowPassMatching == LowPassMatching.NONE)
            {

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
            ColorPairData thisPointLinear  = new ColorPairData();
            int rAround, gAround, bAround;



            int pixelCount = resX * resY;
            ColorPairData[] collectCubeLinear = new ColorPairData[pixelCount];

            int collectCubeLinearIndex = 0;
            // Build full histogram

            for (var y = 0; y < resY; y++)
            {
                for (var x = 0; x < resX; x++)
                {


                    // set preCube (massive speedup later)
                    stepR = (byte)Math.Round(testImgData[x, y, R] / stepSize);
                    stepG = (byte)Math.Round(testImgData[x, y, G] / stepSize);
                    stepB = (byte)Math.Round(testImgData[x, y, B] / stepSize);
                    
                    thisPointLinear.cord.X = testImgData[x, y, R];
                    thisPointLinear.cord.Y = testImgData[x, y, G];
                    thisPointLinear.cord.Z = testImgData[x, y, B];

                    if (aggregateColorSpace == AggregateColorSpace.CIELAB)
                    {
                        /*
                        tmpRGB.R = refImgData[x, y, R];
                        tmpRGB.G = refImgData[x, y, G];
                        tmpRGB.B = refImgData[x, y, B];
                        tmpLab = tmpRGB.To<Lab>();

                        thisPointLinear.color.X = (float)tmpLab.L;
                        thisPointLinear.color.Y = (float)tmpLab.A;
                        thisPointLinear.color.Z = (float)tmpLab.B;


                        tmpRGB.R = thisPointLinear.cord.X;
                        tmpRGB.G = thisPointLinear.cord.Y;
                        tmpRGB.B = thisPointLinear.cord.Z;
                        tmpLab = tmpRGB.To<Lab>();
                        thisPointLinear.cordConverted.X = (float)tmpLab.L;
                        thisPointLinear.cordConverted.Y = (float)tmpLab.A;
                        thisPointLinear.cordConverted.Z = (float)tmpLab.B;
                        */

                        thisPointLinear.color.X = refImgData[x, y, R];
                        thisPointLinear.color.Y = refImgData[x, y, G];
                        thisPointLinear.color.Z = refImgData[x, y, B];
                        thisPointLinear.cordConverted = Helpers.sRGBToCIELab(thisPointLinear.cord);
                        thisPointLinear.color = Helpers.sRGBToCIELab(thisPointLinear.color);

                    } else if (aggregateColorSpace == AggregateColorSpace.CIELCHAB)
                    {
                        /*
                        tmpRGB.R = refImgData[x, y, R];
                        tmpRGB.G = refImgData[x, y, G];
                        tmpRGB.B = refImgData[x, y, B];
                        tmpLab = tmpRGB.To<Lab>();

                        thisPointLinear.color.X = (float)tmpLab.L;
                        thisPointLinear.color.Y = (float)tmpLab.A;
                        thisPointLinear.color.Z = (float)tmpLab.B;


                        tmpRGB.R = thisPointLinear.cord.X;
                        tmpRGB.G = thisPointLinear.cord.Y;
                        tmpRGB.B = thisPointLinear.cord.Z;
                        tmpLab = tmpRGB.To<Lab>();
                        thisPointLinear.cordConverted.X = (float)tmpLab.L;
                        thisPointLinear.cordConverted.Y = (float)tmpLab.A;
                        thisPointLinear.cordConverted.Z = (float)tmpLab.B;
                        */

                        thisPointLinear.color.X = refImgData[x, y, R];
                        thisPointLinear.color.Y = refImgData[x, y, G];
                        thisPointLinear.color.Z = refImgData[x, y, B];
                        thisPointLinear.cordConverted = Helpers.sRGBToCIELChab(thisPointLinear.cord);
                        thisPointLinear.color = Helpers.sRGBToCIELChab(thisPointLinear.color);

                    } else if(aggregateColorSpace == AggregateColorSpace.SRGB)
                    {

                        thisPointLinear.cordConverted = thisPointLinear.cord;
                        thisPointLinear.color.X = refImgData[x, y, R];
                        thisPointLinear.color.Y = refImgData[x, y, G];
                        thisPointLinear.color.Z = refImgData[x, y, B];
                    }

                    if(aggregateWhat == AggregateVariable.VECTOR)
                    {
                        thisPointLinear.color = thisPointLinear.color - thisPointLinear.cordConverted;
                    }

                    /*thisPointLinear.R = refImgData[x, y, R];
                    thisPointLinear.G = refImgData[x, y, G];
                    thisPointLinear.B = refImgData[x, y, B];
                    thisPointLinear.RCORD = testImgData[x, y, R];
                    thisPointLinear.GCORD = testImgData[x, y, G];
                    thisPointLinear.BCORD = testImgData[x, y, B];*/
                    thisPointLinear.nearestQuadrantR = stepR;
                    thisPointLinear.nearestQuadrantG = stepG;
                    thisPointLinear.nearestQuadrantB = stepB;
                    collectCubeLinear[collectCubeLinearIndex++] = thisPointLinear;
                    

                }

                progress.Report(new MatchReport("Building histogram [" + ((float)y*resX/pixelCount*100).ToString("#.##") + "%] "));
            }

            durations.Add(watch.ElapsedMilliseconds);

            // Build 32x32x32 cube data ( TODO later make the precision flexible)
            AverageData[,,] tmpCube = new AverageData[outputValueCount, outputValueCount, outputValueCount];
            UInt64 count = 0;
            float weight;
            float tmp1, tmp2, tmp3,
                tmp1sq,tmp2sq,tmp3sq;
            int redOctant, greenOctant, blueOctant;

            float sqrtOf3 = (float)Math.Sqrt(3);
            AverageData tmpAverageData = new AverageData();
            
            bool simpleWeight = false;
            float simpleWeightValue = sqrtOf3 - 1;
            
            Vector3 cordNormalized;
            

            // compiler explorer. check

            // This loop is currently the bottleneck. Edit: Is it still?
            foreach (ColorPairData collectCubeLinearHere in collectCubeLinear)
            {
                redOctant = collectCubeLinearHere.nearestQuadrantR;
                greenOctant = collectCubeLinearHere.nearestQuadrantG;
                blueOctant = collectCubeLinearHere.nearestQuadrantB;

                /*multiplyHelper.X = collectCubeLinearHere.RCORD;
                multiplyHelper.Y = collectCubeLinearHere.GCORD;
                multiplyHelper.Z = collectCubeLinearHere.BCORD;*/
                cordNormalized = collectCubeLinearHere.cord / stepSize;
                /*rCordNormalized = collectCubeLinearHere.RCORD / stepSize;
                gCordNormalized = collectCubeLinearHere.GCORD / stepSize;
                bCordNormalized = collectCubeLinearHere.BCORD / stepSize;*/

                count++;
                for (rAround = -1; rAround <= 1; rAround++)
                {
                    if (cordNormalized.X > redOctant && rAround == -1) continue; //major speed improvement but slight quality degradation
                    if (cordNormalized.X < redOctant  && rAround == 1) continue; //major speed improvement but slight quality degradation
                    trueStepR = Math.Max(0, Math.Min(steps, redOctant + rAround));
                    tmp1 = (trueStepR - cordNormalized.X);
                    tmp1sq = tmp1 * tmp1;

                    for (gAround = -1; gAround <= 1; gAround++)
                    {
                        if (cordNormalized.Y >  greenOctant && gAround == -1) continue; //major speed improvement but slight quality degradation
                        if (cordNormalized.Y < greenOctant  && gAround == 1) continue; //major speed improvement but slight quality degradation
                        trueStepG = Math.Max(0, Math.Min(steps, greenOctant + gAround));
                        tmp2 = (trueStepG - cordNormalized.Y);
                        tmp2sq = tmp2 * tmp2;

                        for (bAround = -1; bAround <= 1; bAround++)
                        {
                            if (cordNormalized.Z >  blueOctant && bAround == -1) continue; //major speed improvement but slight quality degradation
                            if (cordNormalized.Z < blueOctant && bAround == 1) continue; //major speed improvement but slight quality degradation
                            trueStepB = Math.Max(0, Math.Min(steps, blueOctant + bAround));
                            tmp3 = (trueStepB - cordNormalized.Z);
                            tmp3sq = tmp3 * tmp3;

                            if (simpleWeight)
                            {
                                weight = simpleWeightValue;
                                simpleWeight = false;
                            } else
                            {

                                // 5- Euklidian distance self-multiply
                                // can pretty safely skip the SQRT part actually, my experiments have shown that it makes pretty much zero visual difference
                                /*weight = Math.Max(0, sqrtOf3 - (float)Math.Sqrt(
                                    (tmp1 * tmp1
                                    + tmp2 * tmp2
                                    + tmp3 * tmp3)
                                    ));*/
                                weight = Math.Max(0, 3 - (
                                    (tmp1sq
                                    + tmp2sq
                                    + tmp3sq)
                                    ));
                            }
                            

                            // TODO MEDIAN METHOD + option to only accept places that have 3+ result colors + interpolation to prettify
                            tmpCube[trueStepR, trueStepG, trueStepB].color += collectCubeLinearHere.color * weight;
                            tmpCube[trueStepR, trueStepG, trueStepB].divisor += weight;
                            //tmpAverageData.valueR = (float)tmpAverageData.data.X / tmpAverageData.divisor;
                            //tmpAverageData.valueG = (float)tmpAverageData.data.Y / tmpAverageData.divisor;
                            //tmpAverageData.valueB = (float)tmpAverageData.data.Z / tmpAverageData.divisor;

                        }
                    }
                }

                if(count%200000 == 0)
                {

                    progress.Report(new MatchReport("Building cube [" + (((double)count/(pixelCount))*100).ToString("#.##") + "%] "));
                }
            }


            durations.Add(watch.ElapsedMilliseconds);



            FloatColor[,,] cube = new FloatColor[outputValueCount, outputValueCount, outputValueCount];
            FloatColor tmpFloatColor;

            tmpFloatColor.color = new Vector3(0, 0, 0);

            Vector3 NaNColor = new Vector3 {X=float.NaN,Y=float.NaN,Z=float.NaN };

            // transfer tmpCube to normal cube
            // May seem slow from the code but actually only takes about 1 ms, it's ridiculously fast.
            AverageData averageHelper;
            Vector3 absCoord = new Vector3(0, 0, 0);
            for (redOctant = 0; redOctant<outputValueCount; redOctant++)
            {
                absCoord.X = redOctant * stepSize;
                for (greenOctant = 0; greenOctant < outputValueCount; greenOctant++)
                {
                    absCoord.Y = greenOctant * stepSize;
                    for (blueOctant = 0; blueOctant < outputValueCount; blueOctant++)
                    {
                        absCoord.Z = blueOctant * stepSize;
                        tmpAverageData = tmpCube[redOctant, greenOctant, blueOctant];

                        if (tmpAverageData.divisor == 0)
                        {
                            // Do this only if you skipped the arounds earlier. Maybe make this a "fast algo" option.
                            // This part is pretty quick though, but it still doesn't eliminate artifacts completely.
                            // Also don't do this if interpolation is activated, as it will (hopefully) provide more reasonable values than this.
                            if(interpolationType == InterpolationType.NONE) {

                                averageHelper = new AverageData();
                                for (rAround = -1; rAround <= 1; rAround++)
                                {
                                    trueStepR = Math.Max(0, Math.Min(steps, redOctant + rAround));
                                    for (gAround = -1; gAround <= 1; gAround++)
                                    {
                                        trueStepG = Math.Max(0, Math.Min(steps, greenOctant + gAround));
                                        for (bAround = -1; bAround <= 1; bAround++)
                                        {
                                            trueStepB = Math.Max(0, Math.Min(steps, blueOctant + bAround));
                                            averageHelper.color += tmpCube[trueStepR, trueStepG, trueStepB].color;
                                            averageHelper.divisor += tmpCube[trueStepR, trueStepG, trueStepB].divisor;
                                        }
                                    }
                                }
                                if(aggregateWhat == AggregateVariable.ABSOLUTE)
                                {
                                    tmpFloatColor.color = averageHelper.color / averageHelper.divisor;
                                } else if(aggregateWhat == AggregateVariable.VECTOR)
                                {
                                    //tmpFloatColor.color = absCoord + (averageHelper.color / averageHelper.divisor); I had it like this and I guess it worked but the "back to absolute" seems wrong bc this is done later...
                                    tmpFloatColor.color = averageHelper.color / averageHelper.divisor;
                                }
                                cube[redOctant, greenOctant, blueOctant] = tmpFloatColor;
                            } else
                            {
                                cube[redOctant, greenOctant, blueOctant].color = NaNColor;
                            }
                        }
                        else
                        {
                            /*tmpFloatColor.color.X = (float)tmpAverageData.color.X / tmpAverageData.divisor;
                            tmpFloatColor.color.Y = (float)tmpAverageData.color.Y / tmpAverageData.divisor;
                            tmpFloatColor.color.Z = (float)tmpAverageData.color.Z / tmpAverageData.divisor;*/
                            /*
                            if (aggregateWhat == AggregateVariable.ABSOLUTE)
                            {

                                cube[redQuadrant, greenQuadrant, blueQuadrant].color = tmpAverageData.color / tmpAverageData.divisor;
                            }
                            else if (aggregateWhat == AggregateVariable.VECTOR)
                            {

                                cube[redQuadrant, greenQuadrant, blueQuadrant].color = absCoord + tmpAverageData.color / tmpAverageData.divisor;
                            }*/ // This whole thing comes later now (transfer to absolute)
                            cube[redOctant, greenOctant, blueOctant].color = tmpAverageData.color / tmpAverageData.divisor;
                        }

                    }
                }
            }

            durations.Add(watch.ElapsedMilliseconds);


            // Interpolation
            if (interpolationType == InterpolationType.DUALLINEAR || interpolationType == InterpolationType.SINGLELINEAR)
            {
                int unsolvedNaNs = 0;
                bool thisIsNaN = false;


                /* Possible directions for a hint: (2 dimensional example)
                 *  X   -  X   -   X
                 *  
                 *  -   X   X  X   -
                 *  
                 *  X   X   O   X   X 
                 *  
                 *  -   X   X   X   - 
                 *  
                 *  X   -   X   -   X
                 *  
                 *  Abstract explanation attempt: 
                 *  A hint is if either of these is true:
                 *  - Use any combination of -1 0 and 1 in the 3 dimensions.
                 *  - Take a second value that is the same vector, but times 2. 0 with stay 0, -1 will become -2 etc.
                 */

                // Prepare directions
                List<Vector3> directionsList = new List<Vector3>();
                Vector3 currentDirection = new Vector3();
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            currentDirection.X = x;
                            currentDirection.Y = y;
                            currentDirection.Z = z;
                            directionsList.Add(currentDirection);
                        }
                    }
                }
                Vector3[] directions = directionsList.ToArray();
                Vector3[] directionsX2 = new Vector3[directions.Length];
                for(int i=0;i<directions.Length;i++)
                {
                    directionsX2[i] = directions[i] * 2; // Add second item in same direction
                }

                bool[] hintsHere = new bool[directions.Length];
                int hintCountHere = 0;

                Vector3 currentLocation = new Vector3();
                Vector3 transposedLocation = new Vector3();
                Vector3 transposedLocationX2 = new Vector3();

                bool directionIsNaN = false;
                bool directionX2IsNaN = false;
                FloatColor cubeHere = new FloatColor();
                FloatColor cubeThere = new FloatColor();

                int hintsRequiredInFirstLoop = directions.Length; // How many hints (directions) are required during the first loop to calculate the correct value. The start value is arbitrary
                int hintsRequired = hintsRequiredInFirstLoop;
                int NaNsSolvedInThisLoop = 0;
                AverageData averageOfResolvedHints = new AverageData();
                Vector3 tmp;
                // THE ACTUAL LOOP
                // Loop until everything is solved
                do
                {
                    NaNsSolvedInThisLoop = 0;
                    unsolvedNaNs = 0;
                    // Go through all cells
                    for (currentLocation.X = 0; currentLocation.X < outputValueCount; currentLocation.X++)
                    {
                        for (currentLocation.Y = 0; currentLocation.Y < outputValueCount; currentLocation.Y++)
                        {
                            for (currentLocation.Z = 0; currentLocation.Z < outputValueCount; currentLocation.Z++)
                            {
                                // First check if the current cell is a NaN
                                cubeHere = cube[(int)currentLocation.X, (int)currentLocation.Y, (int)currentLocation.Z];
                                thisIsNaN = float.IsNaN(cubeHere.color.X) || float.IsNaN(cubeHere.color.Y) || float.IsNaN(cubeHere.color.Z);

                                if (thisIsNaN)
                                {
                                    hintCountHere = 0;

                                    // 1. Search for hints
                                    // Kind of a prefiltering.
                                    for (int i = 0; i < directions.Length; i++)
                                    {
                                        transposedLocation = currentLocation + directions[i];
                                        transposedLocationX2 = currentLocation + directionsX2[i];

                                        if (transposedLocationX2.X < 0 || transposedLocationX2.X >= outputValueCount || transposedLocationX2.Y < 0 || transposedLocationX2.Y >= outputValueCount || transposedLocationX2.Z < 0 || transposedLocationX2.Z >= outputValueCount)
                                        {
                                            hintsHere[i] = false;
                                            // doesn't work, outside bounds.
                                        } else
                                        {

                                            cubeThere = cube[(int)transposedLocation.X, (int)transposedLocation.Y, (int)transposedLocation.Z];
                                            directionIsNaN = float.IsNaN(cubeThere.color.X) || float.IsNaN(cubeThere.color.Y) || float.IsNaN(cubeThere.color.Z);
                                            cubeThere = cube[(int)transposedLocationX2.X, (int)transposedLocationX2.Y, (int)transposedLocationX2.Z];
                                            directionX2IsNaN = float.IsNaN(cubeThere.color.X) || float.IsNaN(cubeThere.color.Y) || float.IsNaN(cubeThere.color.Z);

                                            if (!directionIsNaN && (!directionX2IsNaN || interpolationType == InterpolationType.SINGLELINEAR))
                                            {
                                                hintsHere[i] = true;
                                                hintCountHere++;
                                            }
                                            else
                                            {
                                                hintsHere[i] = false;
                                            }
                                        }

                                    }

                                    // 2. ACTUAL SOLVING
                                    // Decide if should solve or not. Want to start out with the cells that have the most hints, hence the limit. Limit is lowered every time the current limit doesn't allow further interpolation.
                                    if(hintCountHere >= hintsRequired)
                                    {

                                        // ACTUAL SOLVING
                                        averageOfResolvedHints.color = new Vector3(0,0,0);
                                        averageOfResolvedHints.divisor = 0;

                                        for (int i = 0; i < directions.Length; i++)
                                        {
                                            if(hintsHere[i] == true)
                                            {
                                                transposedLocation = currentLocation + directions[i];
                                                tmp = cube[(int)transposedLocation.X, (int)transposedLocation.Y, (int)transposedLocation.Z].color;
                                                transposedLocationX2 = currentLocation + directionsX2[i];
                                                cubeThere = cube[(int)transposedLocationX2.X, (int)transposedLocationX2.Y, (int)transposedLocationX2.Z];
                                                /*
                                                 * Let's say we have this, one dimensionally:
                                                 * NaN 2 3
                                                 * 
                                                 * NaN is our unknown.
                                                 * We know it must be 1 according to our logic.
                                                 * 
                                                 * We take 3, subtract 2 and get 1. 
                                                 * Now we subtract 1 from 2 and get 1.
                                                 * 
                                                 */
                                                if (interpolationType == InterpolationType.SINGLELINEAR)
                                                {
                                                    averageOfResolvedHints.color += tmp;
                                                } else if(interpolationType == InterpolationType.DUALLINEAR)
                                                {
                                                    averageOfResolvedHints.color += cubeThere.color - (tmp - cubeThere.color);
                                                }
                                                averageOfResolvedHints.divisor += 1;
                                            }
                                        }
                                        cube[(int)currentLocation.X, (int)currentLocation.Y, (int)currentLocation.Z].color = averageOfResolvedHints.color / averageOfResolvedHints.divisor;
                                        NaNsSolvedInThisLoop++;
                                    } else
                                    {
                                        unsolvedNaNs++;
                                    }
                                }

                            }
                        }
                    }
                    if(NaNsSolvedInThisLoop == 0)
                    {
                        hintsRequired--;
                        if(hintsRequired <= 0)
                        {
                            // Impossible. TODO implement error
                            break;
                        }
                    } else
                    {
                        // Tried resetting or making higher the hintsrequired here after successful runs, but it slows down a LOT without notable improvement
                        //hintsRequired++;
                    }

                    progress.Report(new MatchReport("Interpolating, "+interpolationType+" algorithm [" + unsolvedNaNs + " unsolved remaining, "+hintsRequired+ " hints required] "));
                } while (unsolvedNaNs > 0);
            }


            durations.Add(watch.ElapsedMilliseconds);



            // Smoothing
            if (postMatchSmoothing == PostMatchSmoothing.BOXBLUR3D)
            {
                FloatColor[,,] smoothedCube = new FloatColor[outputValueCount, outputValueCount, outputValueCount];

                int boxBlurRadius = postMatchSmoothing3DBoxBlurRadius; // just to make it shorter to work with

                Vector3 tmpColor = new Vector3();
                for (redOctant = 0; redOctant < outputValueCount; redOctant++)
                {
                    //absCoord.X = redQuadrant * stepSize;
                    for (greenOctant = 0; greenOctant < outputValueCount; greenOctant++)
                    {
                        //absCoord.Y = greenQuadrant * stepSize;
                        for (blueOctant = 0; blueOctant < outputValueCount; blueOctant++)
                        {
                            //absCoord.Z = blueQuadrant * stepSize;

                            averageHelper = new AverageData();

                            //averageHelper.color += cube[redQuadrant, greenQuadrant, blueQuadrant].color;
                            //averageHelper.divisor += 1;

                            for (rAround = -boxBlurRadius; rAround <= boxBlurRadius; rAround++)
                            {
                                trueStepR = Math.Max(0, Math.Min(steps, redOctant + rAround));
                                for (gAround = -boxBlurRadius; gAround <= boxBlurRadius; gAround++)
                                {
                                    trueStepG = Math.Max(0, Math.Min(steps, greenOctant + gAround));
                                    for (bAround = -boxBlurRadius; bAround <= boxBlurRadius; bAround++)
                                    {
                                        trueStepB = Math.Max(0, Math.Min(steps, blueOctant + bAround));
                                        averageHelper.color += cube[trueStepR, trueStepG, trueStepB].color;
                                        averageHelper.divisor += 1;
                                    }
                                }
                            }

                            smoothedCube[redOctant, greenOctant, blueOctant].color = (1.0f- postMatchSmoothing3DBoxBlurStrength) * cube[redOctant, greenOctant, blueOctant].color + postMatchSmoothing3DBoxBlurStrength * (averageHelper.color / averageHelper.divisor);
                        }
                    }
                }

                // Protect avg. luminance
                //
                // NOTE: Currently this does not actually compute luminance but just tries to maintain an average of all 3 coordinates. Make a new option to distinguish between that and luminance.
                if(postMatchSmoothing3DBoxBlurLuminanceProtection > 0)
                {
                    AverageData[] averageLuminancesOfLayerPreSmoothing = new AverageData[outputValueCount];
                    AverageData[] averageLuminancesOfLayerPostSmoothing = new AverageData[outputValueCount];

                    int currentLayerTmp;
                    int currentLayer;

                    // Analyze average luminances
                    for (redOctant = 0; redOctant < outputValueCount; redOctant++)
                    {
                        //absCoord.X = redQuadrant * stepSize;
                        for (greenOctant = 0; greenOctant < outputValueCount; greenOctant++)
                        {
                            //absCoord.Y = greenQuadrant * stepSize;
                            currentLayerTmp = Math.Max(redOctant, greenOctant);
                            for (blueOctant = 0; blueOctant < outputValueCount; blueOctant++)
                            {
                                //absCoord.Z = blueQuadrant * stepSize;
                                // "Layer" we mean like a "ring" around 0. Except not really a ring, but a cube layer. In avg. luminance protection, we try to maintain the same average luminance in each cube layer as in the unsmoothed cube
                                // It may not be technically correct since luminance isn't equally affected by R,G and B, but it's a simplified approach that hopefully works decently.
                                currentLayer = Math.Max(currentLayerTmp, blueOctant);

                                averageLuminancesOfLayerPreSmoothing[currentLayer].color += cube[redOctant,greenOctant,blueOctant].color;
                                averageLuminancesOfLayerPreSmoothing[currentLayer].divisor += 1;
                                averageLuminancesOfLayerPostSmoothing[currentLayer].color += smoothedCube[redOctant,greenOctant,blueOctant].color;
                                averageLuminancesOfLayerPostSmoothing[currentLayer].divisor += 1;
                            }
                        }
                    }

                    Vector3[] differences = new Vector3[outputValueCount];

                    // Compute differences in average luminances.
                    for(int i = 0; i < outputValueCount; i++)
                    {
                        averageLuminancesOfLayerPreSmoothing[i].color /= averageLuminancesOfLayerPreSmoothing[i].divisor;
                        averageLuminancesOfLayerPostSmoothing[i].color /= averageLuminancesOfLayerPostSmoothing[i].divisor;

                        differences[i] = (averageLuminancesOfLayerPostSmoothing[i].color - averageLuminancesOfLayerPreSmoothing[i].color);
                    }

                    // Apply compensation
                    for (redOctant = 0; redOctant < outputValueCount; redOctant++)
                    {
                        //absCoord.X = redQuadrant * stepSize;
                        for (greenOctant = 0; greenOctant < outputValueCount; greenOctant++)
                        {
                            //absCoord.Y = greenQuadrant * stepSize;
                            currentLayerTmp = Math.Max(redOctant, greenOctant);
                            for (blueOctant = 0; blueOctant < outputValueCount; blueOctant++)
                            {
                                //absCoord.Z = blueQuadrant * stepSize;
                                // "Layer" we mean like a "ring" around 0. Except not really a ring, but a cube layer. In avg. luminance protection, we try to maintain the same average luminance in each cube layer as in the unsmoothed cube
                                // It may not be technically correct since luminance isn't equally affected by R,G and B, but it's a simplified approach that hopefully works decently.
                                currentLayer = Math.Max(currentLayerTmp, blueOctant);

                                smoothedCube[redOctant, greenOctant, blueOctant].color = ((smoothedCube[redOctant, greenOctant, blueOctant].color-differences[currentLayer]) * postMatchSmoothing3DBoxBlurLuminanceProtection)+ (smoothedCube[redOctant, greenOctant, blueOctant].color * (1-postMatchSmoothing3DBoxBlurLuminanceProtection));
                            }
                        }
                    }
                }

                cube = smoothedCube;
            }


            durations.Add(watch.ElapsedMilliseconds);


            // Convert Vector back to absolute
            if (aggregateWhat == AggregateVariable.VECTOR || aggregateColorSpace != AggregateColorSpace.SRGB)
            {
                Vector3 tmpColor = new Vector3();
                for (redOctant = 0; redOctant < outputValueCount; redOctant++)
                {
                    absCoord.X = redOctant * stepSize;
                    for (greenOctant = 0; greenOctant < outputValueCount; greenOctant++)
                    {
                        absCoord.Y = greenOctant * stepSize;
                        for (blueOctant = 0; blueOctant < outputValueCount; blueOctant++)
                        {
                            absCoord.Z = blueOctant * stepSize;
                            // TODO Find a way to not clip the result values with ColorMine.
                            if(aggregateColorSpace == AggregateColorSpace.CIELAB)
                            {
                                /*
                                tmpRGB.R = absCoord.X;
                                tmpRGB.G = absCoord.Y;
                                tmpRGB.B = absCoord.Z;
                                tmpLab = tmpRGB.To<Lab>();
                                tmpColor.X = (float)tmpLab.L;
                                tmpColor.Y = (float)tmpLab.A;
                                tmpColor.Z = (float)tmpLab.B;
                                */
                                tmpColor = Helpers.sRGBToCIELab(absCoord);
                                
                                tmpColor = tmpColor + cube[redOctant, greenOctant, blueOctant].color;
                                /*
                                tmpLab.L = tmpColor.X;
                                tmpLab.A = tmpColor.Y;
                                tmpLab.B = tmpColor.Z;
                                tmpRGB = tmpLab.To<Rgb>();

                                tmpColor.X = (float)tmpRGB.R;
                                tmpColor.Y = (float)tmpRGB.G;
                                tmpColor.Z = (float)tmpRGB.B;
                                */

                                tmpColor = Helpers.XYZtoRGB(Helpers.CIELabToXYZ(tmpColor));

                                cube[redOctant, greenOctant, blueOctant].color = tmpColor;

                            }
                            else if(aggregateColorSpace == AggregateColorSpace.CIELCHAB)
                            {
                                /*
                                tmpRGB.R = absCoord.X;
                                tmpRGB.G = absCoord.Y;
                                tmpRGB.B = absCoord.Z;
                                tmpLab = tmpRGB.To<Lab>();
                                tmpColor.X = (float)tmpLab.L;
                                tmpColor.Y = (float)tmpLab.A;
                                tmpColor.Z = (float)tmpLab.B;
                                */
                                tmpColor = Helpers.sRGBToCIELChab(absCoord);
                                
                                tmpColor = tmpColor + cube[redOctant, greenOctant, blueOctant].color;
                                /*
                                tmpLab.L = tmpColor.X;
                                tmpLab.A = tmpColor.Y;
                                tmpLab.B = tmpColor.Z;
                                tmpRGB = tmpLab.To<Rgb>();

                                tmpColor.X = (float)tmpRGB.R;
                                tmpColor.Y = (float)tmpRGB.G;
                                tmpColor.Z = (float)tmpRGB.B;
                                */

                                tmpColor = Helpers.CIELChabTosRGB(tmpColor);

                                cube[redOctant, greenOctant, blueOctant].color = tmpColor;

                            }
                            else if (aggregateColorSpace == AggregateColorSpace.SRGB)
                            {

                                cube[redOctant, greenOctant, blueOctant].color = absCoord + cube[redOctant, greenOctant, blueOctant].color;
                            }

                                
                        }
                    }
                }
                durations.Add(watch.ElapsedMilliseconds);
            }





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
            
            if(globalCube == null)
            {
                setStatus("no LUT was generated yet.",true);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CUBE 3D LUT (.cube)|*.cube";
            

            //sfd.FileName = ;
            if (sfd.ShowDialog() == true)
            {
                MakeLUT(globalCube, sfd.FileName);
            }
        }

        private void MakeLUT(FloatColor[,,] cube,string outputFileName)
        {


            int steps = outputValueCount - 1;
            float stepSize = 255 / steps;

            float red, green, blue;

            //string luttext = "LUT_3D_SIZE "+ outputValueCount + "\n";

            string numberFormat = "0." + (new string('#', 339));

            int floatStringLength = ((float)Math.PI).ToString().Length;

            StringBuilder sb = new StringBuilder("LUT_3D_SIZE " + outputValueCount + "\n", cube.Length * (floatStringLength + 1));

            for (int b = 0; b < outputValueCount; b++)
            {
                for (int g = 0; g < outputValueCount; g++)
                {
                    for (int r = 0; r < outputValueCount; r++)
                    {
                        /*red = float.IsNaN(cube[r, g, b][R]) ? r*stepSize/255 : cube[r, g, b][R]/255;
                        green = float.IsNaN(cube[r, g, b][G]) ? g * stepSize/255 : cube[r, g, b][G]/255;
                        blue = float.IsNaN(cube[r, g, b][B]) ? b * stepSize/255 : cube[r, g, b][B]/255;*/
                        red = float.IsNaN(cube[r, g, b].color.X) ? 0 : cube[r, g, b].color.X / 255;
                        green = float.IsNaN(cube[r, g, b].color.Y) ? 0 : cube[r, g, b].color.Y / 255;
                        blue = float.IsNaN(cube[r, g, b].color.Z) ? 0 : cube[r, g, b].color.Z / 255;
                        //luttext += red.ToString(CultureInfo.InvariantCulture) + " " + green.ToString(CultureInfo.InvariantCulture) + " " + blue.ToString(CultureInfo.InvariantCulture) + "\n";
                        sb.Append(red.ToString(numberFormat, CultureInfo.InvariantCulture).TrimEnd('0') + " " + green.ToString(numberFormat, CultureInfo.InvariantCulture).TrimEnd('0') + " " + blue.ToString(numberFormat, CultureInfo.InvariantCulture).TrimEnd('0') + "\n");
                    }
                }
            }

            File.WriteAllText(outputFileName, sb.ToString());
        }

        private void AggrVariable_radio_Checked(object sender, win.RoutedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }

        private void Interp_radio_Checked(object sender, win.RoutedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }

        private void AggrSpace_radio_Checked(object sender, win.RoutedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }

        private void LowPassMatch_radio_Checked(object sender, win.RoutedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }


        private void Lowpass_Text_TextChanged(object sender, win.Controls.TextChangedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }

        private void boxBlur3d_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }

        private void postMatchSmoothing_radio_Checked(object sender, win.RoutedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);

        }

        private void batch_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            win.FrameworkElement element = e.Source as win.FrameworkElement;
            readGUISettings(element.Name);
        }

        private void btnSelectTestFolder_Click(object sender, win.RoutedEventArgs e)
        {
            var fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            bool? result = fbd.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(fbd.SelectedPath) && Directory.Exists(fbd.SelectedPath))
            {
                batchTestFolder = fbd.SelectedPath;
                if (!batchReferenceFolderIsset)
                {
                    batchReferenceFolder = fbd.SelectedPath;
                    batchReferenceFolderSoftIsset = true;
                    btnSelectReferenceFolder.Background = media.Brushes.LightGreen;
                }
                if (!batchOutputFolderIsset)
                {
                    batchOutputFolder = fbd.SelectedPath;
                    batchOutputFolderSoftIsset = true;
                    btnSelectOutputFolder.Background = media.Brushes.LightGreen;
                }
                btnSelectTestFolder.Background = media.Brushes.Green;
                batchTestFolderIsset = true;
                btnDoColorMatchBatch.IsEnabled = true;
            }
        }
        private void btnSelectReferenceFolder_Click(object sender, win.RoutedEventArgs e)
        {
            var fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            bool? result = fbd.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(fbd.SelectedPath) && Directory.Exists(fbd.SelectedPath))
            {
                batchReferenceFolder = fbd.SelectedPath;

                if (!batchTestFolderIsset)
                {
                    batchTestFolder = fbd.SelectedPath;
                    batchTestFolderSoftIsset = true; 
                    btnSelectTestFolder.Background = media.Brushes.LightGreen;
                }
                if (!batchOutputFolderIsset && !batchOutputFolderSoftIsset)
                {
                    batchOutputFolder = fbd.SelectedPath;
                    batchOutputFolderSoftIsset = true;
                    btnSelectOutputFolder.Background = media.Brushes.LightGreen;
                }
                btnSelectReferenceFolder.Background = media.Brushes.Green;
                batchReferenceFolderIsset = true;
                btnDoColorMatchBatch.IsEnabled = true;
            }
        }

        private void btnSelectOutputFolder_Click(object sender, win.RoutedEventArgs e)
        {
            var fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            bool? result = fbd.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(fbd.SelectedPath) && Directory.Exists(fbd.SelectedPath))
            {
                batchOutputFolder = fbd.SelectedPath;
                if (!batchTestFolderIsset)
                {
                    batchTestFolder = fbd.SelectedPath;
                    batchTestFolderSoftIsset = true;
                    btnSelectTestFolder.Background = media.Brushes.LightGreen;
                }
                if (!batchReferenceFolderIsset)
                {
                    batchReferenceFolder = fbd.SelectedPath;
                    batchReferenceFolderSoftIsset = true;
                    btnSelectReferenceFolder.Background = media.Brushes.LightGreen;
                }
                btnSelectOutputFolder.Background = media.Brushes.Green;
                batchOutputFolderIsset = true;
                btnDoColorMatchBatch.IsEnabled = true;
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
