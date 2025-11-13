using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_StructDef : Expression
	{
		public static Expr_StructDef CurrentStruct;

		public string Name;
		public List<Expr_VariableDef> Variables;

		public Expr_StructDef()
		{
			Variables = new List<Expr_VariableDef>();
		}

		public override Expression Parse(Tokenizer Tok)
		{
			CurrentStruct = this;

			Tok.NextToken().Assert(Keyword.@struct);
			Name = Tok.NextToken().Assert(TokenType.Identifier).Text;
			Tok.NextToken().Assert(Symbol.LBrace);


			while (!Tok.Peek().Is(Symbol.RBrace))
			{
				Token PT = Tok.Peek();
				Expression E = Expression.ParseStatement(Tok);

				if (E is Expr_VariableDef)
				{

					Expr_VariableDef Var = (Expr_VariableDef)E;
					Variables.Add(Var);

				}
				else
					throw new Exception("Unexpected expression type " + E.GetType());
			}

			Tok.NextToken().Assert(Symbol.RBrace);

			/*Tok.NextToken().Assert(Keyword.@class);
			Name = Tok.NextToken().Assert(TokenType.Identifier).Text;
			Tok.NextToken().Assert(Symbol.LBrace);

			while (!Tok.Peek().Is(Symbol.RBrace)) {
				Expression E = Expression.ParseStatement(Tok);

				if (E is Expr_FuncDef) {

					Expr_FuncDef MemberFunc = (Expr_FuncDef)E;
					MemberFunc.FuncParams.Prepend(new ParamDefData(Expr_TypeDef.MakeClassRef(Name), "this"));
					Functions.Add(MemberFunc);

				} else if (E is Expr_VariableDef) {

					Expr_VariableDef Var = (Expr_VariableDef)E;
					Variables.Add(Var);

				} else
					throw new Exception("Unexpected expression type " + E.GetType());
			}

			Tok.NextToken().Assert(Symbol.RBrace);*/

			CurrentStruct = null;
			return this;
		}
	}
}
