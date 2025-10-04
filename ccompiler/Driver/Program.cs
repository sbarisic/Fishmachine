using System;
using System.Linq;
using System.Reflection;

namespace Driver
{
	class Program
	{
		static void Main(String[] args)
		{
			string CFileName = "test.c";
			string OutAsm = "out.asm";

			//string CFileName = "stdfish.c";
			//string OutAsm = "stdfish.asm";

			if (!args.Any())
			{
				string src = @"
int printf(char *, ...);
int main(int argc, char **argv) {
    printf(""%d"", argc);
    return 0;
}
";

				if (File.Exists(CFileName))
				{
					src = File.ReadAllText(CFileName).Trim() + "\n";
				}

				Compiler compiler = Compiler.FromSource(src);
				//Console.WriteLine(compiler.Assembly);

				if (File.Exists(OutAsm))
					File.Delete(OutAsm);

				File.WriteAllText(OutAsm, compiler.Assembly);
			}
			else
			{
				Compiler compiler = Compiler.FromFile(args[0]);

				//Console.WriteLine(compiler.Assembly);
				if (File.Exists(OutAsm))
					File.Delete(OutAsm);

				File.WriteAllText(OutAsm, compiler.Assembly);
			}
		}
	}
}
