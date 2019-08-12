using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorMatch3D
{
    class MatchReport
    {

        public string message = "";
        public bool error = false;
        public float[,,][] cube = null;

        public MatchReport(string Amessage, bool Aerror = false, float[,,][] cubeA = null)
        {
            message = Amessage;
            error = Aerror;
            cube = cubeA;
        }
    }
}
