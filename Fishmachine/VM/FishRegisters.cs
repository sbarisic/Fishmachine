using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public struct FishRegisters
	{
		public uint IP;

		float[] ST;
		uint[] Regs;
		uint RFLAGS;

		bool GetRFLAGS(int Bit)
		{
			return (RFLAGS >> Bit & 0x1) == 1;
		}

		void SetRFLAGS(int Bit, bool Val)
		{
			if (Val)
			{
				RFLAGS = RFLAGS | 0x1u << Bit;
			}
			else
			{
				RFLAGS = RFLAGS & ~(0x1u << Bit);
			}
		}

		public bool IsZero
		{
			get
			{
				return GetRFLAGS(0);
			}
			set
			{
				SetRFLAGS(0, value);
			}
		}

		public bool Sign
		{
			get
			{
				return GetRFLAGS(1);
			}
			set
			{
				SetRFLAGS(1, value);
			}
		}

		public bool LessThan
		{
			get
			{
				return GetRFLAGS(2);
			}
			set
			{
				SetRFLAGS(2, value);
			}
		}

		public bool Equal
		{
			get
			{
				return GetRFLAGS(3);
			}
			set
			{
				SetRFLAGS(3, value);
			}
		}

		public bool GreaterThan
		{
			get
			{
				return GetRFLAGS(4);
			}
			set
			{
				SetRFLAGS(4, value);
			}
		}


		public bool IntEnabled
		{
			get
			{
				return GetRFLAGS(5);
			}
			set
			{
				SetRFLAGS(5, value);
			}
		}


		public FishRegisters()
		{
			Regs = new uint[(int)Reg.MAX_VALUE];
			ST = new float[8];
			IntEnabled = true;
		}

		public void FpuPush(float Val)
		{
			if (FishSettings.DebugPrint || FishSettings.DebugPrintFloats)
			{
				Console.ForegroundColor = ConsoleColor.DarkBlue;
				Console.WriteLine("FPU Push {0}", Val);
				Console.ResetColor();
			}

			for (int i = ST.Length - 1; i >= 1; i--)
			{
				ST[i] = ST[i - 1];
			}

			ST[0] = Val;
		}

		public float FpuPop()
		{
			float Val = ST[0];

			for (int i = 1; i < ST.Length; i++)
			{
				ST[i - 1] = ST[i];
			}

			if (FishSettings.DebugPrint || FishSettings.DebugPrintFloats)
			{
				Console.ForegroundColor = ConsoleColor.DarkBlue;
				Console.WriteLine("FPU Pop {0}", Val);
				Console.ResetColor();
			}

			return Val;
		}

		public float FpuPeek()
		{
			if (FishSettings.DebugPrint || FishSettings.DebugPrintFloats)
			{
				Console.ForegroundColor = ConsoleColor.DarkBlue;
				Console.WriteLine("FPU Peek {0}", ST[0]);
				Console.ResetColor();
			}

			return ST[0];
		}

		public uint Read(Reg Reg)
		{
			uint Ret = 0;

			switch (Reg)
			{
				case Reg.AL:
					Ret = Read(Reg.EAX) & 0xFF;
					break;

				case Reg.AH:
					Ret = (Read(Reg.EAX) >> 8) & 0xFF;
					break;

				case Reg.BH:
					Ret = (Read(Reg.EBX) >> 8) & 0xFF;
					break;

				case Reg.AX:
					Ret = Read(Reg.EAX) & 0xFFFF;
					break;

				case Reg.BL:
					Ret = Read(Reg.EBX) & 0xFF;
					break;

				case Reg.BX:
					Ret = Read(Reg.EBX) & 0xFFFF;
					break;

				case Reg.CL:
					Ret = Read(Reg.ECX) & 0xFF;
					break;

				case Reg.ST0:
					Ret = (uint)ST[0];
					break;

				case Reg.RFLAGS:
					Ret = RFLAGS;
					break;

				default:
					Ret = Regs[(int)Reg];
					break;
			}

			if (FishSettings.DebugPrint)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(": Read {0} = 0x{1:X} hex; {1} dec", Reg, Ret);
				Console.ResetColor();
			}

			return Ret;
		}

		public void Write(Reg Reg, uint Val)
		{
			switch (Reg)
			{
				case Reg.AL:
					Write(Reg.EAX, Val & 0xFF);
					return;

				case Reg.AH:
					Write(Reg.EAX, (Read(Reg.EAX) & 0xFFFF00FF) | ((Val & 0xFF) << 8));
					break;

				case Reg.BH:
					Write(Reg.EBX, (Read(Reg.EBX) & 0xFFFF00FF) | ((Val & 0xFF) << 8));
					break;

				case Reg.AX:
					Write(Reg.EAX, Val & 0xFFFF);
					return;

				case Reg.BL:
					Write(Reg.EBX, Val & 0xFF);
					return;

				case Reg.BX:
					Write(Reg.EBX, Val & 0xFFFF);
					return;

				case Reg.CL:
					Write(Reg.ECX, Val & 0xFF);
					return;

				case Reg.ST0:
					ST[0] = Val;
					return;

				case Reg.RFLAGS:
					RFLAGS = Val;

					if (IntEnabled == false)
						Debugger.Break();

					return;

				default:
					break;
			}

			if (FishSettings.DebugPrint || FishSettings.DebugRegisterWrite)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(": Write {0} = 0x{1:X} hex; {1} dec; new value 0x{2:X} hex; {2} dec;", Reg, Regs[(int)Reg], Val);
				Console.ResetColor();
			}

			Regs[(int)Reg] = Val;
		}


		public void PrintAll()
		{
			if (!FishSettings.DebugPrintRegisters)
				return;

			Reg[] RegsEnum = Enum.GetValues<Reg>().ToArray();
			int c = 1;

			Console.PrintReg(string.Format("EIP 0x{0:X} hex, {0} dec; ", IP));
			Console.PrintReg(string.Format("RFLAGS 0x{0:X4} hex, {0} dec; ", RFLAGS));
			Console.WriteLine();
			Console.PrintReg(string.Format("IsZero {0}; Sign {1}; LessThan {2}; Equal {3}; GreaterThan {4}; IntEnabled {5}", IsZero ? 1 : 0, Sign ? 1 : 0, LessThan ? 1 : 0, Equal ? 1 : 0, GreaterThan ? 1 : 0, IntEnabled ? 1 : 0));
			Console.WriteLine();

			foreach (var R in RegsEnum)
			{
				if (R == Reg.MAX_VALUE)
					continue;

				//Console.Write("{0} = {1:X4} ", R, this.Regs.Read(R));
				Console.PrintReg(R, Read(R));

				if (c++ % 4 == 0)
					Console.WriteLine();
			}
			Console.WriteLine();

			if (FishSettings.DebugPrintFloats)
			{
				c = 1;
				for (int i = 0; i < ST.Length; i++)
				{
					Console.PrintReg(string.Format("ST[{0}] {1}; ", i, ST[i]));
					if (c++ % 4 == 0)
						Console.WriteLine();
				}
			}

			Console.WriteLine();
		}
	}

}
