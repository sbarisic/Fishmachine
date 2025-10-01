using System;
using System.Linq;
using System.Reflection;

namespace Driver
{
    class Program
    {
        static void Main(String[] args)
        {
            if (!args.Any())
            {
                string src = @"
int printf(char *, ...);
int main(int argc, char **argv) {
    printf(""%d"", argc);
    return 0;
}
";

                if (File.Exists("test.c"))
                {
                    src = File.ReadAllText("test.c").Trim() + "\n";
                }

                Compiler compiler = Compiler.FromSource(src);
                //Console.WriteLine(compiler.Assembly);

                if (File.Exists("out.asm"))
                    File.Delete("out.asm");

                File.WriteAllText("out.asm", compiler.Assembly);
            }
            else
            {
                Compiler compiler = Compiler.FromFile(args[0]);

                //Console.WriteLine(compiler.Assembly);
                if (File.Exists("out.asm"))
                    File.Delete("out.asm");

                File.WriteAllText("out.asm", compiler.Assembly);
            }
		}
    }
}
