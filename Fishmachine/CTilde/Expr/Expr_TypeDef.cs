using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTilde.Expr
{
	public class Expr_TypeDef : Expression
	{
		static string[] PtrTypes = new string[] { "string", "voidptr" };

		public string Type;
		public bool IsArray;
		public bool IsPointer;
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

			if (IsPointerType(this))
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

		public static Expr_TypeDef MakeByte()
		{
			Expr_TypeDef Byte = new Expr_TypeDef();
			Byte.Type = "byte";
			return Byte;
		}

		public static Expr_TypeDef MakeArray(Expr_TypeDef BaseType, int Size)
		{
			Expr_TypeDef ArrayType = new Expr_TypeDef();
			ArrayType.Type = BaseType.Type;
			ArrayType.IsArray = true;
			ArrayType.ArraySize = Size;
			return ArrayType;
		}

		public static int GetRawTypeSize(Expr_TypeDef Type)
		{
			if (IsPointerType(Type))
				return 4;
			else if (Type.Type == "int" || Type.Type == "uint" || Type.Type == "float")
				return 4;
			else if (Type.Type == "byte" || Type.Type == "char" || Type.Type == "bool")
				return 1;

			throw new NotImplementedException();
		}

		public static int GetDerefTypeSize(Expr_TypeDef Type)
		{
			if (!IsPointerType(Type))
				throw new Exception("Not pointer or array type");

			if (PtrTypes.Contains(Type.Type))
				return 1;

			Expr_TypeDef DerefType = new Expr_TypeDef();
			DerefType.Type = Type.Type;
			DerefType.IsArray = false;
			DerefType.IsPointer = false;
			return GetRawTypeSize(DerefType);
		}

		/*public static int GetTypeSize(Expr_TypeDef Type)
		{
			if (IsPointerType(Type))
				return GetDerefTypeSize(Type);

			return GetRawTypeSize(Type);
		}*/

		public static bool IsUnsigned(string TypeName)
		{
			if (TypeName == "byte" || TypeName == "uint" || TypeName == "bool")
				return true;
			return false;
		}

		/*public static bool IsPointerType(string TypeName)
		{
			if (TypeName == "string" || TypeName == "voidptr")
				return true;

			return false;
		}*/

		public static bool IsPointerType(Expr_TypeDef TD)
		{
			if (PtrTypes.Contains(TD.Type))
				return true;

			return TD.IsArray || TD.IsPointer;
		}

		public static bool IsArrayType(Expr_TypeDef TD)
		{
			return TD.IsArray;
		}

		public override string ToSourceStr()
		{
			if (PtrTypes.Contains(Type))
				return Type;

			if (IsArray)
				return string.Format("{0}[{1}]", Type, ArraySize);
			else if (IsPointer)
				return string.Format("{0}*", Type);
			else
				return Type;
		}
	}
}
