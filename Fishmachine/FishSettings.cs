using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine
{
	static class FishSettings
	{
		public static bool DebugPrint = false;
		public static bool DebugPrintInstruction = false;
		public static bool DebugPrintMemory = false;
		public static bool FormatPrint = false;

		public static bool DebugPrintFloats = false;
		public static bool DebugPrintSyscall = true;
		public static bool DebugPrintRegisters = true;
		public static bool DebugRegisterWrite = true;
		public static bool DebugPrintIP = true;
	}

}
