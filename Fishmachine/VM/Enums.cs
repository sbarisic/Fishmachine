using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine.VM
{
	public enum FishException : byte
	{
		None = 0,
		InvalidInstruction,
		DivisionByZero,
		AccessViolation,
		StackOverflow,
		StackUnderflow,
		FloatInfinity,
		FloatNaN,
		RequestWait,
	}

	public enum FishSyscall : byte
	{
		StopMachine,
		PrintChar,
		PrintNum,
		SoftwareInterrupt,
		Alloc,
	}

	public enum FishInterrupt : byte
	{
		None,
		Int0,
		Int1_KeyboardKey,
		Int2_KeyboardChar,
		Int3,
	}

	public enum FishInst : byte
	{
		INVALID = 0,

		NOP,
		HALT,
		WAIT,
		LEAVE,
		RET,
		DBG_BREAK,
		SYSCALL,
		SYSCALL_2,

		JUMP_REG,
		JUMP_LONG,

		JUMP_IF_ZERO_LONG,
		JUMP_IF_NOT_ZERO_LONG,
		JUMP_IF_LESS_LONG,
		JUMP_IF_GREAT_LONG,
		JUMP_IF_LESSEQ_LONG,
		JUMP_IF_GREATEQ_LONG,

		FLOAT_ADD,
		FLOAT_SUB,
		FLOAT_MUL,
		FLOAT_DIV,

		FLOAT_LOAD_LONG,
		DOUBLE_LOAD_LONG,

		FLOAT_LOAD_OFFSET_REG,
		FLOAT_STORE_OFFSET_REG,
		FLOAT_POP_OFFSET_REG,
		DOUBLE_LOAD_OFFSET_REG,
		DOUBLE_STORE_OFFSET_REG,
		DOUBLE_POP_OFFSET_REG,

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
		MOVEBYTE_OFFSET_REG_REG,
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
}
