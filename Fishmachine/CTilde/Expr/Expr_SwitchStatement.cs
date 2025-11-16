using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_SwitchStatement : Expression
	{
		public Expression Exp1;
		public Expr_SwitchBlock Body;

		public Expr_SwitchStatement()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Symbol.LParen);
			Exp1 = Expression.ParseExpression(Tok, Symbol.RParen);

			Token PT = Tok.Peek();
			if (Tok.Peek().Is(Symbol.RParen))
				Tok.NextToken().Assert(Symbol.RParen);

			Tok.Peek().Assert(Symbol.LBrace);
			Body = new Expr_SwitchBlock().Parse<Expr_SwitchBlock>(Tok);

			return this;
		}

		public override string ToSourceStr()
		{
			StringBuilder BodyStr = new StringBuilder();
			foreach (var E in Body.Expressions)
			{
				BodyStr.AppendLine(E.ToSourceStr());
			}
			BodyStr.AppendFormat("switch ({0}) {{\n{1}\n}}", Exp1.ToSourceStr(), BodyStr.ToString());
			return BodyStr.ToString();
		}

		/*public override string ToSourceStr()
		{
			return string.Format("for ({0}; {1}; {2}) {3}", Exp1.ToSourceStr(), Exp2.ToSourceStr(), Exp3.ToSourceStr(), Body.ToSourceStr());
		}*/
	}
}
