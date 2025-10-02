using CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishmachine
{
	public class AsmToken
	{
		public string Name;
		public uint Address = 0;
		public bool Global = false;

		public override string ToString()
		{
			return string.Format("{0} @ {1}", Name, Address);
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

		public AsmInstr(FishInst Inst)
		{
			this.Inst = Inst;
		}

		public void SetOp1(uint Val, int Size)
		{
			Op1 = Val;
			Op1Size = Size;
			HasOp1 = true;
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
		}

		public void SetOp3(AsmToken Val, int Size)
		{
			Op3Token = Val;
			Op3Size = Size;
			HasOp3 = true;
		}

		public void WriteBytes(BinaryWriter BW)
		{
			if (Raw != null)
			{
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

		public int Size()
		{
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

	public class Assembler
	{
		List<AsmInstr> Assembly = new List<AsmInstr>();
		List<AsmToken> Tokens = new List<AsmToken>();

		uint CurAddr = 0;

		public Assembler()
		{

		}

		AsmToken RefToken(string TokenName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == TokenName).FirstOrDefault();

			if (Tok == null)
			{
				Tok = new AsmToken();
				Tok.Name = TokenName;
				Tokens.Add(Tok);
			}

			return Tok;
		}

		AsmToken DefineToken(string TokenName, uint Addr, bool Global)
		{
			AsmToken Tok = RefToken(TokenName);

			if (!Tok.Global)
				Tok.Global = Global;

			Tok.Address = Addr;
			return Tok;
		}

		AsmToken FindToken(string TokenName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == TokenName).FirstOrDefault();
			return Tok;
		}

		public uint GetSymbolOffset(string SymbolName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == SymbolName).FirstOrDefault();

			if (Tok == null)
				return 0;

			return Tok.Address;
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
			CurAddr += (uint)Instr.Size();
		}

		bool TryParseToken(string TokenStr, out AsmToken Token, out uint Value)
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
				Token = RefToken(TokenStr);
				return true;
			}

			return false;
		}

		int ParseOffset(string Tok)
		{
			return Convert.ToInt32(Tok);
		}

		public void Assemble(string assemblyCode)
		{
			string[] Lines = assemblyCode.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);


			for (int i = 0; i < Lines.Length; i++)
			{
				string L = Lines[i].Trim();

				if (L.StartsWith("#"))
					continue;

				if (L.Contains("#"))
					L = L.Split('#')[0];

				L = L.Trim();


				if (L.StartsWith(".") && !L.EndsWith(":"))
				{
					// Directive	
					string[] Tokens = L.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

					switch (Tokens[0])
					{
						case ".section":
						case ".text":
							break;

						case ".globl":
							DefineToken(Tokens[1], 0, true);
							break;

						case ".String":
							{
								AsmInstr RawStr = new AsmInstr(FishInst.NOP);
								string QStr = L.Substring(".String".Length).Trim();
								QStr = QStr.Substring(1, QStr.Length - 2);
								QStr = QStr.Replace("\\n", "\n").Replace("\\t", "\t");

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
					DefineToken(LabelName, CurAddr, false);
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
						case "CALL_REG":
						case "SETNOTEQUAL_REG":
						case "SETEQUAL_REG":
						case "SETGREATER_REG":
						case "SETGREATEREQUAL_REG":
							if (Tokens.Length != 2)
								Throw(i, $"{Tokens[0]} requires 1 operand");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1((byte)ParseReg(i, Tokens[1]), 1);
							AddAsmInstr(Instr);

							break;

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

								if (TryParseToken(Tokens[1], out AsmToken Tok, out uint Val))
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

						case "LEA_ADDR_REG":
							if (Tokens.Length != 3)
								Throw(i, $"{Tokens[0]} requires 2 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.SetOp1(RefToken(Tokens[1]), 4);
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

						case "JUMP_IF_ZERO_LONG":
						case "JUMP_LONG":
							{

								if (Tokens.Length != 2)
									Throw(i, $"{Tokens[0]} requires 1 operand");

								Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));

								if (TryParseToken(Tokens[1], out AsmToken Tok, out uint Val))
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

						case "SYSCALL":
						case "NOP":
						case "LEAVE":
						case "RET":
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

		public void LoadOffset(uint Offset)
		{
			foreach (var T in Tokens)
			{
				/*if (T.Global)
					continue;*/

				T.Address += Offset;
			}

			foreach (var I in Assembly)
			{
				I.Address += Offset;
			}
		}

		public byte[] Link()
		{
			AsmInstr[] Bytes = Assembly.ToArray();

			for (int i = 0; i < Bytes.Length; i++)
			{
				if (Bytes[i].Op1Token != null || Bytes[i].Op2Token != null || Bytes[i].Op3Token != null)
				{
					if (Bytes[i].Op1Token != null)
					{
						Bytes[i].SetOp1(Bytes[i].Op1Token.Address, 4);
					}

					if (Bytes[i].Op2Token != null)
					{
						Bytes[i].SetOp2(Bytes[i].Op2Token.Address, 4);
					}

					if (Bytes[i].Op3Token != null)
					{
						Bytes[i].SetOp3(Bytes[i].Op3Token.Address, 4);
					}
				}
			}

			File.WriteAllBytes("out.txt", Encoding.UTF8.GetBytes(string.Join("\n", Bytes.Select(B => B.ToString()))));

			using (MemoryStream MS = new MemoryStream())
			{
				MS.Seek(0, SeekOrigin.Begin);

				using (BinaryWriter BW = new BinaryWriter(MS))
				{
					foreach (var Instr in Bytes)
					{
						Instr.WriteBytes(BW);
					}

					BW.Flush();
				}

				return MS.ToArray();
			}
		}
	}
}
