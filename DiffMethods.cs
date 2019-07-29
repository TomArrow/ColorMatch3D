using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ChannelMixMatcher
{
    class DiffMethods
    {


        const int R = 0;
        const int G = 1;
        const int B = 2;

        // Methods
        public enum Method { RELATIVE, ABSOLUTE, SUPERRELATIVE};

        private static float[] refColor = new float[3];
        private static float[] testColor = new float[3];
        private static float multiplier, multiplierRef;

        // Normalize test img. We want only the absolute relations of colors.
        // Find channel with highest value, set it to 255, then scale otehr channels accordingly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? DiffRelative(float[] testMatrixed, float[,,] refImgData, int x, int y)
        {
            // Normalize test img. We want only the absolute relations of colors.
            // Find channel with highest value, set it to 255, then scale otehr channels accordingly
            // 
            if (testMatrixed[R] == 0 && testMatrixed[G] == 0 && testMatrixed[B] == 0)
            {
                //this is useless because we can't normalize it
                return null;
            }
            else
            {

                if (Math.Abs(testMatrixed[R]) >= Math.Abs(testMatrixed[G]) && Math.Abs(testMatrixed[R]) >= Math.Abs(testMatrixed[B]))
                {
                    //if (testMatrixed[R] == 0 || refImgData[x, y, R] == 0) continue;
                    multiplier = Math.Abs(255f / (float)testMatrixed[R]);
                    multiplierRef = Math.Abs(255f / (float)refImgData[x,y,R]);
                    testColor[R] = 255;
                    testColor[G] = multiplier * testMatrixed[G];
                    testColor[B] = multiplier * testMatrixed[B];
                    refColor[R] = 255;
                    refColor[G] = multiplierRef * refImgData[x,y,G];
                    refColor[B] = multiplierRef * refImgData[x,y,B];
                }
                else if (Math.Abs(testMatrixed[G]) >= Math.Abs(testMatrixed[R]) && Math.Abs(testMatrixed[G]) >= Math.Abs(testMatrixed[B]))
                {
                    //if (testMatrixed[G] == 0 || refImgData[x, y, G] == 0) continue;
                    multiplier = Math.Abs(255f / (float)testMatrixed[G]);
                    multiplierRef = Math.Abs(255f / (float)refImgData[x,y,G]);
                    testColor[R] = multiplier * testMatrixed[R];
                    testColor[G] = 255;
                    testColor[B] = multiplier * testMatrixed[B];
                    refColor[R] = multiplierRef * refImgData[x,y,R];
                    refColor[G] = 255;
                    refColor[B] = multiplierRef * refImgData[x,y,B];
                }
                else if (Math.Abs(testMatrixed[B]) >= Math.Abs(testMatrixed[R]) && Math.Abs(testMatrixed[B]) >= Math.Abs(testMatrixed[G]))
                {
                    //if (testMatrixed[B] == 0 || refImgData[x, y, B] == 0) continue;
                    multiplier = Math.Abs(255f / (float)testMatrixed[B]);
                    multiplierRef = Math.Abs(255f / (float)refImgData[x,y,B]);
                    testColor[R] = multiplier * testMatrixed[R];
                    testColor[G] = multiplier * testMatrixed[G];
                    testColor[B] = 255;
                    refColor[R] = multiplierRef * refImgData[x,y,R];
                    refColor[G] = multiplierRef * refImgData[x,y,G];
                    refColor[B] = 255;
                }
                return Math.Abs(testColor[R] - refColor[R]) + Math.Abs(testColor[G] - refColor[G]) + Math.Abs(testColor[B] - refColor[B]);
            }
        }

        // Just return the absolute difference between each channel summed together
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? DiffAbsolute(float[] testMatrixed, float[,,] refImgData, int x, int y)
        {
            return Math.Abs(testMatrixed[R] - refImgData[x, y, R]) + Math.Abs(testMatrixed[G] - refImgData[x, y, G]) + Math.Abs(testMatrixed[B] - refImgData[x, y, B]);
        }


        private static bool regradedAllSame, referenceAllSame;
        private static float[] testMatrixedNormalized, referenceNormalized;

        // Normalize test img to an extreme degree. We want only the absolute relations of colors, EXCLUDING saturation.
        // We take the highest and lowest value, normalize highest to 255 and lowest to 0 and then scale the one in between accordingly
        // As a result, like with relative, we can't use any pixel that has 3 identical channels
        // In fact, in a stricter implementation, we might not even allow any pixel that doesn't have 3 different channels, as otherwise it isn't clear what's being normalized.
        // TODO Actually do it. Currently it's just the normal relative code.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? DiffSuperRelative(float[] testMatrixed, float[,,] refImgData, int x, int y)
        {

            // If all values are equal
            regradedAllSame = Math.Abs(testMatrixed[R]) == Math.Abs(testMatrixed[G]) && Math.Abs(testMatrixed[G]) == Math.Abs(testMatrixed[B]);
            referenceAllSame = Math.Abs(refImgData[x, y, R]) == Math.Abs(refImgData[x, y, G]) && Math.Abs(refImgData[x, y, G]) == Math.Abs(refImgData[x, y, B]);
            if (regradedAllSame || referenceAllSame)
            {
                // If all values are equal in both regraded and reference, then the color tone is clearly the same, so diff is 0
                // However if that is not the case, it's difficult to make any kind of valid diff value as no idea how to normalize those against each other.
                // Idea tho: Maybe just normalize the one and then compute difference. Dunno.
                if(regradedAllSame && referenceAllSame)
                {
                    //return 0;
                    // Logically here we would return zero difference, however due to how the program operates, it leads to dumb results.
                    // For example in some initial or intermediate colormixer settings, every value will always be identical, for example when each channel's matrix is identical, like 100 100 100
                    // Problem is, this will of course usually return null as the reference image has normal shades.
                    // BUT if the target image has a single pixel that has identical values like 255 255 255 or 0 0 0, then it will return zero here and  then the average diff is zero for the
                    // entire image and that's obviously very wrong!
                    // So, if we go back to returning 0 here, we must stop returning null in other cases and instead make an actual diff calculation for all the normal pixels, and we do it by
                    // just setting all to 255 255 255 for the testMatrixed (taking Sign into account ofc) and then compare it against a normal supernormalization of the reference image.
                    return 0;
                } else if(regradedAllSame)
                {
                    // Skip this pixel.
                    testMatrixedNormalized = new float[3] { 255*Math.Sign(testMatrixed[R]), 255 * Math.Sign(testMatrixed[G]), 255 * Math.Sign(testMatrixed[B]) };
                    referenceNormalized = SuperNormalize(new float[3] { refImgData[x, y, R], refImgData[x, y, G], refImgData[x, y, B] });
                    return Math.Abs(testMatrixedNormalized[R] - referenceNormalized[R]) + Math.Abs(testMatrixedNormalized[G] - referenceNormalized[G]) + Math.Abs(testMatrixedNormalized[B] - referenceNormalized[B]);
                } else if (referenceAllSame) {


                    testMatrixedNormalized = SuperNormalize(testMatrixed);
                    referenceNormalized = new float[3] { 255 * Math.Sign(refImgData[x, y, R]), 255 * Math.Sign(refImgData[x, y, G]), 255 * Math.Sign(refImgData[x, y, B]) };
                    return Math.Abs(testMatrixedNormalized[R] - referenceNormalized[R]) + Math.Abs(testMatrixedNormalized[G] - referenceNormalized[G]) + Math.Abs(testMatrixedNormalized[B] - referenceNormalized[B]);
                } else
                {
                    // Weird, should never happen
                    return null;
                }
            }

            // Normalize regraded
            testMatrixedNormalized = SuperNormalize(testMatrixed);

            // Normalize test
            referenceNormalized = SuperNormalize(new float[3] { refImgData[x, y, R], refImgData[x, y, G], refImgData[x, y, B] });

            return Math.Abs(testMatrixedNormalized[R] - referenceNormalized[R]) + Math.Abs(testMatrixedNormalized[G] - referenceNormalized[G]) + Math.Abs(testMatrixedNormalized[B] - referenceNormalized[B]);
        }

        private static int biggestIndex, smallestIndex, leftoverIndex;
        private static double biggest;
        private static double smallest;
        private static double absChannel;
        private static float[] normalizedPixel = new float[3];
        private static float rangeMultiplier;

        // Expects float[3] for R G B
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float[] SuperNormalize(float[] pixel)
        {

            biggest = 0;
            smallest = double.PositiveInfinity;

            // R G B, 3 iterations for each channel once
            for(var i = 0; i < 3; i++)
            {
                absChannel = Math.Abs(pixel[i]);
                if (absChannel > biggest) {
                    biggestIndex = i;
                    biggest = absChannel;
                }
                if(absChannel < smallest)
                {
                    smallestIndex = i;
                    smallest = absChannel;
                }
            }

            // Find out which one is leftover
            // My own boolshit idea lol:
            // Works like this:
            // 00 xor 01 = 01 = !10
            // 00 xor 10 = 10 = !01
            // 10 xor 01 = 11 = !00
            // 
            // means:
            // 0 xor 1 = 1 = !2
            // 0 xor 2 = 2 = !1
            // 1 xor 2 = 3 = !0
            // 
            // But of course negating would turn around all the zeros to the left into 1s and you'd end up with giant numbers.
            // Therefore the unary & with 3 that limits the output to the lowest 2 bits.
            leftoverIndex = ~(biggestIndex ^ smallestIndex) & 3;

            // Genius idea by Jayy#6249 in the C# Discord, left in here for rememberance: 
            /* leftoverIndex = Math.Abs((biggestIndex + smallestIndex) - 3); */

            // Alternate first idea by Spixy#6134. also very good, but not quite as cool and fast I think
            // Leaving it in here as comment either way to make it clearer what's actually going on.
            /*
            switch (smallestIndex) {
                case 1:
                    leftoverIndex = 2;
                    break;
                case 2:
                    leftoverIndex = 1;
                    break;
                case 3:
                    leftoverIndex = 0;
                    break;
            }*/

            normalizedPixel[biggestIndex] = 255 * Math.Sign(pixel[biggestIndex]);
            normalizedPixel[smallestIndex] = 0;
            rangeMultiplier = 255/(Math.Abs(pixel[biggestIndex]) - Math.Abs(pixel[smallestIndex]));
            normalizedPixel[leftoverIndex] = rangeMultiplier * Math.Sign(pixel[leftoverIndex]) * (Math.Abs(pixel[leftoverIndex]) - Math.Abs(pixel[smallestIndex]));

            return (float[])normalizedPixel.Clone();
        }

    }
}
