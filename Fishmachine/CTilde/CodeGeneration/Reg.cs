using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration
{
    public enum Reg
    {
        EAX,
        EBX,
        ECX,
        EDX,

        EBP,
        ESP,
        EDI,
        ESI,

        XSC, // special register for interrupt number
        XR1,

        DB0, // Debug registers
        DB1,
        DB2,
        DB3,

        MAX_VALUE, // Max allocated register array length, registers below this are mapped on top of this

        AL,
        AX,
        AH,
        BH,
        BL,
        BX,
        CL,

        RFLAGS,

        ST0,
    }
}