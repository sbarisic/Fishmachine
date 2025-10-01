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

		public uint Read(CodeGeneration.Reg Reg)
		{
			return Regs[(int)Reg];
		}

		public void Write(CodeGeneration.Reg Reg, uint Val)
		{
			Regs[(int)Reg] = Val;
		}

		public uint IP;

		public FishRegisters()
		{
			Regs = new uint[24];
		}
	}

	public struct CurInstruction
	{
		public FishInst Inst;
		public uint Op1;
		public uint Op2;
		public uint Op3;
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

		FishRegisters Regs = new FishRegisters();
		CurInstruction CurInstr;

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

		public void WriteByte(uint Address, byte Value)
		{
			Address = VirtualToReal(Address);
			MemoryBankForRealAddress(Address)[Address] = Value;
		}

		public void Jump(uint Address)
		{
			Regs.IP = Address;
		}

		void Fetch()
		{
			Console.WriteLine("IP = {0}", Regs.IP);

			CurInstr = new CurInstruction();
			CurInstr.Inst = (FishInst)ReadByteFromIP();

			switch (CurInstr.Inst)
			{
				// Single register, 2 byte total
				case FishInst.PUSH_REG:
					CurInstr.Reg1 = ReadByteFromIP();
					break;

				// Two registers, 3 byte total
				case FishInst.MOVE_REG_REG:
					CurInstr.Reg1 = ReadByteFromIP();
					CurInstr.Reg2 = ReadByteFromIP();
					break;

				// Single 32-bit operand, 5 byte total
				case FishInst.JUMP_LONG:
				case FishInst.PUSH_LONG:
					CurInstr.Operand1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() }, 0);
					break;

				// Two 32-bit operands, 9 byte total
				case FishInst.MOVE_LONG_REG:
					CurInstr.Operand1 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() }, 0);
					CurInstr.Operand2 = BitConverter.ToUInt32(new byte[] { ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP(), ReadByteFromIP() }, 0);
					break;
			}

			Console.WriteLine("FETCH {0} Op1 = {1:X4} Op2 = {2:X4} Op3 = {3:X4}", CurInstr.Inst, CurInstr.Op1, CurInstr.Op2, CurInstr.Op3);
		}

		void Decode()
		{

		}

		void Execute()
		{
			switch (CurInstr.Inst)
			{
				case FishInst.NOP:
					break;



				default:
					throw new InvalidProgramException("Invalid instruction");
			}
		}

		public bool Step()
		{
			Fetch();
			Decode();
			Execute();

			return true;
		}

		public void Run()
		{
			while (Step()) ;
		}
	}
}
