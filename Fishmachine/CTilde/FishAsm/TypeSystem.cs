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
		List<FishStructDef> Structs = new List<FishStructDef>();

		public FishStructDef DefineStruct(string Name)
		{
			FishStructDef S = new FishStructDef(Name);
			Structs.Add(S);
			return S;
		}

		public int GetSize(string Name)
		{
			foreach (FishStructDef S in Structs)
			{
				if (S.Name == Name)
					return S.Size;
			}

			return -1;
		}
	}

	public class FishStructDef
	{
		public string Name;
		public List<FishFieldDef> Fields;

		public int Size
		{
			get
			{
				return Fields.Sum(F => F.Size);
			}
		}

		public FishStructDef(string name)
		{
			Name = name;
			Fields = new List<FishFieldDef>();
		}

		public FishFieldDef Field(string Name, int Size, Expr_TypeDef Type)
		{
			FishFieldDef F = new FishFieldDef(Name, Size, Type);
			Fields.Add(F);
			return F;
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
