using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_MemberAccessOp : Expression
	{
		public string MemberName;
		public string VariableName;

		public override IEnumerator<Expression> GetEnumerator()
		{
			yield break;
		}

		public Expr_MemberAccessOp()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Token PT = Tok.Peek();
			//throw new NotImplementedException();

			MemberName = Tok.NextToken().Text;


			return this;
		}

		public override string ToSourceStr()
		{
			return $"{MemberName}";
		}
	}
}
