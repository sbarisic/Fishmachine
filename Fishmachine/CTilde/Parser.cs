using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CTilde.Expr;
using Fishmachine.CTilde;

namespace CTilde
{
	public class Parser
	{
		Tokenizer Tokenizer;

		public Parser(Tokenizer Tokenizer)
		{
			this.Tokenizer = Tokenizer;
		}

		public Expression Parse()
		{
			try
			{
				return new Expr_Module().Parse(Tokenizer);
			}
			catch (ExprException E)
			{
				Console.WriteLine(E);
				throw;
			}

		}
	}
}
