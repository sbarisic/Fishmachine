using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_AssignVariable : Expression
	{
		public Expr_Identifier Variable;
		public Expression AssignmentValue;

		public override IEnumerator<Expression> GetEnumerator()
		{
			yield return Variable;
			yield return AssignmentValue;
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Variable = new Expr_Identifier().Parse<Expr_Identifier>(Tok);

			Tok.NextToken().Assert(Symbol.Assignment);

			AssignmentValue = Expression.ParseExpression(Tok, Symbol.Semicolon);
			return this;
		}

		public override string ToSourceStr()
		{
			return string.Format("{0} = {1}", Variable.ToSourceStr(), AssignmentValue.ToSourceStr());
		}
	}
}
