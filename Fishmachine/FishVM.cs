using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine
{
	public enum FishInst : byte
	{
		INVALID = 0,

		NOP,
		HALT,
		LEAVE,
		RET,
		DBG_BREAK,
		SYSCALL,
		SYSCALL_2,

		JUMP_REG,
		JUMP_LONG,

		JUMP_IF_ZERO_LONG,
		JUMP_IF_NOT_ZERO_LONG,

		CALL_REG,
		CALL_LONG,

		PUSH_REG,
		PUSH_LONG,

		POP_REG,

		TEST_REG_REG,
		MOVE_REG_REG,
		MOVE_LONG_REG,
		MOVE_OFFSET_REG_REG,
		MOVE_REG_OFFSET_REG,

		MOVEZ_LONG_REG,
		MOVEZ_OFFSET_REG_REG,
		MOVEZ_REG_REG,
		MOVES_LONG_REG,
		MOVES_OFFSET_REG_REG,
		MOVES_REG_REG,
		MOVEBYTE_REG_OFFSET_REG,
		MOVEBYTE_REG_REG,

		CMP_REG_REG,
		CMP_LONG_REG,

		SETNOTEQUAL_REG,
		SETEQUAL_REG,
		SETGREATER_REG,
		SETGREATEREQUAL_REG,
		SETLESS_REG,
		SETLESSEQUAL_REG,

		SUB_LONG_REG,
		SUB_REG_REG,

		ADD_LONG_REG,
		ADD_REG_REG,

		MUL_REG,
		IMUL_REG,

		LEA_ADDR_REG,
		LEA_OFFSET_REG_REG,
	}

	public struct FishRegisters
	{
		public uint[] Regs;
		public uint IP;


		public bool IsZero;
		public bool Sign;

		public bool LessThan;
		public bool Equal;
		public bool GreaterThan;

		public uint Read(CodeGeneration.Reg Reg)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(": Read {0} = 0x{1:X} - {1}", Reg, Regs[(int)Reg]);
			Console.ResetColor();

			if (Reg == Reg.AX)
				return Read(Reg.EAX) & 0xFFFF;


			switch (Reg)
			{
				case Reg.AL:
					return Read(Reg.EAX) & 0xFF;

				case Reg.AX:
					return Read(Reg.EAX) & 0xFFFF;

				case Reg.BL:
					return Read(Reg.EBX) & 0xFF;

				case Reg.BX:
					return Read(Reg.EBX) & 0xFFFF;

				default:
					break;
			}

			return Regs[(int)Reg];
		}

		public void Write(CodeGeneration.Reg Reg, uint Val)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(": Write {0} = 0x{2:X} - {2}", Reg, Regs[(int)Reg], Val);
			Console.ResetColor();

			switch (Reg)
			{
				case Reg.AL:
					Write(Reg.EAX, Val & 0xFF);
					return;

				case Reg.AX:
					Write(Reg.EAX, Val & 0xFFFF);
					return;

				case Reg.BL:
					Write(Reg.EBX, Val & 0xFF);
					return;

				case Reg.BX:
					Write(Reg.EBX, Val & 0xFFFF);
					return;

				default:
					break;
			}

			Regs[(int)Reg] = Val;
		}

		public FishRegisters()
		{
			Regs = new uint[24];
		}
	}

	public class FishVM
	{
		public static int FishInstSize(FishInst Inst)
		{
			switch (Inst)
			{
				case FishInst.NOP:
				case FishInst.HALT:
				case FishInst.LEAVE:
				case FishInst.RET:
				case FishInst.DBG_BREAK:
				case FishInst.SYSCALL_2:
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

		byte[] Memory;

		public FishRegisters Regs = new FishRegisters();
		bool Halted;

		public FishVM()
		{
		}

		public void AllocateMemory(int Size)
		{
			Memory = new byte[Size];
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

		public byte[] MemoryBankForRealAddress(uint Address)
		{
			return Memory;
		}

		public byte ReadByte(uint Address)
		{
			Address = VirtualToReal(Address);
			return MemoryBankForRealAddress(Address)[Address];
		}

		public byte[] ReadBytes(uint VirtAddr, int Count)
		{
			VirtAddr = VirtualToReal(VirtAddr);
			byte[] Bytes = MemoryBankForRealAddress(VirtAddr);
			byte[] Result = new byte[Count];
			Array.Copy(Bytes, VirtAddr, Result, 0, Count);
			return Result;
		}

		public uint ReadUInt32(uint VirtAddr)
		{
			VirtAddr = VirtualToReal(VirtAddr);
			byte[] Bytes = MemoryBankForRealAddress(VirtAddr);

			return BitConverter.ToUInt32(Bytes, (int)VirtAddr);
		}

		byte ReadByteFromIP()
		{
			byte Value = ReadByte(Regs.IP);
			Regs.IP = Regs.IP + 1;
			return Value;
		}

		public void WriteByte(uint VirtAddress, byte Value)
		{
			VirtAddress = VirtualToReal(VirtAddress);
			MemoryBankForRealAddress(VirtAddress)[VirtAddress] = Value;
		}

		public void WriteBytes(uint VirtAddress, byte[] Value)
		{
			VirtAddress = VirtualToReal(VirtAddress);
			Array.Copy(Value, 0, MemoryBankForRealAddress(VirtAddress), VirtAddress, Value.Length);
		}

		public void Jump(uint VirtAddress)
		{
			Regs.IP = VirtAddress;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("IP = 0x{0:X4}", Regs.IP);
			Console.ResetColor();
		}

		public void Syscall(uint Num, uint Arg1)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("SYSCALL {0}", Num);
			Console.ResetColor();

			//Console.ForegroundColor = ConsoleColor.Yellow;
			Reg[] RegsEnum = Enum.GetValues<Reg>().ToArray();
			foreach (var R in RegsEnum)
			{
				//Console.Write("{0} = {1:X4} ", R, this.Regs.Read(R));
				Regs.Read(R);
			}
			//Console.WriteLine();
			//Console.ResetColor();

			if (Num == 0)
			{
				Halted = true;
			}
			else if (Num == 1)
			{
				Console.WriteLine("VM: 0x{0:X} = '{1}'", Arg1, (char)Arg1);
				File.AppendAllText("vm_sys.txt", ((char)Arg1).ToString());
			}
		}

		bool Step()
		{
			Console.Write("{0:X4}: ", Regs.IP);

			FishInst Inst = (FishInst)ReadByteFromIP();
			Console.WriteLine("{0}", Inst);

			switch (Inst)
			{
				case FishInst.NOP:
					{
						break;
					}

				case FishInst.HALT:
					{
						return false;
					}

				case FishInst.INVALID:
					throw new Exception("Invalid instruction");

				case FishInst.PUSH_REG:
					{
						Reg R = (Reg)ReadByteFromIP();
						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);

						uint RVal = Regs.Read(R);
						WriteBytes(WriteAddr, BitConverter.GetBytes(RVal));
						Regs.Write(Reg.ESP, WriteAddr);

						Console.ForegroundColor = ConsoleColor.DarkYellow;
						Console.WriteLine("Push ({0}) {1} to {2}", R, RVal, WriteAddr);
						Console.ResetColor();

						break;
					}

				case FishInst.POP_REG:
					{
						Reg R = (Reg)ReadByteFromIP();
						uint ESP = Regs.Read(Reg.ESP);
						uint RegVal = ReadUInt32(ESP);
						Regs.Write(R, RegVal);

						Regs.Write(Reg.ESP, ESP + sizeof(uint));

						Console.ForegroundColor = ConsoleColor.DarkYellow;
						Console.WriteLine("Pop ({0}) new {1} from {2}", R, RegVal, ESP);
						Console.ResetColor();
						break;
					}

				case FishInst.MUL_REG:
					{
						Reg R = (Reg)ReadByteFromIP();
						uint RegVal = Regs.Read(R);
						uint AX = Regs.Read(Reg.AX);
						uint Mul = RegVal * AX;
						Regs.Write(Reg.AX, Mul);

						break;
					}

				case FishInst.IMUL_REG:
					{
						Reg R = (Reg)ReadByteFromIP();
						int RegVal = (int)Regs.Read(R);
						int AX = (int)Regs.Read(Reg.AX);
						int Mul = RegVal * AX;
						Regs.Write(Reg.AX, (uint)Mul);

						break;
					}

				case FishInst.PUSH_LONG:
					{
						uint Val = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);
						WriteBytes(WriteAddr, BitConverter.GetBytes(Val));
						Regs.Write(Reg.ESP, WriteAddr);
						Console.ForegroundColor = ConsoleColor.DarkYellow;
						Console.WriteLine("Push long {0} to {1}", Val, WriteAddr);
						Console.ResetColor();
						break;
					}
				case FishInst.MOVEBYTE_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();
						byte Val = (byte)(Regs.Read(R1) & 0xFF);
						Regs.Write(R2, Val);
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Move byte from {0} to {1}: {2:X2}", R1, R2, Val);
						Console.ResetColor();
						break;
					}
				case FishInst.LEA_ADDR_REG:
					{
						uint Addr = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();
						Regs.Write(R2, Addr);
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("LEA_ADDR_REG: Write address {0:X8} to {1}", Addr, R2);
						Console.ResetColor();
						break;
					}
				case FishInst.LEA_OFFSET_REG_REG:
					{
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();
						uint Addr = (uint)(Regs.Read(R1) + Offset);
						Regs.Write(R2, Addr);
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("LEA_OFFSET_REG_REG: Write address {0:X8} to {1}", Addr, R2);
						Console.ResetColor();
						break;
					}
				case FishInst.MOVEZ_REG_REG:
				case FishInst.MOVES_REG_REG:
				case FishInst.MOVE_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

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
							R1Val = (Regs.Read(R1) & 0xFF);
						}

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.TEST_REG_REG:
					{
						Reg R2 = (Reg)ReadByteFromIP();
						Reg R1 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						int Result = (int)R1Val - (int)R2Val;

						Regs.LessThan = R1Val < R2Val;
						Regs.Equal = R1Val == R2Val;
						Regs.IsZero = (R1Val & R2Val) == 0;
						Regs.GreaterThan = R1Val > R2Val;
						Regs.Sign = Result < 0;

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Test ({0}) {1}; 0x{1:X} and ({2}) {3}; 0x{3:X}", R1, R1Val, R2, R2Val);
						Console.WriteLine("IsZero = {0}, Sign = {1}, GreaterThan = {2}, Equal = {3}, LessThan = {4}", Regs.IsZero, Regs.Sign, Regs.GreaterThan, Regs.Equal, Regs.LessThan);
						Console.ResetColor();
						break;
					}

				case FishInst.CMP_REG_REG:
					{
						Reg R2 = (Reg)ReadByteFromIP();
						Reg R1 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						int Result = (int)R1Val - (int)R2Val;
						Regs.Write(R2, (uint)Result);

						Regs.LessThan = R1Val < R2Val;
						Regs.Equal = R1Val == R2Val;
						Regs.IsZero = Result == 0;
						Regs.GreaterThan = R1Val > R2Val;
						Regs.Sign = Result < 0;

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Cmp ({0}) {1}; 0x{1:X} and ({2}) {3}; 0x{3:X}", R1, R1Val, R2, R2Val);
						Console.WriteLine("IsZero = {0}, Sign = {1}, GreaterThan = {2}, Equal = {3}, LessThan = {4}", Regs.IsZero, Regs.Sign, Regs.GreaterThan, Regs.Equal, Regs.LessThan);
						Console.ResetColor();

						break;
					}

				case FishInst.SETNOTEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Val = !Regs.Equal ? (uint)1 : (uint)0;
						Regs.Write(R1, Val);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
						Console.ResetColor();
						break;
					}

				case FishInst.SETEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Val = Regs.Equal ? (uint)1 : (uint)0;
						Regs.Write(R1, Val);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
						Console.ResetColor();
						break;
					}

				case FishInst.SETGREATEREQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Val = Regs.GreaterThan || Regs.Equal ? (uint)1 : (uint)0;
						Regs.Write(R1, Val);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
						Console.ResetColor();
						break;
					}

				case FishInst.SETGREATER_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Val = Regs.GreaterThan ? (uint)1 : (uint)0;
						Regs.Write(R1, Val);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
						Console.ResetColor();
						break;
					}

				case FishInst.SETLESS_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Val = Regs.LessThan ? (uint)1 : (uint)0;
						Regs.Write(R1, Val);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
						Console.ResetColor();
						break;
					}

				case FishInst.SETLESSEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Val = Regs.LessThan || Regs.Equal ? (uint)1 : (uint)0;
						Regs.Write(R1, Val);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Write to {0} value {1}; 0x{1:X}", R1, Val);
						Console.ResetColor();
						break;
					}

				case FishInst.MOVEBYTE_REG_OFFSET_REG:
				case FishInst.MOVE_REG_OFFSET_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						uint Addr = (uint)(Regs.Read(R2) + Offset);

						if (Inst == FishInst.MOVE_REG_OFFSET_REG)
						{
							byte[] WriteVal = BitConverter.GetBytes(R1Val);
							WriteBytes(Addr, WriteVal);

							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Wrote bytes ({5:X8}; {5}) {{ {0:X2} {1:X2} {2:X2} {3:X2} }} to {4:X4}", WriteVal[0], WriteVal[1], WriteVal[2], WriteVal[3], Addr, R1Val);
							Console.ResetColor();
						}
						else
						{
							byte WriteB = (byte)(R1Val & 0xFF);
							WriteByte(Addr, WriteB);

							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Wrote byte {0:X2} to {1:X4}", WriteB, Addr);
							Console.ResetColor();
						}
						//Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVEZ_OFFSET_REG_REG:
					{
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Zero extend word from memory
						byte[] wordBytes = ReadBytes(Addr, 2);
						ushort wordVal = BitConverter.ToUInt16(wordBytes, 0);
						uint R1Val = wordVal;

						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Read zero-extended word {0:X4} from {1:X4} -> {2:X8}", wordVal, Addr, R1Val);
						Console.ResetColor();

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVES_OFFSET_REG_REG:
					{
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Sign extend byte from memory
						byte byteVal = ReadByte(Addr);
						uint R1Val = (uint)(sbyte)byteVal;

						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Read sign-extended byte {0:X2} ({1}) from {2:X4} -> {3:X8}", byteVal, (sbyte)byteVal, Addr, R1Val);
						Console.ResetColor();

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVE_OFFSET_REG_REG:
					{
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Regular 32-bit read
						uint R1Val = ReadUInt32(Addr);
						byte[] ReadVal = BitConverter.GetBytes(R1Val);

						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Read bytes ({5:X8}; {5}) {{ {0:X2} {1:X2} {2:X2} {3:X2} }} from {4:X4}", ReadVal[0], ReadVal[1], ReadVal[2], ReadVal[3], Addr, R1Val);
						Console.ResetColor();

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.SUB_LONG_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						uint RVal = Regs.Read(R2);
						Regs.Write(R2, RVal - L1);
						break;
					}

				case FishInst.SUB_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine("Sub ({0}) {1}, ({2}) {3} = {4}", R1, R1Val, R2, R2Val, R2Val - R1Val);
						Console.ResetColor();

						Regs.Write(R2, R2Val - R1Val);
						break;
					}

				case FishInst.ADD_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine("Add ({0}) {1}, ({2}) {3} = {4}", R1, R1Val, R2, R2Val, R1Val + R2Val);
						Console.ResetColor();

						Regs.Write(R2, R1Val + R2Val);
						break;
					}

				case FishInst.ADD_LONG_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						uint Result = L1 + Regs.Read(R2);
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.MOVE_LONG_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						Regs.Write(R2, L1);
						break;
					}

				case FishInst.MOVEZ_LONG_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						// Zero extend the lower 16 bits of the immediate value
						uint Result = L1 & 0xFFFF;
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.MOVES_LONG_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						// Sign extend the lower 8 bits of the immediate value
						byte byteVal = (byte)(L1 & 0xFF);
						uint Result = (uint)(sbyte)byteVal;
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.CMP_LONG_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						uint R2Val = Regs.Read(R2);
						int Result = (int)R2Val - (int)L1;

						Regs.LessThan = R2Val < L1;
						Regs.Equal = R2Val == L1;
						Regs.IsZero = Result == 0;
						Regs.GreaterThan = R2Val > L1;
						Regs.Sign = Result < 0;

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("Cmp ({0}) {1}; 0x{1:X} and immediate {2}; 0x{2:X}", R2, R2Val, L1);
						Console.WriteLine("IsZero = {0}, Sign = {1}, GreaterThan = {2}, Equal = {3}, LessThan = {4}", Regs.IsZero, Regs.Sign, Regs.GreaterThan, Regs.Equal, Regs.LessThan);
						Console.ResetColor();

						break;
					}

				case FishInst.CALL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint RetAddr = Regs.IP;
						uint Addr = Regs.Read(R1);


						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);
						WriteBytes(WriteAddr, BitConverter.GetBytes(RetAddr));

						Regs.Write(Reg.ESP, WriteAddr);
						Jump(Addr);
						break;
					}

				case FishInst.CALL_LONG:
					{
						uint Addr = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						uint RetAddr = Regs.IP;

						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);
						WriteBytes(WriteAddr, BitConverter.GetBytes(RetAddr));

						Regs.Write(Reg.ESP, WriteAddr);
						Jump(Addr);
						break;
					}

				case FishInst.RET:
					{
						uint ESP = Regs.Read(Reg.ESP);
						uint RetAddr = ReadUInt32(ESP);
						Regs.Write(Reg.ESP, ESP + sizeof(uint));
						Jump(RetAddr);
						break;
					}

				case FishInst.SYSCALL_2:
					{
						uint ESP = Regs.Read(Reg.ESP);
						uint SyscallNum = ReadUInt32(ESP);
						uint Arg1 = ReadUInt32(ESP + 4);
						Syscall(SyscallNum, Arg1);
						break;
					}

				case FishInst.SYSCALL:
					{
						uint SyscallNum = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Syscall(SyscallNum, 0);
						break;
					}

				case FishInst.JUMP_IF_NOT_ZERO_LONG:
				case FishInst.JUMP_IF_ZERO_LONG:
				case FishInst.JUMP_LONG:
					{
						uint Addr = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });

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
						break;
					}

				case FishInst.JUMP_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						uint Addr = Regs.Read(R1);
						Jump(Addr);
						break;
					}

				case FishInst.LEAVE:
					{
						// Restore ESP from EBP
						Regs.Write(Reg.ESP, Regs.Read(Reg.EBP));

						// Pop EBP
						uint ESP = Regs.Read(Reg.ESP);
						uint RegVal = ReadUInt32(ESP);
						Regs.Write(Reg.EBP, RegVal);

						Regs.Write(Reg.ESP, ESP + sizeof(uint));
						break;
					}

				case FishInst.DBG_BREAK:
					{
						/*if (Debugger.IsAttached)
							Debugger.Break();*/

						Console.ReadLine();
						break;
					}


				default:
					throw new Exception(string.Format("Unknown instruction {0}", Inst));
			}

			return true;
		}

		public void Run()
		{
			Halted = false;

			while (Step())
			{
				if (Halted)
					break;
			}
		}
	}
}
