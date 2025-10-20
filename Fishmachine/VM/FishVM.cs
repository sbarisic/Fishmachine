using ABT;
using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public partial class FishVM
	{
		public static int FishInstSize(FishInst Inst)
		{
			switch (Inst)
			{
				case FishInst.INVALID:
				case FishInst.NOP:
				case FishInst.HALT:
				case FishInst.WAIT:
				case FishInst.LEAVE:
				case FishInst.RET:
				case FishInst.DBG_MEM:
				case FishInst.DBG_BREAK:
				case FishInst.SYSCALL_2:
				case FishInst.FLOAT_ADD:
				case FishInst.FLOAT_SUB:
				case FishInst.FLOAT_MUL:
				case FishInst.FLOAT_DIV:
				case FishInst.SOFTINT_ENABLE:
				case FishInst.SOFTINT_DISABLE:
					return 1;

				// One register, 2 byte total
				case FishInst.PUSH_REG:
				case FishInst.JUMP_REG:
				case FishInst.CALL_REG:
				case FishInst.POP_REG:
				case FishInst.MUL_REG:
				case FishInst.IMUL_REG:
				case FishInst.SETNOTEQUAL_REG:
				case FishInst.SETEQUAL_REG:
				case FishInst.SETGREATER_REG:
				case FishInst.SETGREATEREQUAL_REG:
				case FishInst.SETLESS_REG:
				case FishInst.SETLESSEQUAL_REG:
					return 2;

				// Two registers, 3 byte total
				case FishInst.ADD_REG_REG:
				case FishInst.SUB_REG_REG:
				case FishInst.MOVE_REG_REG:
				case FishInst.TEST_REG_REG:
				case FishInst.MOVEZ_REG_REG:
				case FishInst.MOVES_REG_REG:
				case FishInst.MOVEBYTE_REG_REG:
				case FishInst.CMP_REG_REG:
					return 3;

				// One 32-bit operand, 5 byte total
				case FishInst.SYSCALL:

				case FishInst.FLOAT_LOAD_LONG:
				case FishInst.DOUBLE_LOAD_LONG:
				case FishInst.JUMP_LONG:
				case FishInst.PUSH_LONG:
				case FishInst.CALL_LONG:
				case FishInst.JUMP_IF_ZERO_LONG:
				case FishInst.JUMP_IF_NOT_ZERO_LONG:
				case FishInst.JUMP_IF_LESS_LONG:
				case FishInst.JUMP_IF_LESSEQ_LONG:
				case FishInst.JUMP_IF_GREAT_LONG:
				case FishInst.JUMP_IF_GREATEQ_LONG:
					return 5;

				// One 32-bit operand, one 8-bit operand, 6 byte total
				case FishInst.LEA_ADDR_REG:
				case FishInst.SUB_LONG_REG:
				case FishInst.ADD_LONG_REG:
				case FishInst.MOVE_LONG_REG:
				case FishInst.MOVEZ_LONG_REG:
				case FishInst.MOVES_LONG_REG:
				case FishInst.CMP_LONG_REG:
				case FishInst.FLOAT_LOAD_OFFSET_REG:
				case FishInst.FLOAT_POP_OFFSET_REG:
				case FishInst.FLOAT_STORE_OFFSET_REG:
				case FishInst.DOUBLE_LOAD_OFFSET_REG:
				case FishInst.DOUBLE_POP_OFFSET_REG:
				case FishInst.DOUBLE_STORE_OFFSET_REG:
					return 6;

				// One 32 bit operand, two 8-bit operands, 7 byte total
				case FishInst.MOVE_REG_OFFSET_REG:
				case FishInst.MOVEBYTE_REG_OFFSET_REG:
				case FishInst.MOVEBYTE_OFFSET_REG_REG:
				case FishInst.MOVE_OFFSET_REG_REG:
				case FishInst.LEA_OFFSET_REG_REG:
				case FishInst.MOVEZ_OFFSET_REG_REG:
				case FishInst.MOVES_OFFSET_REG_REG:
					return 7;

				default:
					throw new InvalidProgramException("Invalid instruction");
			}
		}

		Stack<(uint, FishInterrupt)> IRETStack = new Stack<(uint, FishInterrupt)>();
		FishException LastException;
		bool Halted;

		uint MemAllocPtrStart = 0x0;
		uint MemAllocPtr = 0x0;
		byte[] Memory;

		public Graphics Gfx;
		public uint IntTableAddr;

		public FishRegisters Regs = new FishRegisters();
		List<VMSymbol> VMSymbols = new List<VMSymbol>();

		public FishVM()
		{
		}

		public VMSymbol FindSymbol(string Name)
		{
			for (int i = 0; i < VMSymbols.Count; i++)
			{
				if (VMSymbols[i].Name == Name)
					return VMSymbols[i];
			}

			return null;
		}

		public void DefineSymbol(string Name, uint Addr)
		{
			VMSymbol S = FindSymbol(Name);

			if (S != null)
				S.Address = Addr;
			else
				VMSymbols.Add(new VMSymbol(Name, Addr));
		}

		public void AllocateMemory(int Size)
		{
			Memory = new byte[Size];
		}

		public void SetMemMgrPointer(uint Addr)
		{
			MemAllocPtrStart = Addr;
			MemAllocPtr = Addr;
		}

		public int LoadToMemory(byte[] Input, int Offset)
		{
			Array.Copy(Input, 0, Memory, Offset, Input.Length);
			return Input.Length + Offset;
		}

		public uint VirtualToReal(uint Address)
		{
			return Address;
		}

		public uint RealToVirtual(uint Address)
		{
			return Address;
		}

		public byte[] MemoryBankForRealAddress(uint Address, out FishException E)
		{
			E = FishException.None;

			if (Address == 0)
			{
				E = FishException.AccessViolation;
				return null;
			}

			if (Address >= Memory.Length)
			{
				E = FishException.AccessViolation;
				return null;
			}

			return Memory;
		}

		public byte ReadByte(uint Address, out FishException E)
		{
			try
			{
				Address = VirtualToReal(Address);
				byte[] MemBank = MemoryBankForRealAddress(Address, out E);

				if (FishSettings.DebugExceptions && E != FishException.None)
				{
					Console.WriteException("{0} reading byte at 0x{1:X}", E, Address);

					uint ESP = Regs.Read(Reg.ESP);
					PrintMem(ESP, out FishException _);

					Regs.PrintAll();
				}

				if (E != FishException.None)
					return 0;

				return MemBank[Address];
			}
			catch (Exception Ex)
			{
				Console.WriteLine("Exception: {0}", Ex.ToString());
				E = FishException.AccessViolation;
				return 0;
			}
		}

		public byte[] ReadBytes(uint VirtAddr, int Count, out FishException E)
		{
			VirtAddr = VirtualToReal(VirtAddr);

			byte[] Bytes = MemoryBankForRealAddress(VirtAddr, out E);
			if (E != FishException.None)
				return null;

			byte[] Result = new byte[Count];
			Array.Copy(Bytes, VirtAddr, Result, 0, Count);
			return Result;
		}

		public uint ReadUInt32(uint VirtAddr, out FishException E)
		{
			VirtAddr = VirtualToReal(VirtAddr);
			byte[] Bytes = MemoryBankForRealAddress(VirtAddr, out E);
			if (E != FishException.None)
				return 0;

			return BitConverter.ToUInt32(Bytes, (int)VirtAddr);
		}

		byte ReadByteFromIP(out FishException E)
		{
			byte Value = ReadByte(Regs.IP, out E);
			if (E != FishException.None)
				return 0;

			Regs.IP = Regs.IP + 1;
			return Value;
		}

		void ReadBytes4FromIP(byte[] Bytes, out FishException E)
		{
			Bytes[0] = ReadByteFromIP(out E);
			if (E != FishException.None)
				return;

			Bytes[1] = ReadByteFromIP(out E);
			if (E != FishException.None)
				return;

			Bytes[2] = ReadByteFromIP(out E);
			if (E != FishException.None)
				return;

			Bytes[3] = ReadByteFromIP(out E);
			if (E != FishException.None)
				return;
		}

		int ReadInt32FromIP(out FishException E)
		{
			byte[] Bytes = new byte[4];

			ReadBytes4FromIP(Bytes, out E);
			if (E != FishException.None)
				return 0;

			return BitConverter.ToInt32(Bytes);
		}

		uint ReadUInt32FromIP(out FishException E)
		{
			byte[] Bytes = new byte[4];

			ReadBytes4FromIP(Bytes, out E);
			if (E != FishException.None)
				return 0;

			return BitConverter.ToUInt32(Bytes);
		}

		public void WriteByte(uint VirtAddress, byte Value, out FishException E)
		{
			VirtAddress = VirtualToReal(VirtAddress);
			MemoryBankForRealAddress(VirtAddress, out E)[VirtAddress] = Value;
		}

		public void WriteBytes(uint VirtAddress, byte[] Value, out FishException E)
		{
			VirtAddress = VirtualToReal(VirtAddress);
			byte[] MemBank = MemoryBankForRealAddress(VirtAddress, out E);
			if (E != FishException.None)
				return;

			if (FishSettings.DebugPrintMemory)
			{
				Console.WriteLine("Write {0} bytes to 0x{1:X}", Value.Length, VirtAddress);
			}

			Array.Copy(Value, 0, MemBank, VirtAddress, Value.Length);
		}

		public void WriteUInt32(uint VirtAddress, uint UInt, out FishException E)
		{
			WriteBytes(VirtAddress, BitConverter.GetBytes(UInt), out E);
		}

		public void Jump(uint VirtAddress)
		{
			Regs.IP = VirtAddress;

			if (FishSettings.DebugPrint)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("IP = 0x{0:X4}", Regs.IP);
				Console.ResetColor();
			}
		}

		public void Interrupt(FishInterrupt Num)
		{
			if (!Regs.IntEnabled)
				return;

			if (!Regs.SoftIntEnabled)
				return;

			if (IRETStack.Count > 0)
				return;

			Regs.Write(Reg.XSC, (uint)Num);
		}

		public void Interrupt(FishInterrupt Num, uint Arg1)
		{
			if (!Regs.IntEnabled)
				return;

			if (IRETStack.Count > 0)
				return;

			Regs.Write(Reg.XR1, Arg1);
			Interrupt(Num);
		}

		public void Syscall(FishSyscall FInt, uint Arg1, out FishException E)
		{
			E = FishException.None;

			//if (FishSettings.DebugPrint)
			//{
			if (FishSettings.DebugPrintSyscall)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("SYSCALL {0}", FInt);
				Console.ResetColor();
			}
			//}

			//Console.ForegroundColor = ConsoleColor.Yellow;
			/*Reg[] RegsEnum = Enum.GetValues<Reg>().ToArray();
			foreach (var R in RegsEnum)
			{
				if (R == Reg.MAX_VALUE)
					continue;

				//Console.Write("{0} = {1:X4} ", R, this.Regs.Read(R));
				Regs.Read(R);
			}*/
			//Console.WriteLine();
			//Console.ResetColor();

			if (FInt == FishSyscall.StopMachine)
			{
				Halted = true;
			}
			else if (FInt == FishSyscall.PrintChar)
			{
				Console.WriteLine("PrintChar '{0}'", (char)Arg1);

				if (FishSettings.DebugPrint)
				{
					Console.WriteLine("VM: 0x{0:X} = '{1}'", Arg1, (char)Arg1);
				}

				Gfx.Write((char)Arg1);
				//File.AppendAllText("vm_sys.txt", ((char)Arg1).ToString());
			}
			else if (FInt == FishSyscall.PrintNum)
			{
				Console.WriteLine("PrintNum '{0}'", Arg1);

				if (FishSettings.DebugPrint)
				{
					Console.WriteLine("VM: 0x{0:X} = '{1}'", Arg1, Arg1);
				}

				Gfx.Write(Arg1.ToString());
				//File.AppendAllText("vm_sys.txt", ((char)Arg1).ToString());
			}
			else if (FInt == FishSyscall.SoftwareInterrupt)
			{
				Console.WriteLine("Interrupt {0}!", Arg1);
				Interrupt((FishInterrupt)Arg1);
			}
			else if (FInt == FishSyscall.Alloc)
			{
				bool Failed = false;

				uint BytesPtr = Arg1;
				uint Bytes = ReadUInt32(BytesPtr, out E);

				if (E == FishException.None)
				{
					MemAllocPtr = MemAllocPtr - Bytes;
					uint AllocMem = MemAllocPtr;

					if (Bytes == 0)
						AllocMem = 0;

					WriteUInt32(BytesPtr, AllocMem, out E);
					if (E == FishException.None && FishSettings.DebugPrintMemory)
					{
						Console.WriteLine("Alloc {0} bytes at 0x{1:X} ({1})", Bytes, AllocMem);
					}
					else
						Failed = true;
				}
				else
					Failed = true;

				if (Failed && FishSettings.DebugPrintMemory)
				{
					Console.WriteLine("FAIL - Alloc {0} bytes at 0x{1:X} ({1})", Arg1, 0);
				}
			}
			/*else if (Num == 5)
			{
				uint EAX = Regs.Read(Reg.EAX);
				EAX = ReadUInt32(EAX, out E);

				if (E != FishException.None)
					return;

				Console.WriteLine("EAX: {0}", EAX);
			}
			else if (Num == 6)
			{
				uint EAX = Regs.Read(Reg.EAX);
				uint EBX = Regs.Read(Reg.EBX);
				EAX = ReadUInt32(EAX, out E);

				if (E != FishException.None)
					return;

				EBX = ReadUInt32(EBX, out E);

				if (E != FishException.None)
					return;

				Console.WriteLine("EAX: {0}; EBX: {1}", EAX, EBX);
			}*/
		}

		bool CallLong(uint Addr, out FishException E)
		{
			uint RetAddr = Regs.IP;

			uint ESP = Regs.Read(Reg.ESP);
			uint WriteAddr = ESP - sizeof(uint);

			WriteBytes(WriteAddr, BitConverter.GetBytes(RetAddr), out E);
			if (E != FishException.None)
				return true;

			Regs.Write(Reg.ESP, WriteAddr);

			Jump(Addr);
			return false;
		}

		bool PushReg(Reg R, out FishException E)
		{
			uint ESP = Regs.Read(Reg.ESP);
			uint WriteAddr = ESP - sizeof(uint);

			uint RVal = Regs.Read(R);
			WriteUInt32(WriteAddr, RVal, out E);
			if (E != FishException.None)
				return true;

			Regs.Write(Reg.ESP, WriteAddr);

			if (FishSettings.DebugPrint)
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine("Push ({0}) {1} to {2}", R, RVal, WriteAddr);
				Console.ResetColor();
			}

			return false;
		}

		bool PopReg(Reg R, out FishException E)
		{
			uint ESP = Regs.Read(Reg.ESP);
			uint RegVal = ReadUInt32(ESP, out E);
			if (E != FishException.None)
				return true;

			Regs.Write(R, RegVal);
			Regs.Write(Reg.ESP, ESP + sizeof(uint));

			if (FishSettings.DebugPrint)
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine("Pop ({0}) new {1} from {2}", R, RegVal, ESP);
				Console.ResetColor();
			}

			return false;
		}

		uint EvaluateSetCondition(FishInst inst)
		{
			return inst switch
			{
				FishInst.SETEQUAL_REG => Regs.Equal ? 1u : 0u,
				FishInst.SETNOTEQUAL_REG => !Regs.Equal ? 1u : 0u,
				FishInst.SETGREATER_REG => Regs.GreaterThan ? 1u : 0u,
				FishInst.SETGREATEREQUAL_REG => (Regs.GreaterThan || Regs.Equal) ? 1u : 0u,
				FishInst.SETLESS_REG => Regs.LessThan ? 1u : 0u,
				FishInst.SETLESSEQUAL_REG => (Regs.LessThan || Regs.Equal) ? 1u : 0u,
				_ => throw new InvalidOperationException()
			};
		}

		uint ApplyMoveSemantics(FishInst inst, uint value)
		{
			return inst switch
			{
				FishInst.MOVE_REG_REG => value,
				FishInst.MOVES_REG_REG => (uint)(sbyte)(value & 0xFF),  // Sign extend byte
				FishInst.MOVEZ_REG_REG => value & 0xFF,                  // Zero extend byte
				_ => throw new InvalidOperationException()
			};
		}

		bool ShouldJump(FishInst inst)
		{
			return inst switch
			{
				FishInst.JUMP_LONG => true,
				FishInst.JUMP_IF_ZERO_LONG => Regs.IsZero,
				FishInst.JUMP_IF_NOT_ZERO_LONG => !Regs.IsZero,
				FishInst.JUMP_IF_LESS_LONG => Regs.LessThan,
				FishInst.JUMP_IF_GREAT_LONG => Regs.GreaterThan,
				FishInst.JUMP_IF_LESSEQ_LONG => Regs.LessThan || Regs.Equal,
				FishInst.JUMP_IF_GREATEQ_LONG => Regs.GreaterThan || Regs.Equal,
				_ => false
			};
		}

		bool Enter(uint N, out FishException E)
		{
			if (PushReg(Reg.EBP, out E))
				return true;

			Regs.Write(Reg.EBP, Regs.Read(Reg.ESP));
			Regs.Write(Reg.ESP, Regs.Read(Reg.ESP) - N);
			return false;
		}

		bool Leave(out FishException E)
		{
			Regs.Write(Reg.ESP, Regs.Read(Reg.EBP));
			if (PopReg(Reg.EBP, out E))
				return true;

			return false;
		}

		public void PrintGlobal(string Name, uint Addr)
		{
			uint Val = ReadUInt32(Addr, out FishException E);

			if (E != FishException.None)
				return;

			Console.WriteLine("Global '{0}' at 0x{1:X4} = 0x{2:X}", Name, Addr, Val);
		}

		public void PrintMem(uint Start, out FishException E)
		{
			int PrintLines = 16;
			int BytesLen = 16;
			E = FishException.None;

			for (int i = 0; i < VMSymbols.Count; i++)
			{
				uint Val = ReadUInt32(VMSymbols[i].Address, out E);
				/*if (E != FishException.None)
					return;*/

				uint Val2 = ReadUInt32(Val, out E);
				/*if (E != FishException.None)
					return;*/

				Console.WriteLine("{0} (@ 0x{1:X}) = 0x{2:X}; 0x{3:X}", VMSymbols[i].Name, VMSymbols[i].Address, Val, Val2);
			}

			bool GrowDown = true;

			if (GrowDown)
			{
				Console.WriteLine("Memory dump format");
				Console.WriteLine("0x(ML): (M)L (ML-1) (ML-2) (ML-3) ... (ML-n)");
				Console.WriteLine("0x(ML-n-1): (ML) (ML-1) (ML-2) (ML-3) ... (ML-n)");
			}
			else
			{
				Console.WriteLine("Memory dump format");
				Console.WriteLine("0x(ML): (ML) (ML+1) (ML+2) (ML+3) ... (ML+n)");
				Console.WriteLine("0x(ML+n+1): (ML) (ML+1) (ML+2) (ML+3) ... (ML+n)");
			}

			for (int i = 0; i < PrintLines; i++)
			{
				uint MemPtr = 0;
				int CurOffset = (i * BytesLen);

				if (GrowDown)
				{
					MemPtr = (uint)(Start - CurOffset);
				}
				else
				{
					MemPtr = (uint)(Start + CurOffset);
				}
				Console.Write("0x{0:X4}: ", MemPtr);

				for (int k = 0; k < BytesLen; k++)
				{
					Console.Write(" ");

					if (k % 4 == 0 && k != 0)
						Console.Write(" ");

					uint MemPtrLoc = (uint)(MemPtr - k);
					byte B = ReadByte(MemPtrLoc, out E);

					/*if (E != FishException.None)
						return;*/

					if (B == 0)
						Console.ForegroundColor = ConsoleColor.DarkGray;

					Console.Write("{0:X2}", B, MemPtrLoc);

					Console.ResetColor();
				}

				Console.Write(" ");

				for (int k = 0; k < BytesLen; k++)
				{
					if (k % 4 == 0 && k != 0)
						Console.Write(" ");

					uint MemPtrLoc = (uint)(MemPtr - k);
					byte B = ReadByte(MemPtrLoc, out E);

					/*if (E != FishException.None)
						return;*/

					char C = Encoding.ASCII.GetString(new byte[] { B })[0];
					if (char.IsLetterOrDigit(C) || char.IsPunctuation(C) || char.IsSymbol(C) || char.IsWhiteSpace(C))
					{
						if (C == '\n' || C == '\r' || C == '\t')
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write(".");
							Console.ResetColor();
						}
						else
							Console.Write("{0}", (char)B, MemPtrLoc);
					}
					else
						Console.Write(".");
				}

				Console.WriteLine();
			}
		}

		public bool Run(out FishException E)
		{
			Halted = false;
			E = FishException.None;

			const int YieldInstr = 1;
			int YieldCounter = 0;

			while (Step(out E))
			{
				Regs.PrintAll();

				if (E != FishException.None && E != FishException.RequestWait)
					throw new Exception(string.Format("VM {0}", E));

				if (E != FishException.None && LastException != FishException.None)
				{
					throw new Exception("VM double fault");
				}
				LastException = E;

				if (Halted)
					break;

				if (E != FishException.None)
				{
					return true;
				}

				YieldCounter++;

				if (YieldCounter >= YieldInstr)
				{
					YieldCounter = 0;
					return true;
				}
			}

			return false;
		}
	}
}
