using System;
using System.Collections.Generic;

namespace ABT {
    public class Utils {

        public static Int32 RoundUp(Int32 value, Int32 alignment) {
            return (value + alignment - 1) & ~(alignment- 1);
        }

    }
}
