﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IA
{
    public static class LongExtensions
    {
        public static ulong FromDbLong(this long l)
        {
            unchecked
            {
                return (ulong)l;
            }
        }
    }
}