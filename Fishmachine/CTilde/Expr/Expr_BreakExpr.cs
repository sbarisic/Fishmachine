using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_BreakExpr : Expression
	{
		public override IEnumerator<Expression> GetEnumerator()
		{
			yield break;
		}

		public Expr_BreakExpr()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Keyword.@break);
			Tok.NextToken().Assert(Symbol.Semicolon);
			return this;
		}

		public override string ToSourceStr()
		{
			return "break;";
		}
	}
}
