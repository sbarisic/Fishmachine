using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_CaseBlock : Expression
	{
		public Expression CaseExpression;
		public List<Expression> Body;

		public Expr_CaseBlock()
		{
			Body = new List<Expression>();
		}
		public override Expression Parse(Tokenizer Tok)
		{
			bool IsDefault = false;

			if (Tok.Peek().Is(Keyword.@case))
				Tok.NextToken().Assert(Keyword.@case);
			else if (Tok.Peek().Is(Keyword.@default))
			{
				Tok.NextToken().Assert(Keyword.@default);
				IsDefault = true;
			}
			else
				throw new Exception("Expected 'case' or 'default' keyword.");

			if (!IsDefault)
				CaseExpression = Expression.ParseExpression(Tok, Symbol.Colon);
			else
				Tok.NextToken().Assert(Symbol.Colon);


			while (!(Tok.Peek().Is(Keyword.@break) || Tok.Peek().Is(Keyword.@case) || Tok.Peek().Is(Keyword.@default)))
			{
				Expression E = Expression.ParseExpression(Tok, Symbol.Semicolon);
				Body.Add(E);
			}

			if (Tok.Peek().Is(Keyword.@break))
			{
				Tok.NextToken().Assert(Keyword.@break);
				Tok.NextToken().Assert(Symbol.Semicolon);
				Body.Add(new Expr_BreakExpr());
			}

			return this;
		}
		public override string ToSourceStr()
		{
			StringBuilder BodyStr = new StringBuilder();
			foreach (var E in Body)
			{
				BodyStr.AppendLine(E.ToSourceStr() + ";");
			}

			BodyStr.AppendLine("break;");

			return string.Format("case {0}: {1}", CaseExpression?.ToSourceStr() ?? "default", BodyStr.ToString());
		}
	}
}
