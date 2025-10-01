namespace Fishmachine
{
	internal class Program
	{
		static void Main(string[] args)
		{
			byte[] Bytecode = null;

			if (File.Exists("out.asm"))
			{
				Assembler Asm = new Assembler();
				Asm.Assemble(File.ReadAllText("out.asm"));

				Bytecode = Asm.Link();
			}

			if (Bytecode != null)
				File.WriteAllBytes("bytecode.bin", Bytecode);

			/*FishVM VM = new FishVM();
            VM.AllocateMemory(1024 * 64);
            VM.LoadToMemory(Input, 0x1000);

            VM.Jump(0x1000);
            VM.Run();

            Console.WriteLine("Hello, World!");*/
		}
	}
}
