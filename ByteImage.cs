﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorMatch3D
{
    class ByteImage
    {
        public byte[] imageData;
        public int stride;

        public ByteImage(byte[] imageDataA, int strideA)
        {
            imageData = imageDataA;
            stride = strideA;
        }

        public int Length
        {
            get { return imageData.Length; }
        }

        public byte this[int index]
        {
            get
            {
                return imageData[index];
            }

            set
            {
                imageData[index] = value;
            }
        }
    }
}