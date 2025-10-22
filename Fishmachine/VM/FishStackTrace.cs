using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public struct FishStackEntry
	{
		public FishExcept Exception;
		public FishRegisters Regs;
		public uint IP;
		public uint DB0;
		public uint DB1;
		public uint DB2;
		public uint DB3;

		public uint InstrIP;
		public FishInst Instr;
		public string InstrParams;

		public FishStackEntry(FishVM VM, FishExcept E)
		{
			this.Exception = E;
			this.Regs = VM.Regs;
			this.InstrIP = VM.CurrentInstructionIP;
			this.Instr = VM.CurrentInstruction;
			this.IP = VM.Regs.IP;
			DB0 = Regs.Read(CodeGeneration.Reg.DB0);
			DB1 = Regs.Read(CodeGeneration.Reg.DB1);
			DB2 = Regs.Read(CodeGeneration.Reg.DB2);
			DB3 = Regs.Read(CodeGeneration.Reg.DB3);
			InstrParams = "";
		}

		public override string ToString()
		{
			return string.Format("(0x{8:X}) {1}{7}\n    {0} - EAX: 0x{2:X}, EBX: 0x{3:X}, ECX: 0x{4:X}, EDX: 0x{5:X} - IP (0x{6:X})",
				Exception,
				Instr.ToString(),
				Regs.Read(CodeGeneration.Reg.EAX),
				Regs.Read(CodeGeneration.Reg.EBX),
				Regs.Read(CodeGeneration.Reg.ECX),
				Regs.Read(CodeGeneration.Reg.EDX),
				IP,
				(InstrParams.Length > 0) ? (" " + InstrParams) : (""),
				InstrIP
				);
		}
	}

	public class FishStackTrace
	{
		public FishExcept Exception;
		public List<FishStackEntry> Trace = new List<FishStackEntry>();

		public FishStackTrace()
		{
			Clear();
		}

		public override string ToString()
		{
			StringBuilder SB = new StringBuilder();
			SB.AppendFormat("Excption Stack Trace: {0}\n", Exception);

			foreach (var Itm in Trace.Reverse<FishStackEntry>())
			{
				SB.AppendFormat("  at {0}\n", Itm.ToString());
			}

			return SB.ToString();
		}

		public FishStackTrace(FishStackTrace Other) : this()
		{
			Exception = Other.Exception;

			foreach (var Itm in Other.Trace)
			{
				Trace.Add(Itm);
			}
		}

		public void Clear()
		{
			Exception = FishExcept.None;
			Trace.Clear();
		}

		public void SetParams(params object[] Args)
		{
			FishStackEntry E = Trace.Last();
			Trace.RemoveAt(Trace.Count - 1);
			E.InstrParams = string.Join(", ", Args.Select(a => a.ToString()));
			Trace.Add(E);
		}

		public bool Is(FishExcept E)
		{
			return Exception == E;
		}

		public void SetException(FishVM VM, FishExcept ExType)
		{
			Trace.Add(new FishStackEntry(VM, ExType));
			Exception = ExType;
		}
	}
}
