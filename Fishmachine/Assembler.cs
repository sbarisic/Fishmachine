using CodeGeneration;
using Fishmachine.CTilde;
using Fishmachine.VM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine
{
	public class AsmToken
	{
		public string Name;
		public bool Global = false;
		public bool IsVariable = false;
		public uint Address = 0;
		public int AsmLine;

		public AsmToken()
		{

		}

		public uint ResolveAddress(AsmToken AT)
		{
			if (Address == 0)
			{
				throw new ExprException(AT, string.Format("Linker error: could not resolve symbol '{0}'", Name));
			}

			return Address;
		}

		public override string ToString()
		{
			return string.Format("[line {0}] {1} @ {2} ({3})", AsmLine, Name, Address, ((Global ? "glob " : "") + (IsVariable ? "var " : "")).Trim());
		}
	}

	public class AsmInstr
	{
		public uint Address;

		public FishInst Inst;

		uint Op1;
		uint Op2;
		uint Op3;

		int Op1Size;
		int Op2Size;
		int Op3Size;

		bool HasOp1 = false;
		bool HasOp2 = false;
		bool HasOp3 = false;

		public AsmToken Op1Token { get; private set; }

		public AsmToken Op2Token { get; private set; }

		public AsmToken Op3Token { get; private set; }

		public byte[] Raw;
		public bool IsAlign = false;
		public int AlignSize = 0;
		int AlignOffset = 0;

		public AsmInstr(FishInst Inst)
		{
			this.Inst = Inst;
		}

		public void SetOp1(uint Val, int Size)
		{
			Op1 = Val;
			Op1Size = Size;
			HasOp1 = true;
			Op1Token = null;
		}

		public void SetOp1(AsmToken Val, int Size)
		{
			Op1Token = Val;
			Op1Size = Size;
			HasOp1 = true;
		}

		public void SetOp2(uint Val, int Size)
		{
			Op2 = Val;
			Op2Size = Size;
			HasOp2 = true;
			Op2Token = null;
		}

		public void SetOp2(AsmToken Val, int Size)
		{
			Op2Token = Val;
			Op2Size = Size;
			HasOp2 = true;
		}

		public void SetOp3(uint Val, int Size)
		{
			Op3 = Val;
			Op3Size = Size;
			HasOp3 = true;
			Op3Token = null;
		}

		public void SetOp3(AsmToken Val, int Size)
		{
			Op3Token = Val;
			Op3Size = Size;
			HasOp3 = true;
		}

		public void WriteBytes(BinaryWriter BW)
		{
			if (IsAlign && AlignSize != 0)
			{
				BW.Write(new byte[AlignOffset]);
				return;
			}

			if (Raw != null)
			{
				//if (HasOp1 || HasOp2 || HasOp3)
				//	throw new Exception("Cannot have raw data and operands in the same instruction");
				if (HasOp1)
				{
					byte[] Op1Bytes = BitConverter.GetBytes(Op1);
					Array.Copy(Op1Bytes, 0, Raw, 0, Op1Bytes.Length);
				}

				if (HasOp2)
				{
					byte[] Op2Bytes = BitConverter.GetBytes(Op2);
					Array.Copy(Op2Bytes, 0, Raw, 4, Op2Bytes.Length);
				}

				if (HasOp3)
				{
					byte[] Op3Bytes = BitConverter.GetBytes(Op3);
					Array.Copy(Op3Bytes, 0, Raw, 8, Op3Bytes.Length);
				}

				BW.Write(Raw);
			}
			else
			{
				BW.Write((byte)Inst);

				if (HasOp1)
					BW.Write(BitConverter.GetBytes(Op1), 0, Op1Size);

				if (HasOp2)
					BW.Write(BitConverter.GetBytes(Op2), 0, Op2Size);

				if (HasOp3)
					BW.Write(BitConverter.GetBytes(Op3), 0, Op3Size);
			}
		}

		public int Size(uint CurAddr)
		{
			if (IsAlign && AlignSize != 0)
			{
				int Offset = 0;

				while ((CurAddr % AlignSize) != 0)
				{
					CurAddr++;
					Offset++;
				}

				AlignOffset = Offset;
				return Offset;
			}

			if (Raw != null)
			{
				return Raw.Length;
			}

			return FishVM.FishInstSize(Inst);
		}

		public override string ToString()
		{
			if (Raw != null)
			{
				return string.Format("{0:X4}: {1}", Address, string.Join(" ", Raw.Select(R => R.ToString("X2"))));
			}

			return string.Format("{4:X4}: {0} {1}, {2}, {3}", Inst, Op1Token?.Name ?? Op1.ToString(), Op2Token?.Name ?? Op2.ToString(), Op3Token?.Name ?? Op3.ToString(), Address);
		}
	}

	public class AssemblerState
	{
		List<AsmToken> Tokens = new List<AsmToken>();

		public AsmToken RefToken(int AsmLine, string TokenName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == TokenName).FirstOrDefault();

			if (Tok == null)
			{
				Tok = new AsmToken();
				Tok.AsmLine = AsmLine;
				Tok.Name = TokenName;
				Tokens.Add(Tok);
			}

			return Tok;
		}

		public void ClearLocalTokens()
		{
			Tokens.RemoveAll(T => !T.Global);
		}

		public AsmToken DefineToken(int AsmLine, string TokenName, uint Addr, bool Global, bool ReassignAddress = true)
		{
			AsmToken Tok = RefToken(AsmLine, TokenName);

			if (!Tok.Global)
				Tok.Global = Global;

			if (ReassignAddress)
				Tok.Address = Addr;

			return Tok;
		}

		public AsmToken FindToken(string TokenName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == TokenName).FirstOrDefault();
			return Tok;
		}

		public AsmToken[] GetAllGlobals()
		{
			return Tokens.Where(T => T.Global).ToArray();
		}

		public AsmToken[] GetGlobalVariables()
		{
			return GetAllGlobals().Where(G => G.IsVariable).ToArray();
		}

		public uint GetSymbolOffset(string SymbolName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == SymbolName).FirstOrDefault();

			if (Tok == null)
				throw new Exception(string.Format("Unresolved symbol '{0}'", SymbolName));

			return Tok.Address;
		}
	}

	public class Assembler
	{
		List<AsmInstr> Assembly = new List<AsmInstr>();
		uint CurAddr = 0;

		public Assembler(uint Address)
		{
			CurAddr = Address;
		}

		void Throw(int Line, string Msg)
		{
			throw new Exception(string.Format("{0}: {1}", Line, Msg));
		}

		Reg ParseReg(int Line, string Token)
		{
			foreach (var kv in CGenState.reg_strs)
			{
				if (Token == kv.Value)
					return kv.Key;
			}

			Throw(Line, $"Unknown register: {Token}");
			return Reg.EAX;
		}

		void AddAsmInstr(AsmInstr Instr)
		{
			Instr.Address = CurAddr;
			Assembly.Add(Instr);
			CurAddr += (uint)Instr.Size(CurAddr);
		}

		bool TryParseToken(int AsmLine, AssemblerState state, string TokenStr, out AsmToken Token, out uint Value)
		{
			Token = null;
			Value = 0;

			if (TokenStr.StartsWith("$"))
			{
				string NumStr = TokenStr.Substring(1);

				if (NumStr.StartsWith("0x"))
					Value = Convert.ToUInt32(NumStr, 16);
				else
					Value = Convert.ToUInt32(NumStr);

				return true;
			}
			else
			{
				Token = state.RefToken(AsmLine, TokenStr);
				return true;
			}

			return false;
		}

		int ParseOffset(string Tok)
		{
			return Convert.ToInt32(Tok);
		}

		public void Assemble(AssemblerState state, string assemblyCode)
		{
			string[] Lines = assemblyCode.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			state.ClearLocalTokens();

			for (int i = 0; i < Lines.Length; i++)
			{
				string L = Lines[i].Trim();
				int AsmLine = i + 1;

				if (L.StartsWith("#"))
					continue;

				if (L.Contains("#"))
					L = L.Split('#')[0];

				L = L.Trim();


				if (L.StartsWith(".") && !L.EndsWith(":"))
				{
					// Directive	
					string[] Tokens = L.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

					switch (Tokens[0])
					{
						case ".section":
						case ".text":
						case ".data":
							break;

						case ".align":
							{
								int Align = int.Parse(Tokens[1]);
								AsmInstr AlignInstr = new AsmInstr(FishInst.NOP);
								AlignInstr.IsAlign = true;
								AlignInstr.AlignSize = Align;
								AddAsmInstr(AlignInstr);
								break;
							}

						case ".comm":
							{
								state.DefineToken(AsmLine, Tokens[1], 0, true, false);
								break;
							}

						case ".globl":
							{
								AsmToken Tok = state.FindToken(Tokens[1]);
								if (Tok == null)
								{
									state.DefineToken(AsmLine, Tokens[1], 0, true);
								}
								break;
							}

						case ".globlvar":
							{
								AsmToken Tok = state.FindToken(Tokens[1]);
								if (Tok == null)
								{
									Tok = state.DefineToken(AsmLine, Tokens[1], 0, true);
									Tok.IsVariable = true;
								}
								break;
							}

						case ".float":
							{
								AsmInstr RawStr = new AsmInstr(FishInst.NOP);
								string QStr = L.Substring(".float".Length).Trim().TrimEnd('f');

								float val = 0;

								val = float.Parse(QStr, CultureInfo.InvariantCulture);
								RawStr.Raw = BitConverter.GetBytes(val);

								AddAsmInstr(RawStr);
								break;
							}

						case ".long":
							{
								AsmInstr RawStr = new AsmInstr(FishInst.NOP);
								string QStr = L.Substring(".long".Length).Trim();

								int val = 0;

								if (QStr.StartsWith("."))
								{
									AsmToken tt = state.RefToken(AsmLine, QStr);

									RawStr.Raw = BitConverter.GetBytes(0);
									RawStr.SetOp1(tt, sizeof(int));
								}
								else
								{
									val = int.Parse(QStr);
									RawStr.Raw = BitConverter.GetBytes(val);
								}

								AddAsmInstr(RawStr);
								break;
							}

						case ".Raw":
							{
								int Count = int.Parse(Tokens[1]);
								int Val = int.Parse(Tokens[2]);

								AsmInstr RawStr = new AsmInstr(FishInst.NOP);
								RawStr.Raw = new byte[Count];

								for (int j = 0; j < Count; j++)
									RawStr.Raw[j] = (byte)Val;

								AddAsmInstr(RawStr);
								break;
							}

						case ".String":
							{
								AsmInstr RawStr = new AsmInstr(FishInst.NOP);
								string QStr = L.Substring(".String".Length).Trim();
								QStr = QStr.Substring(1, QStr.Length - 2);
								QStr = QStr.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");

								int Len = QStr.Length;
								RawStr.Raw = new byte[Len + 1];
								Array.Copy(Encoding.ASCII.GetBytes(QStr), RawStr.Raw, Len);
								AddAsmInstr(RawStr);
								break;
							}

						default:
							Throw(i, $"Unknown directive: {Tokens[0]}");
							break;
					}
				}
				else if (L.EndsWith(":"))
				{
					// Label
					string LabelName = L.Substring(0, L.Length - 1).Trim();
					state.DefineToken(AsmLine, LabelName, CurAddr, false);
				}
				else
				{
					string[] Tokens = L.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
					AsmInstr Instr;






					switch (Tokens[0])
					{
						case "MUL_REG":
						case "IMUL_REG":
						case "POP_REG":
						case "PUSH_REG":
						case "FLOAT_POP_REG":
						case "FLOAT_PUSH_REG":
						case "CALL_REG":
						case "SETNOTEQUAL_REG":
						case "SETEQUAL_REG":
						case "SETGREATER_REG":
						case "SETGREATEREQUAL_REG":
						case "SETLESS_REG":
						case "SETLESSEQUAL_REG":
							if (Tokens.Length != 2)
								Throw(i, $"{Tokens[0]} requires 1 operand");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1((byte)ParseReg(i, Tokens[1]), 1);
							AddAsmInstr(Instr);

							break;

						case "BOLAND_REG_REG":
						case "BOLOR_REG_REG":
						case "BINAND_REG_REG":
						case "BINOR_REG_REG":
						case "BINXOR_REG_REG":
						case "SUB_REG_REG":
						case "MUL_REG_REG":
						case "DIV_REG_REG":
						case "CMP_REG_REG":
						case "MOVEZ_REG_REG":
						case "MOVES_REG_REG":
						case "MOVEBYTE_REG_REG":
						case "ADD_REG_REG":
						case "TEST_REG_REG":
						case "MOVE_REG_REG":
							if (Tokens.Length != 3)
								Throw(i, $"{Tokens[0]} requires 2 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1((byte)ParseReg(i, Tokens[1]), 1);
							Instr.SetOp2((byte)ParseReg(i, Tokens[2]), 1);
							AddAsmInstr(Instr);

							break;

						case "CMP_LONG_REG":
						case "MOVEZ_LONG_REG":
						case "MOVES_LONG_REG":
						case "ADD_LONG_REG":
						case "MOVE_LONG_REG":
						case "SUB_LONG_REG":
							{
								if (Tokens.Length != 3)
									Throw(i, $"{Tokens[0]} requires 2 operands");

								Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));

								if (TryParseToken(AsmLine, state, Tokens[1], out AsmToken Tok, out uint Val))
								{
									if (Tok != null)
									{
										Instr.SetOp1(Tok, 4);
									}
									else
									{
										Instr.SetOp1(Val, 4);
									}
								}
								else
								{
									Throw(i, $"Invalid operand: {Tokens[1]}");
								}

								Instr.SetOp2((byte)ParseReg(i, Tokens[2]), 1);

								AddAsmInstr(Instr);
								break;
							}

						case "FLOAT_LOAD_OFFSET_REG":
						case "FLOAT_POP_OFFSET_REG":
						case "FLOAT_STORE_OFFSET_REG":
						case "DOUBLE_LOAD_OFFSET_REG":
						case "DOUBLE_POP_OFFSET_REG":
						case "DOUBLE_STORE_OFFSET_REG":
							{
								if (Tokens.Length != 3)
									Throw(i, $"{Tokens[0]} requires 2 operands");

								Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
								Instr.SetOp1((uint)ParseOffset(Tokens[1]), 4);
								Instr.SetOp2((byte)ParseReg(i, Tokens[2]), 1);

								AddAsmInstr(Instr);
								break;
							}

						case "LEA_ADDR_REG":
							if (Tokens.Length != 3)
								Throw(i, $"{Tokens[0]} requires 2 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1(state.RefToken(AsmLine, Tokens[1]), 4);
							Instr.SetOp2((byte)ParseReg(i, Tokens[2]), 1);

							AddAsmInstr(Instr);
							break;

						case "MOVEBYTE_REG_OFFSET_REG":
						case "MOVE_REG_OFFSET_REG":
							if (Tokens.Length != 4)
								Throw(i, $"{Tokens[0]} requires 3 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1((byte)ParseReg(i, Tokens[1]), 1);
							Instr.SetOp2((uint)ParseOffset(Tokens[2]), 4);
							Instr.SetOp3((byte)ParseReg(i, Tokens[3]), 1);

							AddAsmInstr(Instr);
							break;

						case "MOVEBYTE_OFFSET_REG_REG":
						case "MOVEZ_OFFSET_REG_REG":
						case "MOVES_OFFSET_REG_REG":
						case "MOVE_OFFSET_REG_REG":
						case "LEA_OFFSET_REG_REG":
							if (Tokens.Length != 4)
								Throw(i, $"{Tokens[0]} requires 3 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1((uint)ParseOffset(Tokens[1]), 4);
							Instr.SetOp2((byte)ParseReg(i, Tokens[2]), 1);
							Instr.SetOp3((byte)ParseReg(i, Tokens[3]), 1);

							AddAsmInstr(Instr);
							break;

						case "SYSCALL":
						case "PUSH_LONG":
						case "JUMP_IF_ZERO_LONG":
						case "JUMP_IF_NOT_ZERO_LONG":
						case "JUMP_LONG":
						case "FLOAT_LOAD_LONG":
						case "DOUBLE_LOAD_LONG":
						case "JUMP_IF_LESS_LONG":
						case "JUMP_IF_GREAT_LONG":
						case "JUMP_IF_LESSEQ_LONG":
						case "JUMP_IF_GREATEQ_LONG":
							{

								if (Tokens.Length != 2)
									Throw(i, $"{Tokens[0]} requires 1 operand");

								Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));

								if (TryParseToken(AsmLine, state, Tokens[1], out AsmToken Tok, out uint Val))
								{
									if (Tok != null)
									{
										Instr.SetOp1(Tok, 4);
									}
									else
									{
										Instr.SetOp1(Val, 4);
									}
								}
								else
								{
									Throw(i, $"Invalid operand: {Tokens[1]}");
								}

								AddAsmInstr(Instr);

								break;
							}

						case "WAIT":
						case "HALT":
						case "INVALID":
						case "DBG_MEM":
						case "DBG_REGS":
						case "DBG_BREAK":
						case "NOP":
						case "LEAVE":
						case "RET":
						case "SYSCALL_2":
						case "FLOAT_ADD":
						case "FLOAT_SUB":
						case "FLOAT_MUL":
						case "FLOAT_DIV":
						case "SOFTINT_ENABLE":
						case "SOFTINT_DISABLE":
							if (Tokens.Length != 1)
								Throw(i, $"{Tokens[0]} requires 0 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							AddAsmInstr(Instr);

							break;

						default:
							Throw(i, $"Unknown instruction: {Tokens[0]}");
							break;
					}
				}

			}
		}

		/*public void LoadOffset(uint Offset)
		{
			foreach (var T in Tokens)
			{
				T.Address += Offset;
			}

			foreach (var I in Assembly)
			{
				I.Address += Offset;
			}
		}*/

		public byte[] Link()
		{
			AsmInstr[] Bytes = Assembly.ToArray();

			File.WriteAllBytes("out.txt", Encoding.UTF8.GetBytes(string.Join("\n", Bytes.Select(B => B.ToString()))));

			using (MemoryStream MS = new MemoryStream())
			{
				MS.Seek(0, SeekOrigin.Begin);

				using (BinaryWriter BW = new BinaryWriter(MS))
				{
					foreach (var Instr in Bytes)
					{
						if (Instr.Op1Token != null || Instr.Op2Token != null || Instr.Op3Token != null)
						{
							if (Instr.Op1Token != null)
							{
								Instr.SetOp1(Instr.Op1Token.ResolveAddress(Instr.Op1Token), 4);
							}

							if (Instr.Op2Token != null)
							{
								Instr.SetOp2(Instr.Op2Token.ResolveAddress(Instr.Op2Token), 4);
							}

							if (Instr.Op3Token != null)
							{
								Instr.SetOp3(Instr.Op3Token.ResolveAddress(Instr.Op3Token), 4);
							}
						}

						Instr.WriteBytes(BW);
					}

					BW.Flush();
				}

				return MS.ToArray();
			}
		}
	}
}
