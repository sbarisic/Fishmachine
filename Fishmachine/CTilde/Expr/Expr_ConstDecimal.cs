using Fishmachine.CTilde;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Expr
{
	public class Expr_ConstDecimal : Expression
	{
		public string NumberLiteral;

		public Expr_ConstDecimal(Token PeekTok, string NumberLiteral)
		{
			if (!(NumberLiteral.Contains(".") || NumberLiteral.EndsWith("f")))
				throw new ExprException(PeekTok, "Number expected, got decimal '" + NumberLiteral + "'");

			this.NumberLiteral = NumberLiteral;
		}

		public override string ToSourceStr()
		{
			return NumberLiteral;
		}
	}
}
