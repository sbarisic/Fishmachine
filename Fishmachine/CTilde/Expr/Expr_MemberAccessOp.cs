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
		public string InstanceName;
		public string MemberName;

		public Expr_MemberAccessOp()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			Token PT = Tok.Peek();
			//throw new NotImplementedException();

			InstanceName = Tok.NextToken().Text;
			Tok.NextToken().Assert(Symbol.Dot);
			MemberName = Tok.NextToken().Text;


			return this;
		}

		public override string ToSourceStr()
		{
			return $"{InstanceName}.{MemberName}";
		}
	}
}
