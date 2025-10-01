using CodeGeneration;
using System;
using System.Collections.Generic;
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
		LEAVE,
		RET,

		JUMP_REG,
		JUMP_LONG,

		CALL_REG,
		CALL_LONG,

		PUSH_REG,
		PUSH_LONG,

		MOVE_REG_REG,
		MOVE_LONG_REG,
		MOVE_OFFSET_REG_REG,
		MOVE_REG_OFFSET_REG,

		SUB_LONG_REG,
		SUB_REG_REG,

		ADD_LONG_REG,
		ADD_REG_REG,

		LEA_ADDR_REG,
		LEA_OFFSET_REG_REG,
	}

	public struct FishRegisters
	{
		public uint[] Regs;
		public uint IP;

		public uint Read(CodeGeneration.Reg Reg)
		{
			return Regs[(int)Reg];
		}

		public void Write(CodeGeneration.Reg Reg, uint Val)
		{
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
				case FishInst.LEAVE:
				case FishInst.RET:
					return 1;

				// One register, 2 byte total
				case FishInst.PUSH_REG:
				case FishInst.JUMP_REG:
				case FishInst.CALL_REG:
					return 2;

				// Two registers, 3 byte total
				case FishInst.ADD_REG_REG:
				case FishInst.MOVE_REG_REG:
					return 3;

				// One 32-bit operand, 5 byte total
				case FishInst.JUMP_LONG:
				case FishInst.PUSH_LONG:
				case FishInst.CALL_LONG:
					return 5;

				// One 32-bit operand, one 8-bit operand, 6 byte total
				case FishInst.LEA_ADDR_REG:
				case FishInst.SUB_LONG_REG:
				case FishInst.ADD_LONG_REG:
				case FishInst.MOVE_LONG_REG:
					return 6;

				// One 32 bit operand, two 8-bit operands, 7 byte total
				case FishInst.MOVE_REG_OFFSET_REG:
				case FishInst.LEA_OFFSET_REG_REG:
					return 7;

				default:
					throw new InvalidProgramException("Invalid instruction");
			}
		}

		byte[] Memory;

		public FishRegisters Regs = new FishRegisters();

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
			Console.WriteLine("IP = 0x{0:X4}", Regs.IP);
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

				case FishInst.INVALID:
					throw new Exception("Invalid instruction");

				case FishInst.PUSH_REG:
					{
						Reg R = (Reg)ReadByteFromIP();
						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);
						WriteBytes(WriteAddr, BitConverter.GetBytes(Regs.Read(R)));
						Regs.Write(Reg.ESP, WriteAddr);
						break;
					}

				case FishInst.MOVE_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVE_REG_OFFSET_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP();
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();

						uint R1Val = Regs.Read(R1);
						uint Addr = (uint)(Regs.Read(R2) + Offset);
						WriteBytes(Addr, BitConverter.GetBytes(R1Val));
						//Regs.Write(R2, R1Val);
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

				case FishInst.LEA_ADDR_REG:
					{
						uint L1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R2 = (Reg)ReadByteFromIP();
						Regs.Write(R2, L1);
						break;
					}

				case FishInst.LEA_OFFSET_REG_REG:
					{
						int Offset = BitConverter.ToInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() });
						Reg R1 = (Reg)ReadByteFromIP();
						Reg R2 = (Reg)ReadByteFromIP();

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						Regs.Write(R2, Addr);
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

				default:
					throw new Exception(string.Format("Unknown instruction {0}", Inst));
			}

			return true;
		}

		public void Run()
		{
			while (Step()) ;
		}
	}
}
