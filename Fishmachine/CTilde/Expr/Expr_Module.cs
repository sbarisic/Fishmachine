using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_Module : Expression
	{
		public List<Expression> Expressions;

		public override IEnumerator<Expression> GetEnumerator()
		{
			foreach (var e in Expressions)
			{
				yield return e;
			}
		}

		public Expr_Module()
		{
			Expressions = new List<Expression>();
		}

		public void Add(Expression E)
		{
			Expressions.Add(E);
		}

		public override Expression Parse(Tokenizer Tok)
		{
			while (Tok.Peek() != null)
				Add(Expression.ParseStatement(Tok));

			return this;
		}
	}
}
