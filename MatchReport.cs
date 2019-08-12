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
        public float[,] best_matrix = null;
        public float[,] matching_range = null; // 9,2 matrix containing min and max values for each part of the matrix, as it gets progressively more precise

        public MatchReport(string Amessage, bool Aerror = false, float[,] Abest_matrix = null, float[,] Amatching_range = null)
        {
            message = Amessage;
            error = Aerror;
            best_matrix = Abest_matrix;
            matching_range = Amatching_range;
        }
    }
}
