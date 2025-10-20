using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public class VMSymbol
	{
		public string Name;
		public uint Address;
		
		public VMSymbol(string Name, uint Address)
		{
			this.Name = Name;
			this.Address = Address;
		}
	}
}
