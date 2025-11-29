using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_ConstNull : Expression
	{
		public override IEnumerator<Expression> GetEnumerator()
		{
			yield break;
		}

		public override string ToSourceStr()
		{
			return "null";
		}
	}
}
