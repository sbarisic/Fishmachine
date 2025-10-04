using Driver;
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

		static void Main(string[] args)
		{
			// Compile
			string Src = "";

			if (File.Exists("test.c"))
			{
				Src = File.ReadAllText("test.c").Trim() + "\n";
			}

			Compiler compiler = Compiler.FromSource(Src);
			//Console.WriteLine(compiler.Assembly);

			if (File.Exists("out.asm"))
				File.Delete("out.asm");

			File.WriteAllText("out.asm", compiler.Assembly);

			if (File.Exists("out_asm.txt"))
				File.Delete("out_asm.txt");

			File.Copy("out.asm", "out_asm.txt");

			// Assemble
			byte[] Bytecode = null;
			uint KMainAddr = 0;

			if (File.Exists("out.asm"))
			{
				AssemblerState AsmState = new AssemblerState();

				Assembler Asm = new Assembler(0x1000);
				Asm.Assemble(AsmState, File.ReadAllText("out.asm"));

				//Asm.LoadOffset(0x1000);
				Bytecode = Asm.Link();

				KMainAddr = AsmState.GetSymbolOffset("kmain");
			}

			if (Bytecode != null)
				File.WriteAllBytes("bytecode.bin", Bytecode);

			// Setup VM, load program and run
			HookOutput();

			FishVM VM = new FishVM();
			VM.AllocateMemory(0x1000 * 2);
			//VM.LoadToMemory(Bytecode, 0x1000);
			VM.LoadToMemory(Bytecode, 0x1000);

			VM.Regs.Write(CodeGeneration.Reg.ESP, 0x2000);
			VM.Jump(KMainAddr);
			VM.Run();

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
