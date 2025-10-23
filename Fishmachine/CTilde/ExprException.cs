using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.CTilde
{

	[Serializable]
	public class ExprException : Exception
	{
		public Token PeekTok;
		public AsmToken AsmTok;

		public string Msg;

		public ExprException(Token PeekTok)
		{
			this.PeekTok = PeekTok;
		}
		public ExprException(AsmToken AsmTok)
		{
			this.AsmTok = AsmTok;
		}

		public ExprException(Token PeekTok, string Msg) : this(PeekTok)
		{
			this.Msg = Msg;
		}

		public ExprException(AsmToken AsmTok, string Msg) : this(AsmTok)
		{
			this.Msg = Msg;
		}

		public override string ToString()
		{
			StringBuilder Msg = new StringBuilder();

			if (PeekTok != null)
				Msg.Append(PeekTok.ToString() + " - ");

			if (AsmTok != null)
				Msg.Append(AsmTok.ToString() + " - ");

			if (this.Msg != null)
				Msg.AppendLine(this.Msg);
			else
				Msg.AppendLine("Could not parse token");

			return Msg.ToString();
		}
	}
}
