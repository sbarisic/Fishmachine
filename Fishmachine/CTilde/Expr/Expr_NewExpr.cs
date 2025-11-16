using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_NewExpr : Expression
	{
		public Expression NewExpr;

		public Expr_NewExpr()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Keyword.@new);

			if (!Tok.Peek().Is(Symbol.Semicolon))
			{
				NewExpr = Expression.ParseExpression(Tok, Symbol.Semicolon);
			}

			if (NewExpr == null)
				Tok.NextToken().Assert(Symbol.Semicolon);

			return this;
		}

		public override string ToSourceStr()
		{
			return "new " + (NewExpr?.ToSourceStr() ?? ";");
		}
	}
}
