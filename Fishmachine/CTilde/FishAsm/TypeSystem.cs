using CTilde.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.CTilde.FishAsm
{
	public class TypeSystem
	{
		List<FishTypeDef> DefinedTypes = new List<FishTypeDef>();

		public FishStructDef DefineStruct(string Name)
		{
			FishStructDef S = new FishStructDef(Name);
			DefinedTypes.Add(S);
			return S;
		}

		public int GetSize(string Name)
		{
			foreach (FishTypeDef S in DefinedTypes)
			{
				if (S.Name == Name)
					return S.Size;
			}

			return -1;
		}

		public bool TryGetType(string Name, out FishTypeDef FT)
		{
			foreach (FishTypeDef S in DefinedTypes)
			{
				if (S.Name == Name)
				{
					FT = S;
					return true;
				}
			}

			FT = null;
			return false;
		}
	}

	public class FishTypeDef
	{
		public string Name;

		public virtual int Size
		{
			get; set;
		}

		public FishTypeDef(string Name, int Size)
		{
			this.Name = Name;
		}
	}

	public class FishStructDef : FishTypeDef
	{
		public List<FishFieldDef> Fields;

		public override int Size
		{
			get => Fields.Sum(F => F.Size);
			set => throw new InvalidOperationException();
		}

		public FishStructDef(string name) : base(name, 0)
		{
			Fields = new List<FishFieldDef>();
		}

		public FishFieldDef Field(string Name, int Size, Expr_TypeDef Type)
		{
			FishFieldDef F = new FishFieldDef(Name, Size, Type);
			Fields.Add(F);
			return F;
		}

		public int GetFieldOffset(string Name)
		{
			int Offset = 0;

			foreach (FishFieldDef F in Fields)
			{
				if (F.Name == Name)
					return Offset;

				Offset += F.Size;
			}

			throw new Exception("Field not found: " + Name);
		}

		public Expr_TypeDef GetFieldType(string Name)
		{
			foreach (FishFieldDef F in Fields)
			{
				if (F.Name == Name)
					return F.Type;
			}

			throw new Exception("Field not found: " + Name);
		}

		public int GetFieldSize(string Name)
		{
			foreach (FishFieldDef F in Fields)
			{
				if (F.Name == Name)
					return F.Size;
			}

			return -1;
		}
	}

	public class FishFieldDef
	{
		public string Name;
		public int Size;
		public Expr_TypeDef Type;

		public FishFieldDef(string name, int size, Expr_TypeDef type)
		{
			Name = name;
			Size = size;
			Type = type;
		}
	}
}
