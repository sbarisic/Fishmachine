using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public enum BinaryOp
	{
		And,
		Or
	}

	public class Expr_BinaryOp : Expression
	{
		public Expression LExpr;
		public BinaryOp Op;
		public Expression RExpr;

		public Expr_BinaryOp(Expression LExpr)
		{
			this.LExpr = LExpr;
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Token T = Tok.NextToken();

			if (T.Is(Symbol.BinaryAnd))
				Op = BinaryOp.And;
			else if (T.Is(Symbol.BinaryOr))
				Op = BinaryOp.Or;
			else
				throw new NotImplementedException("Unexpected token " + T);

			RExpr = Expression.ParseExpression(Tok, Symbol.Semicolon);
			return this;
		}

		public override string ToSourceStr()
		{
			switch (Op)
			{
				case BinaryOp.And:
					return string.Format("({0}) && ({1})", LExpr.ToSourceStr(), RExpr.ToSourceStr());

				case BinaryOp.Or:
					return string.Format("({0}) || ({1})", LExpr.ToSourceStr(), RExpr.ToSourceStr());

				default:
					throw new NotImplementedException();
			}
		}
	}
}
