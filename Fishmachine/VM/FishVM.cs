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
	public class FishVM
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
				case FishInst.DBG_BREAK:
				case FishInst.SYSCALL_2:
				case FishInst.FLOAT_ADD:
				case FishInst.FLOAT_SUB:
				case FishInst.FLOAT_MUL:
				case FishInst.FLOAT_DIV:
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

		public FishRegisters Regs = new FishRegisters();

		public FishVM()
		{
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
			Address = VirtualToReal(Address);
			return MemoryBankForRealAddress(Address, out E)[Address];
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
			Reg[] RegsEnum = Enum.GetValues<Reg>().ToArray();
			foreach (var R in RegsEnum)
			{
				if (R == Reg.MAX_VALUE)
					continue;

				//Console.Write("{0} = {1:X4} ", R, this.Regs.Read(R));
				Regs.Read(R);
			}
			//Console.WriteLine();
			//Console.ResetColor();

			if (FInt == FishSyscall.StopMachine)
			{
				Halted = true;
			}
			else if (FInt == FishSyscall.PrintChar)
			{
				if (FishSettings.DebugPrint)
				{
					Console.WriteLine("VM: 0x{0:X} = '{1}'", Arg1, (char)Arg1);
				}

				Gfx.Write((char)Arg1);
				//File.AppendAllText("vm_sys.txt", ((char)Arg1).ToString());
			}
			else if (FInt == FishSyscall.PrintNum)
			{
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

		bool Step(out FishException E)
		{
			E = FishException.None;

			// Handle interrupts
			FishInterrupt IntNum = (FishInterrupt)Regs.Read(Reg.XSC);
			if (IntNum != FishInterrupt.None)
			{
				Regs.Write(Reg.XSC, 0);
				// Handle interrupt

				uint IntAddr = ReadUInt32(0x100 + (uint)(IntNum - 1) * 4, out E);
				if (E != FishException.None)
					return true;

				if (IntAddr != 0)
				{
					// Preserve EFLAGS
					IRETStack.Push((Regs.IP, IntNum));
					if (PushReg(Reg.RFLAGS, out E))
						return true;

					// IntEnabled is restored by popping RFLAGS below, as it is just a bit field in RFLAGS
					Regs.IntEnabled = false;

					// Push interrupt arguments
					switch (IntNum)
					{
						case FishInterrupt.Int1_KeyboardKey:
						case FishInterrupt.Int2_KeyboardChar:
							if (PushReg(Reg.XR1, out E))
								return true;

							Regs.Write(Reg.XR1, 0);
							break;

						case FishInterrupt.None:
						case FishInterrupt.Int0:
						case FishInterrupt.Int3:
							break;

						default:
							throw new NotImplementedException();
					}

					CallLong(IntAddr, out E);
					return true;
				}
			}

			if (FishSettings.DebugPrint || FishSettings.DebugPrintInstruction)
				Console.Write("{0:X4}: ", Regs.IP);

			if (IRETStack.Count > 0 && IRETStack.TryPeek(out (uint, FishInterrupt) IRETAddr) && IRETAddr.Item1 == Regs.IP)
			{
				switch (IRETAddr.Item2)
				{
					case FishInterrupt.Int1_KeyboardKey:
					case FishInterrupt.Int2_KeyboardChar:
						if (PopReg(Reg.XR1, out E))
							return true;

						Regs.Write(Reg.XR1, 0);
						break;

					case FishInterrupt.None:
					case FishInterrupt.Int0:
					case FishInterrupt.Int3:
						break;

					default:
						throw new NotImplementedException();
				}

				if (PopReg(Reg.RFLAGS, out E))
					return true;

				IRETStack.Pop();
			}


			FishInst Inst = (FishInst)ReadByteFromIP(out E);
			if (E != FishException.None)
				return true;

			if (FishSettings.DebugPrint || FishSettings.DebugPrintInstruction)
				Console.WriteLine("{0}", Inst);

			switch (Inst)
			{
				case FishInst.NOP:
					{
						break;
					}

				case FishInst.WAIT:
					{
						E = FishException.RequestWait;
						return true;
					}

				case FishInst.HALT:
					{
						return false;
					}

				case FishInst.PUSH_REG:
					{
						Reg R = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						if (PushReg(R, out E))
							return true;

						break;
					}

				case FishInst.POP_REG:
					{
						Reg R = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						if (PopReg(R, out E))
							return true;

						/*uint ESP = Regs.Read(Reg.ESP);
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
						}*/

						break;
					}

				case FishInst.MUL_REG:
					{
						Reg R = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint RegVal = Regs.Read(R);
						uint AX = Regs.Read(Reg.AX);
						uint Mul = RegVal * AX;
						Regs.Write(Reg.AX, Mul);

						break;
					}

				case FishInst.IMUL_REG:
					{
						Reg R = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						int RegVal = (int)Regs.Read(R);
						int AX = (int)Regs.Read(Reg.AX);
						int Mul = RegVal * AX;
						Regs.Write(Reg.AX, (uint)Mul);

						break;
					}

				case FishInst.PUSH_LONG:
					{
						uint Val = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);

						WriteBytes(WriteAddr, BitConverter.GetBytes(Val), out E);
						if (E != FishException.None)
							return true;

						Regs.Write(Reg.ESP, WriteAddr);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.DarkYellow;
							Console.WriteLine("Push long {0} to {1}", Val, WriteAddr);
							Console.ResetColor();
						}

						break;
					}
				case FishInst.MOVEBYTE_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						byte Val = (byte)(Regs.Read(R1) & 0xFF);
						Regs.Write(R2, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Move byte from {0} to {1}: {2:X2}", R1, R2, Val);
							Console.ResetColor();
						}

						break;
					}
				case FishInst.LEA_ADDR_REG:
					{
						uint Addr = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Regs.Write(R2, Addr);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine("LEA_ADDR_REG: Write address {0:X8} to {1}", Addr, R2);
							Console.ResetColor();
						}

						break;
					}
				case FishInst.LEA_OFFSET_REG_REG:
					{
						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						Regs.Write(R2, Addr);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine("LEA_OFFSET_REG_REG: Write address {0:X8} to {1}", Addr, R2);
							Console.ResetColor();
						}

						break;
					}
				case FishInst.MOVEZ_REG_REG:
				case FishInst.MOVES_REG_REG:
				case FishInst.MOVE_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint R1Val = 0;

						if (Inst == FishInst.MOVE_REG_REG)
							R1Val = Regs.Read(R1);
						else if (Inst == FishInst.MOVES_REG_REG)
						{
							// Sign extend byte (0xFF becomes 0xFFFFFFFF)
							byte byteVal = (byte)(Regs.Read(R1) & 0xFF);
							R1Val = (uint)(sbyte)byteVal;
						}
						else if (Inst == FishInst.MOVEZ_REG_REG)
						{
							// Zero extend word (keep lower 8 bits)
							R1Val = Regs.Read(R1) & 0xFF;
						}

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.TEST_REG_REG:
					{
						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						int Result = (int)R1Val - (int)R2Val;

						Regs.LessThan = R1Val < R2Val;
						Regs.Equal = R1Val == R2Val;
						Regs.IsZero = (R1Val & R2Val) == 0;
						Regs.GreaterThan = R1Val > R2Val;
						Regs.Sign = Result < 0;

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Test ({0}) {1}; 0x{1:X} and ({2}) {3}; 0x{3:X}", R1, R1Val, R2, R2Val);
							Console.WriteLine("IsZero = {0}, Sign = {1}, GreaterThan = {2}, Equal = {3}, LessThan = {4}", Regs.IsZero, Regs.Sign, Regs.GreaterThan, Regs.Equal, Regs.LessThan);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.CMP_REG_REG:
					{
						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						int Result = (int)R1Val - (int)R2Val;
						//Regs.Write(R2, (uint)Result);

						Regs.LessThan = R1Val < R2Val;
						Regs.Equal = R1Val == R2Val;
						Regs.IsZero = Result == 0;
						Regs.GreaterThan = R1Val > R2Val;
						Regs.Sign = Result < 0;

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Cmp ({0}) {1}; 0x{1:X} and ({2}) {3}; 0x{3:X}", R1, R1Val, R2, R2Val);
							Console.WriteLine("IsZero = {0}, Sign = {1}, GreaterThan = {2}, Equal = {3}, LessThan = {4}", Regs.IsZero, Regs.Sign, Regs.GreaterThan, Regs.Equal, Regs.LessThan);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.SETNOTEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Val = !Regs.Equal ? 1u : 0u;
						Regs.Write(R1, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.SETEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Val = Regs.Equal ? 1u : 0u;
						Regs.Write(R1, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.SETGREATEREQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Val = Regs.GreaterThan || Regs.Equal ? 1u : 0u;
						Regs.Write(R1, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.SETGREATER_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Val = Regs.GreaterThan ? 1u : 0u;
						Regs.Write(R1, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.SETLESS_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Val = Regs.LessThan ? 1u : 0u;
						Regs.Write(R1, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.SETLESSEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Val = Regs.LessThan || Regs.Equal ? 1u : 0u;
						Regs.Write(R1, Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.MOVEBYTE_REG_OFFSET_REG:
				case FishInst.MOVE_REG_OFFSET_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint R1Val = Regs.Read(R1);
						uint Addr = (uint)(Regs.Read(R2) + Offset);

						if (Inst == FishInst.MOVE_REG_OFFSET_REG)
						{
							byte[] WriteVal = BitConverter.GetBytes(R1Val);
							WriteBytes(Addr, WriteVal, out E);
							if (E != FishException.None)
								return true;

							if (FishSettings.DebugPrint)
							{
								Console.ForegroundColor = ConsoleColor.Yellow;
								Console.WriteLine("Wrote bytes ({5:X8}; {5}) {{ {0:X2} {1:X2} {2:X2} {3:X2} }} to {4:X4}", WriteVal[0], WriteVal[1], WriteVal[2], WriteVal[3], Addr, R1Val);
								Console.ResetColor();
							}
						}
						else
						{
							byte WriteB = (byte)(R1Val & 0xFF);
							WriteByte(Addr, WriteB, out E);
							if (E != FishException.None)
								return true;

							if (FishSettings.DebugPrint)
							{
								Console.ForegroundColor = ConsoleColor.Yellow;
								Console.WriteLine("Wrote byte {0:X2} to {1:X4}", WriteB, Addr);
								Console.ResetColor();
							}
						}
						//Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVEZ_OFFSET_REG_REG:
					{
						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Zero extend word from memory
						byte[] wordBytes = ReadBytes(Addr, 2, out E);
						if (E != FishException.None)
							return true;

						ushort wordVal = BitConverter.ToUInt16(wordBytes, 0);
						uint R1Val = wordVal;

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Read zero-extended word {0:X4} from {1:X4} -> {2:X8}", wordVal, Addr, R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVES_OFFSET_REG_REG:
					{
						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Sign extend byte from memory
						byte byteVal = ReadByte(Addr, out E);
						if (E != FishException.None)
							return true;

						uint R1Val = (uint)(sbyte)byteVal;

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Read sign-extended byte {0:X2} ({1}) from {2:X4} -> {3:X8}", byteVal, (sbyte)byteVal, Addr, R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVE_OFFSET_REG_REG:
					{
						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Regular 32-bit read
						uint R1Val = ReadUInt32(Addr, out E);
						if (E != FishException.None)
							return true;

						byte[] ReadVal = BitConverter.GetBytes(R1Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Read bytes ({5:X8}; {5}) {{ {0:X2} {1:X2} {2:X2} {3:X2} }} from {4:X4}", ReadVal[0], ReadVal[1], ReadVal[2], ReadVal[3], Addr, R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.SUB_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint RVal = Regs.Read(R2);
						Regs.Write(R2, RVal - L1);
						break;
					}

				case FishInst.SUB_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Magenta;
							Console.WriteLine("Sub ({0}) {1}, ({2}) {3} = {4}", R1, R1Val, R2, R2Val, R2Val - R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R2Val - R1Val);
						break;
					}

				case FishInst.ADD_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Magenta;
							Console.WriteLine("Add ({0}) {1}, ({2}) {3} = {4}", R1, R1Val, R2, R2Val, R1Val + R2Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R1Val + R2Val);
						break;
					}

				case FishInst.ADD_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint Result = L1 + Regs.Read(R2);
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.MOVE_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						Regs.Write(R2, L1);
						break;
					}

				case FishInst.MOVEZ_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						// Zero extend the lower 16 bits of the immediate value
						uint Result = L1 & 0xFFFF;
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.MOVES_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						// Sign extend the lower 8 bits of the immediate value
						byte byteVal = (byte)(L1 & 0xFF);
						uint Result = (uint)(sbyte)byteVal;
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.CMP_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R2 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint R2Val = Regs.Read(R2);
						int Result = (int)R2Val - (int)L1;

						Regs.LessThan = R2Val < L1;
						Regs.Equal = R2Val == L1;
						Regs.IsZero = Result == 0;
						Regs.GreaterThan = R2Val > L1;
						Regs.Sign = Result < 0;

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("Cmp ({0}) {1}; 0x{1:X} and immediate {2}; 0x{2:X}", R2, R2Val, L1);
							Console.WriteLine("IsZero = {0}, Sign = {1}, GreaterThan = {2}, Equal = {3}, LessThan = {4}", Regs.IsZero, Regs.Sign, Regs.GreaterThan, Regs.Equal, Regs.LessThan);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.CALL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Addr = Regs.Read(R1);

						if (CallLong(Addr, out E))
							return true;

						//Jump(Addr);
						break;
					}

				case FishInst.CALL_LONG:
					{
						uint Addr = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						if (CallLong(Addr, out E))
							return true;

						break;
					}

				case FishInst.RET:
					{
						uint ESP = Regs.Read(Reg.ESP);
						uint RetAddr = ReadUInt32(ESP, out E);
						if (E != FishException.None)
							return true;

						Regs.Write(Reg.ESP, ESP + sizeof(uint));
						Jump(RetAddr);
						break;
					}

				case FishInst.SYSCALL_2:
					{
						uint ESP = Regs.Read(Reg.ESP);
						uint SyscallNum = ReadUInt32(ESP, out E);
						if (E != FishException.None)
							return true;

						uint Arg1 = ReadUInt32(ESP + 4, out E);
						if (E != FishException.None)
							return true;

						Syscall((FishSyscall)SyscallNum, Arg1, out E);
						if (E != FishException.None)
							return true;

						break;
					}

				case FishInst.SYSCALL:
					{
						uint SyscallNum = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Syscall((FishSyscall)SyscallNum, 0, out E);
						if (E != FishException.None)
							return true;

						break;
					}

				case FishInst.JUMP_IF_NOT_ZERO_LONG:
				case FishInst.JUMP_IF_ZERO_LONG:
				case FishInst.JUMP_LONG:
				case FishInst.FLOAT_LOAD_LONG:
					{
						uint Addr = ReadUInt32FromIP(out E);
						if (E != FishException.None)
							return true;


						if (Inst == FishInst.JUMP_IF_ZERO_LONG)
						{
							if (Regs.IsZero)
							{
								Jump(Addr);
							}
						}
						else if (Inst == FishInst.JUMP_IF_NOT_ZERO_LONG)
						{
							if (!Regs.IsZero)
							{
								Jump(Addr);
							}
						}
						else if (Inst == FishInst.JUMP_LONG)
						{
							Jump(Addr);
						}
						else if (Inst == FishInst.FLOAT_LOAD_LONG)
						{
							float Val = BitConverter.ToSingle(ReadBytes(Addr, 4, out E));
							if (E != FishException.None)
								return true;

							Regs.FpuPush(Val);
						}
						else
							throw new NotImplementedException();

						break;
					}

				case FishInst.DOUBLE_STORE_OFFSET_REG:
				case FishInst.FLOAT_STORE_OFFSET_REG:
				case FishInst.DOUBLE_POP_OFFSET_REG:
				case FishInst.FLOAT_POP_OFFSET_REG:
					{
						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						float FVal = 0;

						if (Inst == FishInst.FLOAT_POP_OFFSET_REG || Inst == FishInst.DOUBLE_POP_OFFSET_REG)
						{
							FVal = Regs.FpuPop();
						}
						else if (Inst == FishInst.FLOAT_STORE_OFFSET_REG || Inst == FishInst.DOUBLE_STORE_OFFSET_REG)
						{
							FVal = Regs.FpuPeek();
						}

						byte[] FBytes = BitConverter.GetBytes(FVal);
						uint Addr = (uint)(Regs.Read(R1) + Offset);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.DarkBlue;
							Console.WriteLine("FPU {0} {1} store to 0x{2:X}", Inst, FVal, Addr);
							Console.ResetColor();
						}

						WriteBytes(Addr, FBytes, out E);
						if (E != FishException.None)
							return true;

						break;
					}

				case FishInst.DOUBLE_LOAD_OFFSET_REG:
				case FishInst.FLOAT_LOAD_OFFSET_REG:
					{
						int Offset = ReadInt32FromIP(out E);
						if (E != FishException.None)
							return true;

						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;


						uint Addr = (uint)(Regs.Read(R1) + Offset);
						byte[] FBytes = ReadBytes(Addr, 4, out E);
						if (E != FishException.None)
							return true;

						float FVal = BitConverter.ToSingle(FBytes);
						Regs.FpuPush(FVal);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.DarkBlue;
							Console.WriteLine("FPU {0} {1} read from 0x{2:X}", Inst, FVal, Addr);
							Console.ResetColor();
						}

						break;
					}

				case FishInst.FLOAT_ADD:
				case FishInst.FLOAT_SUB:
				case FishInst.FLOAT_MUL:
				case FishInst.FLOAT_DIV:
					{
						float Val1 = Regs.FpuPop();
						float Val2 = Regs.FpuPop();
						float Result = 0;

						if (Inst == FishInst.FLOAT_ADD)
							Result = Val1 + Val2;
						else if (Inst == FishInst.FLOAT_SUB)
							Result = Val1 - Val2;
						else if (Inst == FishInst.FLOAT_MUL)
							Result = Val1 * Val2;
						else if (Inst == FishInst.FLOAT_DIV)
							Result = Val1 / Val2;

						if (float.IsInfinity(Result))
						{
							E = FishException.FloatInfinity;
							return true;
						}

						if (float.IsNaN(Result))
						{
							E = FishException.FloatNaN;
							return true;
						}

						Regs.FpuPush(Result);
						break;
					}

				case FishInst.JUMP_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(out E);
						if (E != FishException.None)
							return true;

						uint Addr = Regs.Read(R1);
						Jump(Addr);
						break;
					}

				case FishInst.LEAVE:
					{
						if (Leave(out E))
							return true;
						/*// Restore ESP from EBP
						Regs.Write(Reg.ESP, Regs.Read(Reg.EBP));

						// Pop EBP
						uint ESP = Regs.Read(Reg.ESP);
						uint RegVal = ReadUInt32(ESP, out E);
						if (E != FishException.None)
							return true;

						Regs.Write(Reg.EBP, RegVal);

						Regs.Write(Reg.ESP, ESP + sizeof(uint));*/
						break;
					}

				case FishInst.DBG_BREAK:
					{
						/*if (Debugger.IsAttached)
							Debugger.Break();*/

						Debugger.Break();
						break;
					}


				default:
					{
						E = FishException.InvalidInstruction;
						return true;
					}
			}

			return true;
		}

		public bool Run(out FishException E)
		{
			Halted = false;
			E = FishException.None;

			const int YieldInstr = 24;
			int YieldCounter = 0;

			while (Step(out E))
			{
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
