using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_SwitchBlock : Expression
	{
		public List<Expr_CaseBlock> Expressions;

		public Expr_SwitchBlock()
		{
			Expressions = new List<Expr_CaseBlock>();
		}

		public override Expression Parse(Tokenizer Tok)
		{
			/*Tok.NextToken().Assert(Symbol.LBrace);

			while (!Tok.Peek().Is(Symbol.RBrace))
				Expressions.Add(Expression.ParseStatement(Tok));

			Tok.NextToken().Assert(Symbol.RBrace);*/

			Tok.NextToken().Assert(Symbol.LBrace);

			while (!Tok.Peek().Is(Symbol.RBrace))
			{
				Token PT = Tok.Peek();

				while (Tok.Peek().Is(Keyword.@case))
				{
					Expr_CaseBlock CaseBlock = new Expr_CaseBlock().Parse<Expr_CaseBlock>(Tok);
					Expressions.Add(CaseBlock);
					PT = Tok.Peek();
				}

				if (Tok.Peek().Is(Keyword.@default))
				{
					Expr_CaseBlock CaseBlock = new Expr_CaseBlock().Parse<Expr_CaseBlock>(Tok);
					Expressions.Add(CaseBlock);
					PT = Tok.Peek();
				}

				if (Tok.Peek().Is(Symbol.RBrace))
					break;
				else
					throw new NotImplementedException();
			}

			Tok.NextToken().Assert(Symbol.RBrace);
			return this;
		}

		public override string ToSourceStr()
		{
			return string.Format("{{ {0} }}", string.Join("", Expressions.Select(E => E.ToSourceStr() + "; ")));
		}
	}
}
