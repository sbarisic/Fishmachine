using Driver;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace Fishmachine
{
	static class Console
	{
		static FileStream FS;

		static void OpenWrite()
		{
			if (FS == null)
			{
				FS = File.OpenWrite("vm_out.txt");
			}
		}

		public static void Write(string Str)
		{
			System.Console.Write(Str);

			OpenWrite();
			FS.Write(Encoding.UTF8.GetBytes(Str));
			FS.Flush();
			//File.AppendAllText("vm_out.txt", Str);
		}

		public static void Write(string Fmt, params object[] Args)
		{
			Write(string.Format(Fmt, Args));
		}

		public static void WriteLine(string Fmt, params object[] Args)
		{
			Write(Fmt, Args);
			WriteLine();
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

		static void Compile(string CFile)
		{
			// Compile
			string Src = "";

			if (File.Exists(CFile))
			{
				Src = File.ReadAllText(CFile).Trim() + "\n";
			}

			Compiler compiler = Compiler.FromSource(Src);
			//Console.WriteLine(compiler.Assembly);

			string OutAsmName = Path.GetFileNameWithoutExtension(CFile) + ".asm";

			if (File.Exists(OutAsmName))
				File.Delete(OutAsmName);

			File.WriteAllText(OutAsmName, compiler.Assembly);

			/*if (File.Exists("out_asm.txt"))
				File.Delete("out_asm.txt");

			File.Copy("out.asm", "out_asm.txt");*/
		}

		static byte[] Assemble(AssemblerState AsmState, string[] AsmFiles, out uint KMainAddr)
		{
			// Assemble
			byte[] Bytecode = null;
			KMainAddr = 0;


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

			Bytecode = Asm.Link();
			KMainAddr = AsmState.GetSymbolOffset("kmain");

			if (Bytecode != null)
			{
				File.WriteAllText("out.asm", AllSrc);
				File.WriteAllBytes("bytecode.bin", Bytecode);
			}

			return Bytecode;
		}

		static void Main(string[] args)
		{
			Compile("stdfish.c");
			Compile("test.c");

			AssemblerState AsmState = new AssemblerState();
			AsmState.DefineToken("int_table", 0x100, true);

			byte[] Bytecode = Assemble(AsmState, new[] { "stdfish.asm", "test.asm" }, out uint KMainAddr);

			// Setup VM, load program and run
			HookOutput();

			Graphics Gfx = new Graphics();
			Gfx.Setup(640, 400, 3);
			Gfx.StartThread();

			/*foreach (char c in "Hello World!")
			{
				Gfx.Write(c);
			}*/

			FishSettings.DebugPrint = true;
			FishVM VM = new FishVM();
			VM.Gfx = Gfx;


			VM.AllocateMemory(0x10000);
			//VM.LoadToMemory(Bytecode, 0x1000);
			Console.Write("{0} bytes ", Bytecode.Length);
			VM.LoadToMemory(Bytecode, 0x1000);
			Console.WriteLine("loaded");

			VM.Regs.Write(CodeGeneration.Reg.ESP, 0x9000);
			VM.Jump(KMainAddr);


			FishException Ex = FishException.None;
			while (VM.Run(out Ex))
			{
				if (Gfx.MousePressed())
				{
					Console.WriteLine("Mouse!");
					VM.Interrupt(FishInterrupt.Int0);
				}
			}


			if (Ex != FishException.None)
				throw new Exception($"VM stopped with exception {Ex}");

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
