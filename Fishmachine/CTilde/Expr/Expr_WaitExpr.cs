using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_WaitExpr : Expression
	{
		public Expr_WaitExpr()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Keyword.wait);
			Tok.NextToken().Assert(Symbol.Semicolon);
			return this;
		}
	}
}
