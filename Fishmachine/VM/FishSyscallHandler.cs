using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public ref struct FishSyscallArgs
	{
		public FishVM VM;
		public uint[] Args;
		public ref FishStackTrace E;

		public uint[] Return;
	}

	public delegate void FishSyscallFunc(object UserData, ref FishSyscallArgs Syscall);

	public class FishSyscallHandler
	{
		public FishSyscall Syscall;
		public FishSyscallFunc Func;
		public object UserData;

		public FishSyscallHandler(FishSyscall Syscall, object UserData, FishSyscallFunc Func)
		{
			this.Syscall = Syscall;
			this.Func = Func;
			this.UserData = UserData;
		}

		public void Invoke(ref FishSyscallArgs Syscall)
		{
			Func(UserData, ref Syscall);
		}
	}
}
