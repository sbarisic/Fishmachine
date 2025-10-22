using ABT;
using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
				case FishInst.DBG_REGS:
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
				case FishInst.MUL_REG_REG:
				case FishInst.DIV_REG_REG:
				case FishInst.MOVE_REG_REG:
				case FishInst.TEST_REG_REG:
				case FishInst.MOVEZ_REG_REG:
				case FishInst.MOVES_REG_REG:
				case FishInst.MOVEBYTE_REG_REG:
				case FishInst.CMP_REG_REG:
				case FishInst.BINAND_REG_REG:
				case FishInst.BINOR_REG_REG:
				case FishInst.BINXOR_REG_REG:
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
		FishStackTrace LastException;
		bool Halted;

		uint StackAddr = 0x0;
		uint StackSize = 0x0;
		uint MemAllocPtrStart = 0x0;
		uint MemAllocPtr = 0x0;
		byte[] Memory;

		List<FishMemProt> MemProt = new List<FishMemProt>();

		public Graphics Gfx;
		public uint IntTableAddr;
		public StringBuilder Out = new StringBuilder();

		public FishRegisters Regs = new FishRegisters();
		public FishInst CurrentInstruction;
		public uint CurrentInstructionIP;
		List<VMSymbol> VMSymbols = new List<VMSymbol>();

		public FishVM()
		{
			ProtectMemory(0x0, 0x100, new FishMemProt(FishMemPriv.None, "null page"));
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

		public void AllocateMemory(uint Size)
		{
			Memory = new byte[Size];
		}

		public void SetInitialStack(uint Addr, uint Size)
		{
			StackAddr = Addr;
			StackSize = Size;
			Regs.Write(CodeGeneration.Reg.ESP, Addr);
			Regs.Write(CodeGeneration.Reg.EBP, Addr);

			ProtectMemory(Addr - Size, Addr, new FishMemProt(FishMemPriv.Stack | FishMemPriv.Supervisor, "stack"));
		}

		public void SetMemMgrPointer(uint Addr)
		{
			MemAllocPtrStart = Addr;
			MemAllocPtr = Addr;
		}

		public uint GetMemMgrPointer(out int AllocatedBytes)
		{
			AllocatedBytes = (int)(MemAllocPtrStart - MemAllocPtr);
			return MemAllocPtrStart;
		}

		public uint LoadToMemory(byte[] Input, uint Offset, bool Supervisor)
		{
			Array.Copy(Input, 0, Memory, Offset, Input.Length);
			uint EndAddr = Offset + (uint)Input.Length;

			FishMemPriv Priv = FishMemPriv.ReadWriteExecute;

			if (Supervisor)
				Priv = Priv | FishMemPriv.Supervisor;

			ProtectMemory(Offset, (uint)Input.Length, new FishMemProt(Priv, "bytecode"));
			return EndAddr;
		}

		public uint VirtualToReal(uint Address)
		{
			return Address;
		}

		public uint RealToVirtual(uint Address)
		{
			return Address;
		}

		public byte[] MemoryBankForRealAddress(uint Address, ref FishStackTrace E)
		{
			E.SetException(this, FishExcept.None);

			if (Address >= Memory.Length)
			{
				E.SetException(this, FishExcept.AccessViolation);

				if (Debugger.IsAttached && true)
					Debugger.Break();

				return null;
			}

			return Memory;
		}

		public IEnumerable<FishMemProt> GetProtection(uint Address, uint Size)
		{
			if (Size == 1)
			{
				foreach (FishMemProt P in MemProt)
				{
					if (P.Contains(Address))
						yield return P;
				}
			}
			else
			{
				foreach (FishMemProt P in MemProt)
				{
					if (P.Intersects(Address, Size))
						yield return P;
				}
			}

			yield break;
		}

		public void ProtectMemory(uint Address, uint Size, FishMemProt Prot)
		{
			FishMemProt Found = null;
			if ((Found = GetProtection(Address, Size).FirstOrDefault()) == null)
			{
				Prot.BaseAddr = Address;
				Prot.Size = Size;
				MemProt.Add(Prot);
			}
			else
				throw new Exception();
		}

		public void ProtectMemoryRange(uint StartAddress, uint EndAddress, FishMemProt Prot)
		{
			uint Addr = StartAddress < EndAddress ? StartAddress : EndAddress;
			ProtectMemory(Addr, Prot.Size, Prot);
		}

		public void CheckAccess(uint Address, uint Size, FishMemPriv Priv, ref FishStackTrace E)
		{
			if (!E.Is(FishExcept.None))
				return;

			Address = VirtualToReal(Address);
			byte[] MemBank = MemoryBankForRealAddress(Address, ref E);

			FishMemProt P = GetProtection(Address, Size).FirstOrDefault();

			if (P != null)
			{
				if (P.RequiredPriv == FishMemPriv.None)
				{
					if (FishSettings.DebugMemoryProt)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Access violation at 0x{0:X} for {1} access (prot: {2})", Address, Priv, P);
						Console.ResetColor();
					}

					Regs.Write(Reg.DB0, Address);
					E.SetException(this, FishExcept.AccessViolation);
					return;
				}

				if (P.HasAccess(FishMemPriv.Supervisor))
				{
					if (!Regs.IsSupervisor)
					{
						if (FishSettings.DebugMemoryProt)
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Privilege violation at 0x{0:X} for {1} access (prot: {2})", Address, Priv, P);
							Console.ResetColor();
						}

						Regs.Write(Reg.DB0, Address);
						E.SetException(this, FishExcept.PrivilegeViolation);
						return;
					}
				}

				if (!P.HasAccess(Priv))
				{
					Regs.Write(Reg.DB0, Address);

					if (FishSettings.DebugMemoryProt)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Access violation at 0x{0:X} for {1} access (prot: {2})", Address, Priv, P);
						Console.ResetColor();
					}

					switch (Priv)
					{
						case FishMemPriv.Read:
							Regs.Write(Reg.DB1, 1);
							E.SetException(this, FishExcept.AccessViolationRead);
							return;

						case FishMemPriv.Write:
							Regs.Write(Reg.DB1, 2);
							E.SetException(this, FishExcept.AccessViolationWrite);
							return;

						case FishMemPriv.Execute:
							Regs.Write(Reg.DB1, 3);
							E.SetException(this, FishExcept.AccessViolationExecute);
							return;

						case FishMemPriv.Stack:
							Regs.Write(Reg.DB1, 4);
							E.SetException(this, FishExcept.AccessViolationStack);
							return;

						default:
							Regs.Write(Reg.DB1, 5);
							E.SetException(this, FishExcept.AccessViolationUnknown);
							Debugger.Break();
							return;
					}
				}
			}
		}

		public byte ReadByte(uint Address, FishMemPriv Priv, ref FishStackTrace E)
		{
			try
			{
				Address = VirtualToReal(Address);
				byte[] MemBank = MemoryBankForRealAddress(Address, ref E);

				CheckAccess(Address, 1, Priv, ref E);
				if (!E.Is(FishExcept.None))
					return 0;

				if (FishSettings.DebugExceptions && !E.Is(FishExcept.None))
				{
					Console.WriteException("{0} reading byte at 0x{1:X}", E, Address);

					uint ESP = Regs.Read(Reg.ESP);
					FishStackTrace ST = new FishStackTrace();
					PrintMem(ESP, ref ST);

					Regs.PrintAll();
				}

				if (!E.Is(FishExcept.None))
					return 0;

				return MemBank[Address];
			}
			catch (Exception Ex)
			{
				Console.WriteLine("Exception: {0}", Ex.ToString());
				E.SetException(this, FishExcept.AccessViolation);

				if (Debugger.IsAttached && true)
					Debugger.Break();

				return 0;
			}
		}

		public byte[] ReadBytes(uint VirtAddr, int Count, FishMemPriv Priv, ref FishStackTrace E)
		{
			VirtAddr = VirtualToReal(VirtAddr);

			CheckAccess(VirtAddr, (uint)Count, Priv, ref E);
			if (!E.Is(FishExcept.None))
				return new byte[0];

			byte[] Bytes = MemoryBankForRealAddress(VirtAddr, ref E);
			if (!E.Is(FishExcept.None))
				return null;

			byte[] Result = new byte[Count];
			Array.Copy(Bytes, VirtAddr, Result, 0, Count);
			return Result;
		}

		public uint ReadUInt32(uint VirtAddr, FishMemPriv Priv, ref FishStackTrace E)
		{
			/*VirtAddr = VirtualToReal(VirtAddr);
			byte[] Bytes = MemoryBankForRealAddress(VirtAddr, ref E);
			if (!E.Is(FishExcept.None))
				return 0;*/
			byte[] Bytes = ReadBytes(VirtAddr, 4, Priv, ref E);
			if (!E.Is(FishExcept.None))
				return 0;

			return BitConverter.ToUInt32(Bytes, 0);
		}

		byte ReadByteFromIP(ref FishStackTrace E)
		{
			byte Value = ReadByte(Regs.IP, FishMemPriv.Execute, ref E);
			if (!E.Is(FishExcept.None))
				return 0;

			Regs.IP = Regs.IP + 1;
			return Value;
		}

		void ReadBytes4FromIP(byte[] Bytes, ref FishStackTrace E)
		{
			Bytes[0] = ReadByteFromIP(ref E);
			if (!E.Is(FishExcept.None))
				return;

			Bytes[1] = ReadByteFromIP(ref E);
			if (!E.Is(FishExcept.None))
				return;

			Bytes[2] = ReadByteFromIP(ref E);
			if (!E.Is(FishExcept.None))
				return;

			Bytes[3] = ReadByteFromIP(ref E);
			if (!E.Is(FishExcept.None))
				return;
		}

		int ReadInt32FromIP(ref FishStackTrace E)
		{
			byte[] Bytes = new byte[4];

			ReadBytes4FromIP(Bytes, ref E);
			if (!E.Is(FishExcept.None))
				return 0;

			return BitConverter.ToInt32(Bytes);
		}

		uint ReadUInt32FromIP(ref FishStackTrace E)
		{
			byte[] Bytes = new byte[4];

			ReadBytes4FromIP(Bytes, ref E);
			if (!E.Is(FishExcept.None))
				return 0;

			return BitConverter.ToUInt32(Bytes);
		}

		public void WriteByte(uint VirtAddress, byte Value, FishMemPriv Priv, ref FishStackTrace E)
		{
			VirtAddress = VirtualToReal(VirtAddress);

			CheckAccess(VirtAddress, 1, Priv, ref E);
			if (!E.Is(FishExcept.None))
				return;

			MemoryBankForRealAddress(VirtAddress, ref E)[VirtAddress] = Value;
		}

		public void WriteBytes(uint VirtAddress, byte[] Value, FishMemPriv Priv, ref FishStackTrace E)
		{
			VirtAddress = VirtualToReal(VirtAddress);
			byte[] MemBank = MemoryBankForRealAddress(VirtAddress, ref E);
			if (!E.Is(FishExcept.None))
				return;

			if (FishSettings.DebugPrintMemory)
			{
				Console.WriteLine("Write {0} bytes to 0x{1:X}", Value.Length, VirtAddress);
			}

			CheckAccess(VirtAddress, (uint)Value.Length, Priv, ref E);
			if (!E.Is(FishExcept.None))
				return;

			Array.Copy(Value, 0, MemBank, VirtAddress, Value.Length);
		}

		public void WriteUInt32(uint VirtAddress, uint UInt, FishMemPriv Priv, ref FishStackTrace E)
		{
			WriteBytes(VirtAddress, BitConverter.GetBytes(UInt), Priv, ref E);
		}

		public void SetSupervisor(bool Sup)
		{
			Regs.IsSupervisor = Sup;
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

		public void Interrupt(FishInterrupt Num, ref FishStackTrace E)
		{
			if (!Regs.IntEnabled)
				return;

			if (!Regs.SoftIntEnabled)
				return;

			if (IRETStack.Count > 0)
				return;

			Regs.Write(Reg.XSC, (uint)Num);
			E.SetException(this, FishExcept.None);
		}

		public void Interrupt(FishInterrupt Num, uint Arg1, ref FishStackTrace E)
		{
			if (!Regs.IntEnabled)
				return;

			if (IRETStack.Count > 0)
				return;

			Regs.Write(Reg.XR1, Arg1);
			Interrupt(Num, ref E);
		}

		public void Syscall(FishSyscall FInt, uint Arg1, ref FishStackTrace E)
		{
			E.SetException(this, FishExcept.None);

			//if (FishSettings.DebugPrint)
			//{
			if (FishSettings.DebugPrintSyscall)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("SYSCALL {0}", FInt);
				Console.ResetColor();
			}
			//}


			if (FInt == FishSyscall.StopMachine)
			{
				Halted = true;
			}
			else if (FInt == FishSyscall.PrintChar)
			{
				Console.WriteLine("PrintChar '{0}'", (char)Arg1);
				Out.Append((char)Arg1);

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
				Out.AppendFormat("{0}", Arg1);

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
				Interrupt((FishInterrupt)Arg1, ref E);
			}
			else if (FInt == FishSyscall.Alloc)
			{
				bool Failed = false;

				uint BytesPtr = Arg1;
				uint Bytes = ReadUInt32(BytesPtr, FishMemProt.GetPriv(BytesPtr, StackAddr, StackSize, true), ref E);

				if (E.Is(FishExcept.None))
				{
					MemAllocPtr = MemAllocPtr - Bytes;
					uint AllocMem = MemAllocPtr;

					if (Bytes == 0)
						AllocMem = 0;

					if (AllocMem != 0)
					{
						FishMemPriv Priv = FishMemPriv.ReadWrite;

						if (Regs.IsSupervisor)
							Priv = Priv | FishMemPriv.Supervisor;

						ProtectMemory(AllocMem, Bytes, new FishMemProt(Priv, "alloc"));
					}

					WriteUInt32(BytesPtr, AllocMem, FishMemProt.GetPriv(BytesPtr, StackAddr, StackSize, false), ref E);
					if (E.Is(FishExcept.None) && FishSettings.DebugPrintMemory)
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
			else if (FInt == FishSyscall.Cls)
			{
				Gfx.Clear();
			}


		}

		bool CallLong(uint Addr, ref FishStackTrace E)
		{
			uint RetAddr = Regs.IP;

			uint ESP = Regs.Read(Reg.ESP);
			uint WriteAddr = ESP - sizeof(uint);

			WriteBytes(WriteAddr, BitConverter.GetBytes(RetAddr), FishMemPriv.Stack, ref E);
			if (!E.Is(FishExcept.None))
				return true;

			Regs.Write(Reg.ESP, WriteAddr);

			Jump(Addr);
			return false;
		}

		bool PushReg(Reg R, ref FishStackTrace E)
		{
			uint ESP = Regs.Read(Reg.ESP);
			uint WriteAddr = ESP - sizeof(uint);

			uint RVal = Regs.Read(R);
			WriteUInt32(WriteAddr, RVal, FishMemPriv.Stack, ref E);
			if (!E.Is(FishExcept.None))
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

		bool PopReg(Reg R, ref FishStackTrace E)
		{
			uint ESP = Regs.Read(Reg.ESP);
			uint RegVal = ReadUInt32(ESP, FishMemPriv.Stack, ref E);
			if (!E.Is(FishExcept.None))
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

		bool Enter(uint N, ref FishStackTrace E)
		{
			if (PushReg(Reg.EBP, ref E))
				return true;

			Regs.Write(Reg.EBP, Regs.Read(Reg.ESP));
			Regs.Write(Reg.ESP, Regs.Read(Reg.ESP) - N);
			return false;
		}

		bool Leave(ref FishStackTrace E)
		{
			Regs.Write(Reg.ESP, Regs.Read(Reg.EBP));
			if (PopReg(Reg.EBP, ref E))
				return true;

			return false;
		}

		public void PrintGlobal(string Name, uint Addr)
		{
			FishStackTrace E = new FishStackTrace();
			uint Val = ReadUInt32(Addr, FishMemPriv.Read, ref E);

			if (!E.Is(FishExcept.None))
				return;

			Console.WriteLine("Global '{0}' at 0x{1:X4} = 0x{2:X}", Name, Addr, Val);
		}

		public void PrintMem(uint Start, ref FishStackTrace E)
		{
			int PrintLines = 16;
			int BytesLen = 16;
			E.SetException(this, FishExcept.None);

			for (int i = 0; i < VMSymbols.Count; i++)
			{
				uint Val = ReadUInt32(VMSymbols[i].Address, FishMemPriv.Debugger, ref E);
				/*if (!E.Is(FishExcept.None))
					return;*/

				uint Val2 = ReadUInt32(Val, FishMemPriv.Debugger, ref E);
				/*if (!E.Is(FishExcept.None))
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
					byte B = ReadByte(MemPtrLoc, FishMemPriv.Debugger, ref E);

					/*if (!E.Is(FishExcept.None))
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
					byte B = ReadByte(MemPtrLoc, FishMemPriv.Debugger, ref E);

					/*if (!E.Is(FishExcept.None))
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

		public bool Run(ref FishStackTrace E)
		{
			Halted = false;
			E.Clear();

			const int YieldInstr = 1;
			int YieldCounter = 0;

			while (Step(ref E))
			{
				Regs.PrintAll();

				if (!E.Is(FishExcept.None) && !E.Is(FishExcept.RequestWait))
				{
					string ExStr = E.ToString();
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(ExStr);
					Console.ResetColor();

					throw new Exception(string.Format("VM {0}", ExStr));
				}

				if (!E.Is(FishExcept.None) && !LastException.Is(FishExcept.None))
				{
					throw new Exception("VM double fault");
				}
				LastException = new FishStackTrace(E);

				if (Halted)
					break;

				if (!E.Is(FishExcept.None))
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
