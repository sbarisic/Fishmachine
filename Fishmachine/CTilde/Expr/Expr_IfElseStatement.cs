using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_IfElseStatement : Expression
	{
		public Expression ConditionValue;
		public Expr_Block Body;
		public Expression ElseBody;

		public override IEnumerator<Expression> GetEnumerator()
		{
			yield return ConditionValue;
			yield return Body;

			if (ElseBody != null)
				yield return ElseBody;
		}

		public Expr_IfElseStatement()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Symbol.LParen);
			ConditionValue = Expression.ParseExpression(Tok, Symbol.RParen);

			Token PT = Tok.Peek();

			if (!Tok.Peek().Is(Symbol.LBrace))
			{
				Tok.NextToken().Assert(Symbol.RParen);
			}

			Tok.Peek().Assert(Symbol.LBrace);
			Body = new Expr_Block().Parse<Expr_Block>(Tok);

			if (Tok.Peek().Is(Keyword.@else))
			{
				Tok.NextToken().Assert(Keyword.@else);

				if (Tok.Peek().Is(Keyword.@if))
				{
					Tok.NextToken().Assert(Keyword.@if);
					ElseBody = new Expr_IfElseStatement().Parse(Tok);
				}
				else
				{
					ElseBody = new Expr_Block().Parse<Expr_Block>(Tok);
				}
			}

			return this;
		}

		public override string ToSourceStr()
		{
			if (ElseBody != null)
				return string.Format("if ({0}) {1} else {2}", ConditionValue.ToSourceStr(), Body.ToSourceStr(), ElseBody.ToSourceStr());
			else
				return string.Format("if ({0}) {1}", ConditionValue.ToSourceStr(), Body.ToSourceStr());
		}
	}
}
