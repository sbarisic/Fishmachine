using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration
{
    public static class CodeGenUtils
    {
        public static String RegToString(Reg reg) => CGenState.RegToString(reg);


        public static Int32 RoundUp(Int32 value, Int32 alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }
    }
}
