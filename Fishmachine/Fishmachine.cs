using CodeGeneration;
using CTilde;
using CTilde.FishAsm;
using CTilde.Langs;
using Fishmachine.VM;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

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

	internal class Program
	{
		static void HookOutput()
		{
			if (File.Exists("vm_out.txt"))
				File.Delete("vm_out.txt");
		}

		/*static void Compile(string CFile)
		{
			// Compile
			string Src = "";

			if (File.Exists(CFile))
			{
				Src = File.ReadAllText(CFile).Trim() + "\n";
			}

			Compiler compiler = Compiler.FromSource(Src);

			string OutAsmName = Path.GetFileNameWithoutExtension(CFile) + ".asm";

			if (File.Exists(OutAsmName))
				File.Delete(OutAsmName);

			File.WriteAllText(OutAsmName, compiler.Assembly);
		}*/

		static byte[] Assemble(AssemblerState AsmState, string[] AsmFiles)
		{
			// Assemble
			byte[] Bytecode = null;
			Assembler Asm = new Assembler(0x1000);

			string AllSrc = "";

			foreach (var F in AsmFiles)
			{
				if (File.Exists(F))
				{
					string Src = File.ReadAllText(F).Trim();
					AllSrc += Src + "\n";
					Asm.Assemble(AsmState, Src);
				}
			}

			File.WriteAllText("out.asm", AllSrc);
			Bytecode = Asm.Link();

			if (Bytecode != null)
			{
				File.WriteAllBytes("bytecode.bin", Bytecode);
			}

			return Bytecode;
		}

		static void FormatPrint(FishVM VM)
		{
			System.Console.CursorLeft = 0;
			System.Console.CursorTop = 1;

			string Sep = "|--------------------------|";
			Console.WriteLine("|---- REGS ----------------|");

			uint Val = 0;
			string FmtStr = "";

			Reg[] Regs = Enum.GetValues<CodeGeneration.Reg>();
			for (int i = 0; i < Regs.Length; i++)
			{
				if (Regs[i] == Reg.MAX_VALUE)
					continue;

				Val = VM.Regs.Read(Regs[i]);
				FmtStr = string.Format("| {0} = 0x{1:X} ({1})", Regs[i], Val);

				if (FmtStr.Length < Sep.Length - 1)
					FmtStr = FmtStr + new string(' ', Sep.Length - FmtStr.Length - 1) + "|";


				Console.WriteLine(FmtStr);
			}

			Val = VM.Regs.IP;
			FmtStr = string.Format("| {0} = 0x{1:X} ({1})", "IP", Val);

			if (FmtStr.Length < Sep.Length - 1)
				FmtStr = FmtStr + new string(' ', Sep.Length - FmtStr.Length - 1) + "|";
			Console.WriteLine(FmtStr);
		}

		static void CTildeCompile(string SrcFile, string OutFile, bool Silent)
		{
			Tokenizer Tokenizer = new Tokenizer(SrcFile);
			Parser Parser = new Parser(Tokenizer);

			FishCompileState State = new FishCompileState();
			LangProvider Lng = new FishAsmProvider(State);
			Lng.Compile(Parser.Parse());

			string CtAsmSrc = Lng.CompileToSource();

			if (!Silent)
			{
				Console.WriteLine(OutFile + ":\n" + CtAsmSrc);
				Console.WriteLine();
			}
			File.WriteAllText(OutFile, CtAsmSrc);

			//string CommentsRemoved = string.Join('\n', CtAsmSrc.Split('\n').Where(L => !L.Trim().StartsWith("#")).ToArray());
			//File.WriteAllText("ct_nocomments.asm", CommentsRemoved);
		}

		static string CompileAndRun(string Src, string OutFile, bool Silent)
		{
			CTildeCompile(Src, OutFile, Silent);

			AssemblerState AsmState = new AssemblerState();
			//AsmState.DefineToken("int_table", 0x100, true);

			byte[] Bytecode = Assemble(AsmState, new[] { OutFile });

			AsmToken IntTableTok = AsmState.FindToken("int_table");
			AsmToken KMainTok = AsmState.FindToken("kmain");
			uint KMainAddr = KMainTok.Address;

			AsmToken[] Globals = AsmState.GetGlobalVariables();

			// Setup VM, load program and run

			Graphics Gfx = new Graphics();
			Gfx.Setup(640, 400, 3);
			Gfx.StartThread();

			/*foreach (char c in "Hello World!")
			{
				Gfx.Write(c);
			}*/

			//uint AA = AsmState.GetSymbolOffset("input_array");
			//uint BB = AsmState.GetSymbolOffset("input_length");
			//uint CC = AsmState.GetSymbolOffset("input_count");

			FishVM VM = new FishVM();
			VM.IntTableAddr = IntTableTok.Address;
			VM.Gfx = Gfx;

			foreach (var G in Globals)
			{
				VM.DefineSymbol(G.Name, G.Address);
			}

			VM.AllocateMemory(0x30000);
			VM.SetMemMgrPointer(0x30000 - 1);

			//VM.LoadToMemory(Bytecode, 0x1000);
			if (!Silent)
				Console.Write("{0} (0x{0:X}) bytes ", Bytecode.Length);

			VM.LoadToMemory(Bytecode, 0x1000);

			if (!Silent)
				Console.WriteLine("loaded @ 0x{0:X}", 0x1000);

			uint ESPLoc = 0x20000;
			VM.Regs.Write(CodeGeneration.Reg.ESP, ESPLoc);
			VM.Regs.Write(CodeGeneration.Reg.EBP, ESPLoc);
			//VM.Regs.Write(CodeGeneration.Reg.EBP, 0x20000);

			if (!Silent)
				Console.WriteLine("Jumping to kmain @ 0x{0:X}", KMainAddr);
			VM.Jump(KMainAddr);

			FishException Ex = FishException.None;
			while (VM.Run(out Ex) && Gfx.IsWindowOpen())
			{
				bool Interrupted = false;

				while (!Interrupted && Gfx.IsWindowOpen())
				{
					if (Gfx.MousePressed())
					{
						if (!Silent)
							Console.WriteLine("Mouse!");

						VM.Interrupt(FishInterrupt.Int0);
						//VM.Interrupt(FishInterrupt.Int2_KeyboardChar, Encoding.ASCII.GetBytes(new[] { 'M' })[0]);
						Interrupted = true;
					}
					else if (Gfx.CharPressed(out uint Char))
					{
						byte B = Encoding.ASCII.GetBytes(new[] { (char)Char })[0];
						VM.Interrupt(FishInterrupt.Int2_KeyboardChar, B);
						Interrupted = true;
					}

					if (Ex != FishException.RequestWait)
						break;
				}

				/*else if (Gfx.KeyPressed(out uint Key))
				{
					VM.Interrupt(FishInterrupt.Int1_KeyboardKey, Key);
				}*/

				if (!Silent)
					if (FishSettings.FormatPrint)
						FormatPrint(VM);
			}

			if (!Silent)
				VM.PrintMem(ESPLoc, out Ex);

			if (Ex != FishException.None)
				throw new Exception($"VM stopped with exception {Ex}");

			Gfx.Stop();
			return VM.Out.ToString();
		}

		static void UnitTest(string SrcFile)
		{
			string OutFile = Path.GetFileNameWithoutExtension(SrcFile) + ".asm";
			string ExpOutFile = "data/tests/" + Path.GetFileNameWithoutExtension(SrcFile) + ".txt";
			string ExpectedOutput = File.ReadAllText(ExpOutFile).Replace("\r\n", "\n");

			Console.Write("Running test: ");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write(SrcFile);
			Console.ResetColor();
			Console.Write(" ... ");
			Console.Silent = true;
			string Out = CompileAndRun(SrcFile, OutFile, true);
			Console.Silent = false;

			bool Pass = Out == ExpectedOutput;

			if (Pass)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("PASS");
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("FAIL");
			}

			Console.ResetColor();
			Thread.Sleep(500);
		}

		static void Main(string[] args)
		{
			HookOutput();
			//Console.WriteLine("FishAsm.c:\n");
			//Console.WriteLine(File.ReadAllText("data/FishAsm.c"));
			//Console.WriteLine();

			//string OutStr = CompileAndRun("data/FishAsm.c", "FishAsm.asm");
			//OutStr = CompileAndRun("data/FishAsm.c", "FishAsm.asm");

			UnitTest("data/tests/Test1.c");
			UnitTest("data/tests/Test2.c");

			//Compile("stdfish.c");
			//Compile("test.c");


			//return;



			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
