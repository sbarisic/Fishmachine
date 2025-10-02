namespace Fishmachine
{
	internal class Program
	{
		static void Main(string[] args)
		{
			byte[] Bytecode = null;
			uint KMainAddr = 0;

			if (File.Exists("out.asm"))
			{
				Assembler Asm = new Assembler();
				Asm.Assemble(File.ReadAllText("out.asm"));

				//Asm.LoadOffset(0x1000);
				Bytecode = Asm.Link();

				KMainAddr = Asm.GetSymbolOffset("kmain");
			}

			if (Bytecode != null)
				File.WriteAllBytes("bytecode.bin", Bytecode);

			FishVM VM = new FishVM();
			VM.AllocateMemory(0x1000 * 2);
			//VM.LoadToMemory(Bytecode, 0x1000);
			VM.LoadToMemory(Bytecode, 0);

			VM.Regs.Write(CodeGeneration.Reg.ESP, 0x2000);
			VM.Jump(KMainAddr);
			VM.Run();

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
