using Fishmachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = Fishmachine.Console;

namespace CTilde.Expr
{
	public class Expr_FuncDef : Expression
	{
		public string FuncName;

		public Expr_TypeDef FuncReturnTypeDef;
		//public Expr_VariableDef FuncVariableDef;

		public Expr_ParamsDef FuncParams;
		public Expr_Block FuncBody;
		public bool IsCtor, IsDtor;

		public bool Naked;
		public bool Interrupt;
		public bool IsFunctionCall = false;

		public Expr_FuncDef()
		{
		}

		public override Expression Parse(Tokenizer Tok)
		{
			IsCtor = IsDtor = false;
			if (Tok.Peek().Is(Keyword.__ctor))
				IsCtor = true;

			else if (Tok.Peek().Is(Keyword.__dtor))
				IsDtor = true;

			if (IsCtor || IsDtor)
			{
				Tok.NextToken();

				FuncReturnTypeDef = Expr_TypeDef.MakeVoid();
				FuncName = Expr_ClassDef.CurrentClass?.Name ?? Expr_StructDef.CurrentStruct.Name;

				if (IsCtor)
					FuncName += "__ctor";
				else if (IsDtor)
					FuncName += "__dtor";
			}
			else
			{
				FuncReturnTypeDef = new Expr_TypeDef().Parse<Expr_TypeDef>(Tok);
				FuncName = Tok.NextToken().Assert(TokenType.Identifier).Text;
			}


			Tok.NextToken().Assert(Symbol.LParen);
			FuncParams = new Expr_ParamsDef().Parse<Expr_ParamsDef>(Tok);
			Tok.NextToken().Assert(Symbol.RParen);

			Token P = Tok.Peek();
			if (Tok.Peek().Is(Symbol.LBrace))
			{
				if (!FishSettings.TokenizerSilent)
					Console.WriteLine(">> {0}", P);

				FuncBody = new Expr_Block().Parse<Expr_Block>(Tok);

				FuncBody.Expressions.Where(E => E is Expr_ReturnStatement).Select(S =>
				{
					((Expr_ReturnStatement)S).RetTypeDef = FuncReturnTypeDef;
					return S;
				}).ToArray();

				IsFunctionCall = false;
			}
			else
			{
				IsFunctionCall = true;
			}

			return this;
		}
	}
}
