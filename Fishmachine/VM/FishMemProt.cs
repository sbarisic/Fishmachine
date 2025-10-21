using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public class FishMemProt
	{
		public FishMemPriv RequiredPriv;
		public uint BaseAddr;
		public uint Size;
		public string Name;

		public bool Contains(uint Addr)
		{
			if (Addr >= BaseAddr && Addr < (BaseAddr + Size))
				return true;

			return false;
		}

		public bool Intersects(uint Addr, uint Sz)
		{
			if (Sz == 0)
				return false;

			if (Addr + Sz <= BaseAddr)
				return false;

			if (Addr >= BaseAddr + Size)
				return false;

			if (Addr < BaseAddr && (Addr + Sz) >= (BaseAddr + Size))
				return true;

			if (Contains(Addr) || Contains(Addr + Sz - 1))
				return true;

			return true;
		}

		public static FishMemPriv GetPriv(Reg R, bool Read)
		{
			if (R == Reg.EBP || R == Reg.ESP)
				return FishMemPriv.Stack;

			if (Read)
				return FishMemPriv.Read;

			return FishMemPriv.Write;
		}

		public static FishMemPriv GetPriv(uint Addr, uint StackAddr, uint StackSize, bool Read)
		{
			if (Addr > (StackAddr - StackSize) && Addr <= StackAddr)
				return FishMemPriv.Stack;

			if (Read)
				return FishMemPriv.Read;

			return FishMemPriv.Write;
		}

		public bool HasAccess(FishMemPriv Priv)
		{
			if (Priv == FishMemPriv.Debugger)
				return true;

			if ((RequiredPriv & Priv) != FishMemPriv.None)
			{
				return true;
			}

			return false;
		}

		public FishMemProt(FishMemPriv requiredPriv, uint baseAddr, uint size, string name)
		{
			RequiredPriv = requiredPriv;
			BaseAddr = baseAddr;
			Size = size;
			Name = name;
		}

		public FishMemProt(FishMemPriv requiredPriv, string name) : this(requiredPriv, 0, 0, name)
		{
		}
	}
}
