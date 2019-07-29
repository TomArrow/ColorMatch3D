using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelMixMatcher
{
    class DiffMethods
    {


        const int R = 0;
        const int G = 1;
        const int B = 2;

        static float[] refColor = new float[3];
        static float[] testColor = new float[3];
        static float multiplier, multiplierRef;

        public static double? normalizeRelative(float[] testMatrixed, float[] refImgPixel)
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
                    multiplierRef = Math.Abs(255f / (float)refImgPixel[R]);
                    testColor[R] = 255;
                    testColor[G] = multiplier * testMatrixed[G];
                    testColor[B] = multiplier * testMatrixed[B];
                    refColor[R] = 255;
                    refColor[G] = multiplierRef * refImgPixel[G];
                    refColor[B] = multiplierRef * refImgPixel[B];
                }
                else if (Math.Abs(testMatrixed[G]) >= Math.Abs(testMatrixed[R]) && Math.Abs(testMatrixed[G]) >= Math.Abs(testMatrixed[B]))
                {
                    //if (testMatrixed[G] == 0 || refImgData[x, y, G] == 0) continue;
                    multiplier = Math.Abs(255f / (float)testMatrixed[G]);
                    multiplierRef = Math.Abs(255f / (float)refImgPixel[G]);
                    testColor[R] = multiplier * testMatrixed[R];
                    testColor[G] = 255;
                    testColor[B] = multiplier * testMatrixed[B];
                    refColor[R] = multiplierRef * refImgPixel[R];
                    refColor[G] = 255;
                    refColor[B] = multiplierRef * refImgPixel[B];
                }
                else if (Math.Abs(testMatrixed[B]) >= Math.Abs(testMatrixed[R]) && Math.Abs(testMatrixed[B]) >= Math.Abs(testMatrixed[G]))
                {
                    //if (testMatrixed[B] == 0 || refImgData[x, y, B] == 0) continue;
                    multiplier = Math.Abs(255f / (float)testMatrixed[B]);
                    multiplierRef = Math.Abs(255f / (float)refImgPixel[B]);
                    testColor[R] = multiplier * testMatrixed[R];
                    testColor[G] = multiplier * testMatrixed[G];
                    testColor[B] = 255;
                    refColor[R] = multiplierRef * refImgPixel[R];
                    refColor[G] = multiplierRef * refImgPixel[G];
                    refColor[B] = 255;
                }
                return Math.Abs(testColor[R] - refColor[R]) + Math.Abs(testColor[G] - refColor[G]) + Math.Abs(testColor[B] - refColor[B]);
            }
        }
    }
}
