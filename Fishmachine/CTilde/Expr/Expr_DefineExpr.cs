using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_DefineExpr : Expression
	{
		public string Ident;
		public Expression ValueExpr;

		public override IEnumerator<Expression> GetEnumerator()
		{
			yield return ValueExpr;
		}

		public Expr_DefineExpr()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Tok.NextToken().Assert(Keyword.define);
			Ident = Tok.NextToken().Assert(TokenType.Identifier).Text;

			Token EqTk = Tok.NextToken();
			EqTk.Assert(Symbol.Assignment);

			ValueExpr = Expression.ParseExpression(Tok, Symbol.Semicolon);

			return this;
		}
	}
}
