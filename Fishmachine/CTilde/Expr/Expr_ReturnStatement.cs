using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_ReturnStatement : Expression
	{
		public Expression RetValExpr;
		public Expr_TypeDef RetTypeDef;

		public override IEnumerator<Expression> GetEnumerator()
		{
			yield return RetValExpr;
			yield return RetTypeDef;
		}

		public Expr_ReturnStatement()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Keyword.@return);

			if (!Tok.Peek().Is(Symbol.Semicolon))
			{
				RetValExpr = Expression.ParseExpression(Tok, Symbol.Semicolon);
			}

			if (RetValExpr == null)
				Tok.NextToken().Assert(Symbol.Semicolon);

			return this;
		}

		public Expr_ReturnStatement Parse2(Tokenizer Tok, Expr_TypeDef RetTypeDef)
		{
			Expr_ReturnStatement Ret = (Expr_ReturnStatement)Parse(Tok);
			Ret.RetTypeDef = RetTypeDef;
			return Ret;
		}

		public override string ToSourceStr()
		{
			if (RetValExpr != null)
				return string.Format("return {0};", RetValExpr.ToSourceStr());

			return string.Format("return;");
		}
	}
}
