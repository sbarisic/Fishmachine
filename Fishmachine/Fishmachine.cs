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
	internal class Program
	{
		const bool PrintSrcAndAsm = false;

		static void HookOutput()
		{
			if (File.Exists("vm_out.txt"))
				File.Delete("vm_out.txt");
		}

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

		public static void FormatPrint(FishVM VM)
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

			if (!Silent && PrintSrcAndAsm)
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

			uint LoadAddress = 0x1000; // Program load address: 4096
			uint AllocatedVMMemSize = 0x10000; // 65535 bytes
			uint StackOffset = 0x10;
			uint StackAddr = AllocatedVMMemSize - StackOffset;
			uint StackSize = 1024 * 16; // 16 KB

			FishVM VM = new FishVM();
			VM.IntTableAddr = IntTableTok.Address;
			//VM.Gfx = Gfx;

			foreach (var G in Globals)
			{
				VM.DefineSymbol(G.Name, G.Address);
			}

			VM.RegisterSyscall(FishSyscall.PrintChar, Gfx, Syscall_PrintChar);
			VM.RegisterSyscall(FishSyscall.PrintNum, Gfx, Syscall_PrintNum);
			VM.RegisterSyscall(FishSyscall.PrintFloat, Gfx, Syscall_PrintFloat);
			VM.RegisterSyscall(FishSyscall.SoftwareInterrupt, Gfx, Syscall_SoftwareInterrupt);
			VM.RegisterSyscall(FishSyscall.Alloc, Gfx, Syscall_Alloc);
			VM.RegisterSyscall(FishSyscall.Cls, Gfx, Syscall_Cls);

			VM.AllocateMemory(AllocatedVMMemSize);
			VM.SetMemMgrPointer(AllocatedVMMemSize - StackSize - StackOffset);

			//VM.LoadToMemory(Bytecode, 0x1000);
			if (!Silent)
			{
				Console.WriteLine("RAM {0} bytes", AllocatedVMMemSize);
				Console.WriteLine("Stack at 0x{0:X}, size {1} bytes", StackAddr, StackSize);

				uint MemMgr = VM.GetMemMgrPointer(out int AllocBytes);
				Console.WriteLine("Memory manager at 0x{0:X}, allocated {1} bytes", MemMgr, AllocBytes);
				Console.Write("Program {0} bytes ", Bytecode.Length);
			}

			VM.LoadToMemory(Bytecode, LoadAddress, true);

			if (!Silent)
			{
				int MemPerc = (int)(((float)Bytecode.Length / AllocatedVMMemSize) * 100);
				Console.WriteLine("loaded at 0x{0:X} ({1} %)", LoadAddress, MemPerc);
			}

			VM.SetInitialStack(StackAddr, StackSize);
			//VM.Regs.Write(CodeGeneration.Reg.EBP, 0x20000);

			if (!Silent)
				Console.WriteLine("Jumping to kmain at 0x{0:X}", KMainAddr);

			VM.SetSupervisor(true);
			VM.Jump(KMainAddr);

			VM.RunStandalone(Silent);

			while (Gfx.IsWindowOpen())
			{
				if (Gfx.MousePressed())
				{
					if (!Silent)
						Console.WriteLine("Mouse!");

					VM.EnqueueInterrupt(FishInterrupt.Int0, null);
				}

				if (Gfx.CharPressed(out uint Char))
				{
					byte B = Encoding.ASCII.GetBytes(new[] { (char)Char })[0];
					VM.EnqueueInterrupt(FishInterrupt.Int2_KeyboardChar, new uint[] { B });
				}
			}


			if (!Silent)
				VM.PrintStack();

			Gfx.Stop();
			return VM.Out.ToString();
		}

		static void RunProgram(string SrcFile, bool IsUnitTest)
		{
			if (!IsUnitTest)
			{
				Console.Write("Running program: ");
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine(SrcFile);
				Console.ResetColor();


				Console.Silent = false;
				string ProgOut = CompileAndRun(SrcFile, "ct.asm", false);

				Console.WriteLine("Done!");
				Console.ReadLine();
				return;
			}

			string OutFile = "data/tests/" + Path.GetFileNameWithoutExtension(SrcFile) + ".asm";
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

			File.WriteAllText("data/tests/" + Path.GetFileNameWithoutExtension(SrcFile) + "_out.txt", Out);

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

		static void Syscall_PrintChar(object UserData, ref FishSyscallArgs Syscall)
		{
			Graphics Gfx = (Graphics)UserData;
			uint Arg1 = Syscall.Args[0];

			Console.WriteLine("PrintChar '{0}'", (char)Arg1);
			Syscall.VM.Out.Append((char)Arg1);

			if (FishSettings.DebugPrint)
			{
				Console.WriteLine("VM: 0x{0:X} = '{1}'", Arg1, (char)Arg1);
			}

			Gfx.Write((char)Arg1);
			//File.AppendAllText("vm_sys.txt", ((char)Arg1).ToString());
		}

		static void Syscall_PrintNum(object UserData, ref FishSyscallArgs Syscall)
		{
			Graphics Gfx = (Graphics)UserData;
			uint Arg1 = Syscall.Args[0];

			Console.WriteLine("PrintNum '{0}'", Arg1);
			Syscall.VM.Out.AppendFormat("{0}", Arg1);

			if (FishSettings.DebugPrint)
			{
				Console.WriteLine("VM: 0x{0:X} = '{1}'", Arg1, Arg1);
			}

			Gfx.Write(Arg1.ToString());
			//File.AppendAllText("vm_sys.txt", ((char)Arg1).ToString());
		}

		static void Syscall_PrintFloat(object UserData, ref FishSyscallArgs Syscall)
		{
			Graphics Gfx = (Graphics)UserData;
			uint Arg1 = Syscall.Args[0];

			float F = BitConverter.ToSingle(BitConverter.GetBytes(Arg1));
			string FStr = F.ToString("0.0##############");
			Console.WriteLine("PrintFloat '{0}'", FStr);
			Syscall.VM.Out.AppendFormat("{0}", F);

			if (FishSettings.DebugPrint)
			{
				Console.WriteLine("VM: EAX (float) = '{1}'", FStr);
			}

			Gfx.Write(FStr.ToString());
		}

		static void Syscall_SoftwareInterrupt(object UserData, ref FishSyscallArgs Syscall)
		{
			uint Arg1 = Syscall.Args[0];

			Console.WriteLine("Interrupt {0}!", Arg1);
			Syscall.VM.Interrupt((FishInterrupt)Arg1, ref Syscall.E);
		}

		static void Syscall_Alloc(object UserData, ref FishSyscallArgs Syscall)
		{
			uint Arg1 = Syscall.Args[0];
			bool Failed = false;

			uint BytesPtr = Arg1;
			uint Bytes = Syscall.VM.ReadUInt32FromStack(BytesPtr, ref Syscall.E);

			if (Syscall.E.Is(FishExcept.None))
			{
				uint AllocMem = Syscall.VM.MemMgrAlloc(Bytes);

				if (Bytes == 0)
					AllocMem = 0;

				if (AllocMem != 0)
				{
					FishMemPriv Priv = FishMemPriv.ReadWrite;

					if (Syscall.VM.Regs.IsSupervisor)
						Priv = Priv | FishMemPriv.Supervisor;

					Syscall.VM.ProtectMemory(AllocMem, Bytes, new FishMemProt(Priv, "alloc"));
				}

				Syscall.Return = new uint[] { AllocMem };
			}
			else
				Failed = true;

			if (Failed && FishSettings.DebugPrintMemory)
			{
				Console.WriteLine("FAIL - Alloc {0} bytes at 0x{1:X} ({1})", Arg1, 0);
			}
		}

		static void Syscall_Cls(object UserData, ref FishSyscallArgs Syscall)
		{
			Graphics Gfx = (Graphics)UserData;
			Gfx.Clear();
		}


		static void Main(string[] args)
		{
			HookOutput();
			//Console.WriteLine("FishAsm.c:\n");
			//Console.WriteLine(File.ReadAllText("data/FishAsm.c"));
			//Console.WriteLine();

			//string OutStr = CompileAndRun("data/FishAsm.c", "FishAsm.asm");
			//OutStr = CompileAndRun("data/FishAsm.c", "FishAsm.asm");

			RunProgram("data/FishAsm.c", false);
			//RunProgram("data/tests/Test4.c", false);

			/*
			RunProgram("data/tests/Test1.c", true);
			RunProgram("data/tests/Test2.c", true);
			RunProgram("data/tests/Test3.c", true);
			RunProgram("data/tests/Test4.c", true);
			//*/

			//RunProgram("data/tests/Test5.c", true);

			//Compile("stdfish.c");
			//Compile("test.c");


			//return;



			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
