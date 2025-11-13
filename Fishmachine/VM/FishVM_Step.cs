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
		Reg[] saveRegs = new[] { Reg.EAX, Reg.EBX, Reg.ECX, Reg.EDX, Reg.ESI, Reg.EDI, Reg.XSC, Reg.XR1, Reg.EBP };

		bool Step(ref FishStackTrace E)
		{
			E.SetException(this, FishExcept.None);

			// Handle interrupts
			FishInterrupt IntNum = (FishInterrupt)Regs.Read(Reg.XSC);
			if (IntNum != FishInterrupt.None)
			{
				Regs.Write(Reg.XSC, 0);
				// Handle interrupt

				uint IntTable = ReadUInt32(IntTableAddr, FishMemPriv.Read, ref E);
				if (!E.Is(FishExcept.None))
					return true;

				uint IntAddr = ReadUInt32(IntTable + (uint)(IntNum - 1) * 4, FishMemPriv.Read, ref E);
				if (!E.Is(FishExcept.None))
					return true;

				if (IntAddr != 0)
				{
					// Preserve EFLAGS
					IRETStack.Push((Regs.IP, IntNum));
					if (PushReg(Reg.RFLAGS, ref E))
						return true;

					// IntEnabled is restored by popping RFLAGS below, as it is just a bit field in RFLAGS
					Regs.IntEnabled = false;

					// Push interrupt arguments
					switch (IntNum)
					{
						case FishInterrupt.Int1_KeyboardKey:
						case FishInterrupt.Int2_KeyboardChar:
							if (PushReg(Reg.XR1, ref E))
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

					CallLong(IntAddr, ref E);
					return true;
				}
			}

			if (FishSettings.DebugPrint || FishSettings.DebugPrintInstruction || FishSettings.DebugPrintIP)
				Console.Write("IP 0x{0:X}: ", Regs.IP);

			if (IRETStack.Count > 0 && IRETStack.TryPeek(out (uint, FishInterrupt) IRETAddr) && IRETAddr.Item1 == Regs.IP)
			{
				switch (IRETAddr.Item2)
				{
					case FishInterrupt.Int1_KeyboardKey:
					case FishInterrupt.Int2_KeyboardChar:
						if (PopReg(Reg.XR1, ref E))
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

				if (PopReg(Reg.RFLAGS, ref E))
					return true;

				IRETStack.Pop();
			}


			CurrentInstructionIP = Regs.IP;
			FishInst Inst = (FishInst)ReadByteFromIP(ref E);
			if (!E.Is(FishExcept.None))
				return true;

			E.Clear();
			E.SetException(this, FishExcept.None);
			CurrentInstruction = Inst;

			if (FishSettings.DebugPrint || FishSettings.DebugPrintInstruction)
				Console.WriteLine("{0}", Inst);

			switch (Inst)
			{
				case FishInst.NOP:
					{
						Console.PrintInst(Inst);
						break;
					}

				case FishInst.WAIT:
					{
						Console.PrintInst(Inst);
						E.SetException(this, FishExcept.RequestWait);
						return true;
					}

				case FishInst.HALT:
					{
						Console.PrintInst(Inst);
						return false;
					}

				case FishInst.SOFTINT_ENABLE:
					{
						Console.PrintInst(Inst);

						if (FishSettings.DebugPrintSyscall)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Software interrupts enabled");
							Console.ResetColor();
						}
						Regs.SoftIntEnabled = true;
						break;
					}

				case FishInst.SOFTINT_DISABLE:
					{
						Console.PrintInst(Inst);

						if (FishSettings.DebugPrintSyscall)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Software interrupts disabled");
							Console.ResetColor();
						}
						Regs.SoftIntEnabled = false;
						break;
					}

				case FishInst.FLOAT_PUSH_REG:
					{
						Reg R = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R);
						E.SetParams(R);


						float Val = BitConverter.ToSingle(BitConverter.GetBytes(Regs.Read(R)));
						Regs.FpuPush(Val);

						break;
					}

				case FishInst.FLOAT_POP_REG:
					{
						Reg R = (Reg)ReadByteFromIP(ref E);

						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R);
						E.SetParams(R);

						float Val = Regs.FpuPop();
						uint UVal = BitConverter.ToUInt32(BitConverter.GetBytes(Val));

						Regs.Write(R, UVal);

						break;
					}

				case FishInst.PUSH_REG:
					{
						Reg R = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R);
						E.SetParams(R);

						if (PushReg(R, ref E))
							return true;

						break;
					}

				case FishInst.POP_REG:
					{
						Reg R = (Reg)ReadByteFromIP(ref E);

						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R);
						E.SetParams(R);

						if (PopReg(R, ref E))
							return true;

						break;
					}

				case FishInst.MUL_REG:
					{
						Reg R = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R);
						E.SetParams(R);

						uint RegVal = Regs.Read(R);
						uint AX = Regs.Read(Reg.AX);
						uint Mul = RegVal * AX;
						Regs.Write(Reg.AX, Mul);

						break;
					}

				case FishInst.IMUL_REG:
					{
						Reg R = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R);
						E.SetParams(R);

						int RegVal = (int)Regs.Read(R);
						int AX = (int)Regs.Read(Reg.AX);
						int Mul = RegVal * AX;
						Regs.Write(Reg.AX, (uint)Mul);

						break;
					}

				case FishInst.PUSH_LONG:
					{
						uint Val = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Val);
						E.SetParams(Val);

						uint ESP = Regs.Read(Reg.ESP);
						uint WriteAddr = ESP - sizeof(uint);

						WriteBytes(WriteAddr, BitConverter.GetBytes(Val), FishMemPriv.Stack, ref E);
						if (!E.Is(FishExcept.None))
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
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

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
						uint Addr = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Addr, R2);
						E.SetParams(Addr, R2);

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
						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Offset, R1, R2);
						E.SetParams(Offset, R1, R2);

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
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

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
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						int Result = (int)R1Val & (int)R2Val;

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

				case FishInst.BINAND_REG_REG:
				case FishInst.BINOR_REG_REG:
				case FishInst.BOLOR_REG_REG:
				case FishInst.BINXOR_REG_REG:
				case FishInst.BOLAND_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						switch (Inst)
						{
							case FishInst.BOLOR_REG_REG:
								Regs.Write(R2, ((R1Val != 0) || (R2Val != 0)) ? 1u : 0u);
								break;

							case FishInst.BOLAND_REG_REG:
								Regs.Write(R2, ((R1Val != 0) && (R2Val != 0)) ? 1u : 0u);
								break;

							case FishInst.BINXOR_REG_REG:
								Regs.Write(R2, R1Val ^ R2Val);
								break;

							case FishInst.BINOR_REG_REG:
								Regs.Write(R2, R1Val | R2Val);
								break;

							case FishInst.BINAND_REG_REG:
								Regs.Write(R2, R1Val & R2Val);
								break;

							default:
								throw new NotImplementedException();
						}


						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("{4} ({0}) {1}; 0x{1:X} and ({2}) {3}; 0x{3:X}", R1, R1Val, R2, R2Val, Inst);
							Console.ResetColor();
						}
						break;
					}

				case FishInst.CMP_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						int Result = (int)R1Val - (int)R2Val;
						//Regs.Write(R2, (uint)Result);

						Regs.LessThan = R1Val < R2Val;
						Regs.Equal = R1Val == R2Val;
						Regs.IsZero = Result == 0;
						Regs.GreaterThan = R1Val > R2Val;
						Regs.Sign = Result < 0;

						Regs.Write(R2, Regs.Equal ? 1u : 0u);

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
				case FishInst.SETEQUAL_REG:
				case FishInst.SETGREATER_REG:
				case FishInst.SETGREATEREQUAL_REG:
				case FishInst.SETLESS_REG:
				case FishInst.SETLESSEQUAL_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None)) return true;

						Console.PrintInst(Inst, R1);
						E.SetParams(R1);

						uint Val = EvaluateSetCondition(Inst);
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
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, Offset, R2);
						E.SetParams(R1, Offset, R2);

						uint R1Val = Regs.Read(R1);
						uint Addr = (uint)(Regs.Read(R2) + Offset);

						if (Inst == FishInst.MOVE_REG_OFFSET_REG)
						{
							byte[] WriteVal = BitConverter.GetBytes(R1Val);
							WriteBytes(Addr, WriteVal, FishMemProt.GetPriv(Addr, StackAddr, StackSize, false), ref E);
							if (!E.Is(FishExcept.None))
								return true;

							if (FishSettings.DebugPrint)
							{
								Console.ForegroundColor = ConsoleColor.Yellow;
								Console.WriteLine("Wrote bytes ({5:X8}; {5}) {{ {0:X2} {1:X2} {2:X2} {3:X2} }} to {4:X4}", WriteVal[0], WriteVal[1], WriteVal[2], WriteVal[3], Addr, R1Val);
								Console.ResetColor();
							}
						}
						else if (Inst == FishInst.MOVEBYTE_REG_OFFSET_REG)
						{
							byte WriteB = (byte)(R1Val & 0xFF);
							WriteByte(Addr, WriteB, FishMemProt.GetPriv(Addr, StackAddr, StackSize, false), ref E);
							if (!E.Is(FishExcept.None))
								return true;

							if (FishSettings.DebugPrint)
							{
								Console.ForegroundColor = ConsoleColor.Yellow;
								Console.WriteLine("Wrote byte {0:X2} to {1:X4}", WriteB, Addr);
								Console.ResetColor();
							}
						}
						else throw new NotImplementedException();

						//Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.MOVEBYTE_OFFSET_REG_REG:
				case FishInst.MOVEZ_OFFSET_REG_REG:
					{
						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Offset, R1, R2);
						E.SetParams(Offset, R1, R2);

						if (Inst == FishInst.MOVEZ_OFFSET_REG_REG)
						{
							uint Addr = (uint)(Regs.Read(R1) + Offset);
							// Zero extend word from memory
							byte[] wordBytes = ReadBytes(Addr, 2, FishMemProt.GetPriv(R1, true), ref E);
							if (!E.Is(FishExcept.None))
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
						}
						else if (Inst == FishInst.MOVEBYTE_OFFSET_REG_REG)
						{
							uint R1Val = Regs.Read(R1);
							uint ReadAddr = (uint)(R1Val + Offset);

							byte B = ReadByte(ReadAddr, FishMemProt.GetPriv(R1, true), ref E);
							if (!E.Is(FishExcept.None))
								return true;

							Regs.Write(R2, B);

							if (FishSettings.DebugPrint)
							{
								Console.ForegroundColor = ConsoleColor.Yellow;
								Console.WriteLine("Read byte 0x{0:X} from 0x{1:X4}", B, ReadAddr);
								Console.ResetColor();
							}
						}
						break;
					}

				case FishInst.MOVES_OFFSET_REG_REG:
					{
						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Offset, R1, R2);
						E.SetParams(Offset, R1, R2);

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Sign extend byte from memory
						byte byteVal = ReadByte(Addr, FishMemPriv.Read, ref E);
						if (!E.Is(FishExcept.None))
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
						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Offset, R1, R2);
						E.SetParams(Offset, R1, R2);

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						// Regular 32-bit read
						uint R1Val = ReadUInt32(Addr, FishMemProt.GetPriv(R1, true), ref E);
						if (!E.Is(FishExcept.None))
							return true;

						byte[] ReadVal = BitConverter.GetBytes(R1Val);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Read bytes (0x{5:X8}; {5}) {{ 0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2} }} from 0x{4:X4}", ReadVal[0], ReadVal[1], ReadVal[2], ReadVal[3], Addr, R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R1Val);
						break;
					}

				case FishInst.SUB_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, L1, R2);
						E.SetParams(L1, R2);

						uint RVal = Regs.Read(R2);
						Regs.Write(R2, RVal - L1);
						break;
					}

				case FishInst.SUB_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

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
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

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
						uint L1 = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, L1, R2);
						E.SetParams(L1, R2);

						uint Result = L1 + Regs.Read(R2);
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.MUL_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Magenta;
							Console.WriteLine("Sub ({0}) {1}, ({2}) {3} = {4}", R1, R1Val, R2, R2Val, R2Val * R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R2Val * R1Val);
						break;
					}

				case FishInst.DIV_REG_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1, R2);
						E.SetParams(R1, R2);

						uint R1Val = Regs.Read(R1);
						uint R2Val = Regs.Read(R2);

						if (FishSettings.DebugPrint)
						{
							Console.ForegroundColor = ConsoleColor.Magenta;
							Console.WriteLine("Sub ({0}) {1}, ({2}) {3} = {4}", R1, R1Val, R2, R2Val, R2Val / R1Val);
							Console.ResetColor();
						}

						Regs.Write(R2, R2Val / R1Val);
						break;
					}

				case FishInst.MOVE_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, L1, R2);
						E.SetParams(L1, R2);

						Regs.Write(R2, L1);
						break;
					}

				case FishInst.MOVEZ_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, L1, R2);
						E.SetParams(L1, R2);

						// Zero extend the lower 16 bits of the immediate value
						uint Result = L1 & 0xFFFF;
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.MOVES_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, L1, R2);
						E.SetParams(L1, R2);

						// Sign extend the lower 8 bits of the immediate value
						byte byteVal = (byte)(L1 & 0xFF);
						uint Result = (uint)(sbyte)byteVal;
						Regs.Write(R2, Result);
						break;
					}

				case FishInst.CMP_LONG_REG:
					{
						uint L1 = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R2 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, L1, R2);
						E.SetParams(L1, R2);

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
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1);
						E.SetParams(R1);

						uint Addr = Regs.Read(R1);

						if (CallLong(Addr, ref E))
							return true;

						//Jump(Addr);
						break;
					}

				case FishInst.CALL_LONG:
					{
						uint Addr = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Addr);
						E.SetParams(Addr);

						if (CallLong(Addr, ref E))
							return true;

						break;
					}

				case FishInst.RET:
					{
						Console.PrintInst(Inst);

						uint ESP = Regs.Read(Reg.ESP);
						uint RetAddr = ReadUInt32(ESP, FishMemPriv.Stack, ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Regs.Write(Reg.ESP, ESP + sizeof(uint));
						Jump(RetAddr);
						break;
					}

				case FishInst.SYSCALL_2:
					{
						Console.PrintInst(Inst);

						uint ESP = Regs.Read(Reg.ESP);
						uint SyscallNum = ReadUInt32(ESP, FishMemPriv.Stack, ref E);
						if (!E.Is(FishExcept.None))
							return true;

						uint Arg1 = ReadUInt32(ESP + 4, FishMemPriv.Stack, ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Syscall((FishSyscall)SyscallNum, Arg1, ref E);
						if (!E.Is(FishExcept.None))
							return true;

						// Consume the two arguments pushed before SYSCALL_2
						Regs.Write(Reg.ESP, ESP + 8);

						break;
					}

				case FishInst.SYSCALL:
					{
						uint SyscallNum = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, SyscallNum);
						E.SetParams(SyscallNum);

						Syscall((FishSyscall)SyscallNum, 0, ref E);
						if (!E.Is(FishExcept.None))
							return true;

						break;
					}

				case FishInst.JUMP_IF_NOT_ZERO_LONG:
				case FishInst.JUMP_IF_ZERO_LONG:
				case FishInst.JUMP_IF_LESS_LONG:
				case FishInst.JUMP_IF_GREAT_LONG:
				case FishInst.JUMP_IF_LESSEQ_LONG:
				case FishInst.JUMP_IF_GREATEQ_LONG:
				case FishInst.JUMP_LONG:
				case FishInst.FLOAT_LOAD_LONG:
					{
						uint Addr = ReadUInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Addr);
						E.SetParams(Addr);

						if (Inst == FishInst.FLOAT_LOAD_LONG)
						{
							float Val = BitConverter.ToSingle(ReadBytes(Addr, 4, FishMemPriv.Read, ref E));
							if (!E.Is(FishExcept.None))
								return true;

							Regs.FpuPush(Val);
						}
						else if (ShouldJump(Inst))
						{
							Jump(Addr);
						}

						break;
					}

				case FishInst.DOUBLE_STORE_OFFSET_REG:
				case FishInst.FLOAT_STORE_OFFSET_REG:
				case FishInst.DOUBLE_POP_OFFSET_REG:
				case FishInst.FLOAT_POP_OFFSET_REG:
					{
						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Offset, R1);
						E.SetParams(Offset, R1);

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

						WriteBytes(Addr, FBytes, FishMemPriv.Write, ref E);
						if (!E.Is(FishExcept.None))
							return true;

						break;
					}

				case FishInst.DOUBLE_LOAD_OFFSET_REG:
				case FishInst.FLOAT_LOAD_OFFSET_REG:
					{
						int Offset = ReadInt32FromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, Offset, R1);
						E.SetParams(Offset, R1);

						uint Addr = (uint)(Regs.Read(R1) + Offset);
						byte[] FBytes = ReadBytes(Addr, 4, FishMemPriv.Read, ref E);
						if (!E.Is(FishExcept.None))
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

						Console.PrintInst(Inst, string.Format("(FPU STACK {0}, {1})", Val1, Val2));
						E.SetParams(Val1, Val2);

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
							E.SetException(this, FishExcept.FloatInfinity);
							return true;
						}

						if (float.IsNaN(Result))
						{
							E.SetException(this, FishExcept.FloatNaN);
							return true;
						}

						Regs.FpuPush(Result);
						break;
					}

				case FishInst.JUMP_REG:
					{
						Reg R1 = (Reg)ReadByteFromIP(ref E);
						if (!E.Is(FishExcept.None))
							return true;

						Console.PrintInst(Inst, R1);
						E.SetParams(R1);

						uint Addr = Regs.Read(R1);
						Jump(Addr);
						break;
					}

				case FishInst.LEAVE:
					{
						Console.PrintInst(Inst);

						if (Leave(ref E))
							return true;

						break;
					}

				case FishInst.DBG_MEM:
					{
						Console.PrintInst(Inst);
						PrintMem(StackAddr, ref E);
						break;
					}

				case FishInst.DBG_REGS:
					{
						Console.PrintInst(Inst);
						Regs.PrintAll(true);
						break;
					}

				case FishInst.DBG_BREAK:
					{
						Console.PrintInst(Inst);
						PrintMem(StackAddr, ref E);
						Regs.PrintAll();

						Debugger.Break();
						break;
					}


				default:
					{
						E.SetException(this, FishExcept.InvalidInstruction);
						return true;
					}
			}

			return true;
		}
	}
}
