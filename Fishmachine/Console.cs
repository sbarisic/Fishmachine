using CodeGeneration;
using Fishmachine.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine
{
	static class Console
	{
		static FileStream FS;
		public static bool Silent = false;

		static void OpenWrite()
		{
			if (FS == null)
			{
				if (File.Exists("vm_out.txt"))
					File.Delete("vm_out.txt");

				FS = File.OpenWrite("vm_out.txt");
			}
		}

		public static void Write(string Str)
		{
			if (!Silent)
				System.Console.Write(Str);

			OpenWrite();
			FS.Write(Encoding.UTF8.GetBytes(Str));
			FS.Flush();
			//File.AppendAllText("vm_out.txt", Str);
		}

		public static void Clear()
		{
		}

		public static void PrintInst(FishInst Inst, string Str)
		{
			ForegroundColor = ConsoleColor.DarkGray;
			Write(Inst.ToString());

			if (!string.IsNullOrEmpty(Str))
			{
				Write(" " + Str);
			}

			WriteLine();
			ResetColor();
		}

		public static void PrintInst(FishInst Inst)
		{
			PrintInst(Inst, "");
		}

		public static void PrintInst(FishInst Inst, int Offset, Reg R1, Reg R2)
		{
			PrintInst(Inst, string.Format("{0}(%{1}), %{2}", Offset, R1, R2));
		}

		public static void PrintInst(FishInst Inst, Reg R1, int Offset, Reg R2)
		{
			PrintInst(Inst, string.Format("%{0}, {1}(%{2})", R1, Offset, R2));
		}

		public static void PrintInst(FishInst Inst, uint A1)
		{
			PrintInst(Inst, string.Format("0x{0:X}", A1));
		}

		public static void PrintInst(FishInst Inst, uint A1, Reg R)
		{
			PrintInst(Inst, string.Format("0x{0:X}, %{1}", A1, R));
		}

		public static void PrintInst(FishInst Inst, int Offset, Reg R)
		{
			PrintInst(Inst, string.Format("{0}(%{1})", Offset, R));
		}

		public static void PrintInst(FishInst Inst, Reg R)
		{
			PrintInst(Inst, "%" + R.ToString());
		}

		public static void PrintInst(FishInst Inst, Reg R1, Reg R2)
		{
			PrintInst(Inst, string.Format("%{0}, %{1}", R1, R2));
		}

		public static void PrintReg(string Str)
		{
			ForegroundColor = ConsoleColor.DarkYellow;
			const char SplitChar = '=';

			if (Str.Contains(SplitChar))
			{
				string[] SplStr = Str.Split(SplitChar);
				Write(SplStr[0]);
				ForegroundColor = ConsoleColor.White;

				for (int i = 1; i < SplStr.Length; i++)
				{
					Write(SplitChar + "");
					Write(SplStr[i]);
				}
			}
			else
				Write(Str);
			ResetColor();
		}

		public static void PrintReg(Reg R, string Val)
		{
			PrintReg(string.Format("{0} = {1}", R, Val));
		}

		public static void PrintReg(Reg R, uint Val)
		{
			PrintReg(R, string.Format("0x{0:X8} hex, {0} dec; ", Val));
		}

		public static void Write(string Fmt, params object[] Args)
		{
			if (Args.Length == 0)
				Write(Fmt);
			else
				Write(string.Format(Fmt, Args));
		}

		public static void WriteLine(string Fmt, params object[] Args)
		{
			Write(Fmt, Args);
			WriteLine();
		}

		public static void WriteException(string Msg)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(Msg);
			Console.ResetColor();
		}

		public static void WriteException(string Msg, params object[] Args)
		{
			WriteException(string.Format(Msg, Args));
		}

		public static void WriteLine()
		{
			Write("\n");
		}

		public static string ReadLine()
		{
			return System.Console.ReadLine();
		}

		public static void ResetColor()
		{
			System.Console.ResetColor();
		}

		public static System.ConsoleColor ForegroundColor
		{
			get
			{
				return System.Console.ForegroundColor;
			}

			set
			{
				System.Console.ForegroundColor = value;
			}
		}
	}
}
