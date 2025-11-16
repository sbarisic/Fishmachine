using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_ForStatement : Expression
	{
		public Expression Exp1;
		public Expression Exp2;
		public Expression Exp3;

		public Expr_Block Body;

		public override IEnumerator<Expression> GetEnumerator()
		{
			yield return Exp1;
			yield return Exp2;
			yield return Exp3;
			yield return Body;
		}

		public Expr_ForStatement()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Symbol.LParen);
			Exp1 = Expression.ParseStatement(Tok);

			Exp2 = Expression.ParseExpression(Tok, Symbol.Semicolon);
			Exp3 = Expression.ParseExpression(Tok, Symbol.Semicolon);

			Token PT = Tok.Peek();
			if (Tok.Peek().Is(Symbol.RParen))
				Tok.NextToken().Assert(Symbol.RParen);

			Tok.Peek().Assert(Symbol.LBrace);
			Body = new Expr_Block().Parse<Expr_Block>(Tok);

			return this;
		}

		public override string ToSourceStr()
		{
			return string.Format("for ({0}; {1}; {2}) {3}", Exp1.ToSourceStr(), Exp2.ToSourceStr(), Exp3.ToSourceStr(), Body.ToSourceStr());
		}
	}
}
