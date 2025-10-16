using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_TypeDef : Expression
	{
		public string Type;
		public bool IsArray, IsPointer;
		public int ArraySize = 0;

		public override Expression Parse(Tokenizer Tok)
		{
			Type = Tok.NextToken().Assert(TokenType.Identifier).Text;
			if (Tok.Peek().Is(Symbol.Star))
			{
				Tok.NextToken().Assert(Symbol.Star);
				IsPointer = true;
			}
			else if (Tok.Peek().Is(Symbol.LBracket) && Tok.Peek(2).Is(Symbol.RBracket))
			{
				Tok.NextToken().Assert(Symbol.LBracket);
				Tok.NextToken().Assert(Symbol.RBracket);
				IsArray = true;
			}
			else if (Tok.Peek().Is(Symbol.LBracket) && Tok.Peek(3).Is(Symbol.RBracket))
			{
				Tok.NextToken().Assert(Symbol.LBracket);

				Token SizeTok = Tok.NextToken();
				if (SizeTok.Is(TokenType.Decimal))
				{
					ArraySize = int.Parse(SizeTok.Text);
				}
				else
					throw new NotImplementedException();

				Tok.NextToken().Assert(Symbol.RBracket);
				IsArray = true;
			}

			if (IsPointerType(Type))
				IsPointer = true;

			return this;
		}

		public static Expr_TypeDef MakeClassRef(string Name)
		{
			Expr_TypeDef Ref = new Expr_TypeDef();
			Ref.Type = Name;
			Ref.IsPointer = true;
			return Ref;
		}

		public static Expr_TypeDef MakeVoid()
		{
			Expr_TypeDef Void = new Expr_TypeDef();
			Void.Type = "void";
			return Void;
		}


		public static int GetRawTypeSize(Expr_TypeDef Type)
		{
			if (Type.Type == "int" || Type.Type == "uint" || Type.Type == "float" || Type.Type == "string" || Type.Type == "voidptr")
				return 4;
			else if (Type.Type == "byte" || Type.Type == "char" || Type.Type == "bool")
				return 1;

			throw new NotImplementedException();
		}

		public static int GetTypeSize(Expr_TypeDef Type)
		{
			if (Type.IsPointer || Type.IsArray)
				return 4;

			return GetRawTypeSize(Type);
		}

		public static bool IsUnsigned(string TypeName)
		{
			if (TypeName == "byte" || TypeName == "uint" || TypeName == "bool")
				return true;
			return false;
		}

		public static bool IsPointerType(string TypeName)
		{
			if (TypeName == "string" || TypeName == "voidptr")
				return true;

			return false;
		}

		public static int GetPointerTypeSize(Expr_TypeDef Type)
		{
			if (Type.Type == "string")
				return 1; // string is array of bytes

			if (!(Type.IsPointer || Type.IsArray))
				throw new Exception("Not pointer or array type");

			return GetRawTypeSize(Type);
		}
	}
}
