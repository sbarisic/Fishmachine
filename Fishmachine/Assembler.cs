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
		public uint Op1;
		public uint Op2;
		public uint Op3;

		public AsmToken Op1Token;
		public AsmToken Op2Token;
		public AsmToken Op3Token;

		public byte[] Raw;

		public AsmInstr(FishInst Inst)
		{
			this.Inst = Inst;
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
			return string.Format("{0} {1}, {2}, {3}", Inst, Op1Token?.Name ?? Op1.ToString(), Op2Token?.Name ?? Op2.ToString(), Op3Token?.Name ?? Op3.ToString());
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

		AsmToken DefineToken(string TokenName, bool Global)
		{
			AsmToken Tok = RefToken(TokenName);

			if (!Tok.Global)
				Tok.Global = Global;

			Tok.Address = 0;
			return Tok;
		}

		AsmToken FindToken(string TokenName)
		{
			AsmToken Tok = Tokens.Where(T => T.Name == TokenName).FirstOrDefault();
			return Tok;
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
							DefineToken(Tokens[1], true);
							break;

						case ".String":
							{
								AsmInstr RawStr = new AsmInstr(FishInst.NOP);
								int Len = Tokens[1].Length;
								RawStr.Raw = new byte[Len + 1];
								Array.Copy(Encoding.ASCII.GetBytes(Tokens[1]), RawStr.Raw, Len);
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
					AsmToken Tok = DefineToken(LabelName, false);
					Tok.Address = CurAddr;
				}
				else
				{
					string[] Tokens = L.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
					AsmInstr Instr;

					switch (Tokens[0])
					{
						case "PUSH_REG":
							if (Tokens.Length != 2)
								Throw(i, $"{Tokens[0]} requires 1 operand");

							Instr = new AsmInstr(FishInst.PUSH_REG);
							Instr.Op1 = (byte)ParseReg(i, Tokens[1]);
							AddAsmInstr(Instr);

							break;

						case "MOVE_REG_REG":
							if (Tokens.Length != 3)
								Throw(i, $"{Tokens[0]} requires 2 operands");

							Instr = new AsmInstr(FishInst.MOVE_REG_REG);
							Instr.Op1 = (byte)ParseReg(i, Tokens[1]);
							Instr.Op2 = (byte)ParseReg(i, Tokens[2]);
							AddAsmInstr(Instr);

							break;

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
										Instr.Op1Token = Tok;
									}
									else
									{
										Instr.Op1 = Val;
									}
								}
								else
								{
									Throw(i, $"Invalid operand: {Tokens[1]}");
								}

								Instr.Op2 = (byte)ParseReg(i, Tokens[2]);

								AddAsmInstr(Instr);
								break;
							}

						case "LEA_ADDR_REG":
							if (Tokens.Length != 3)
								Throw(i, $"{Tokens[0]} requires 2 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.Op1Token = RefToken(Tokens[1]);
							Instr.Op2 = (byte)ParseReg(i, Tokens[2]);

							AddAsmInstr(Instr);
							break;

						case "MOVE_REG_OFFSET_REG":
							if (Tokens.Length != 4)
								Throw(i, $"{Tokens[0]} requires 3 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.Op1 = (byte)ParseReg(i, Tokens[1]);
							Instr.Op2 = (uint)ParseOffset(Tokens[2]);
							Instr.Op3 = (byte)ParseReg(i, Tokens[3]);

							AddAsmInstr(Instr);
							break;

						case "LEA_OFFSET_REG_REG":
							if (Tokens.Length != 4)
								Throw(i, $"{Tokens[0]} requires 3 operands");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.Op1 = (uint)ParseOffset(Tokens[1]);
							Instr.Op2 = (byte)ParseReg(i, Tokens[2]);
							Instr.Op3 = (byte)ParseReg(i, Tokens[3]);

							AddAsmInstr(Instr);
							break;

						case "CALL_REG":
							if (Tokens.Length != 2)
								Throw(i, $"{Tokens[0]} requires 1 operand");

							Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));
							Instr.Op1 = (byte)ParseReg(i, Tokens[1]);
							AddAsmInstr(Instr);

							break;


						case "JUMP_LONG":
							{

								if (Tokens.Length != 2)
									Throw(i, $"{Tokens[0]} requires 1 operand");

								Instr = new AsmInstr(Enum.Parse<FishInst>(Tokens[0]));

								if (TryParseToken(Tokens[1], out AsmToken Tok, out uint Val))
								{
									if (Tok != null)
									{
										Instr.Op1Token = Tok;
									}
									else
									{
										Instr.Op1 = Val;
									}
								}
								else
								{
									Throw(i, $"Invalid operand: {Tokens[1]}");
								}

								AddAsmInstr(Instr);

								break;
							}

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

		public byte[] Link()
		{
			AsmInstr[] Bytes = Assembly.ToArray();

			for (int i = 0; i < Bytes.Length; i++)
			{
				if (Bytes[i].Op1Token != null || Bytes[i].Op2Token != null || Bytes[i].Op3Token != null)
				{
					if (Bytes[i].Op1Token != null)
					{
						Bytes[i].Op1 = Bytes[i].Op1Token.Address;
					}

					if (Bytes[i].Op2Token != null)
					{
						Bytes[i].Op2 = Bytes[i].Op2Token.Address;
					}

					if (Bytes[i].Op3Token != null)
					{
						Bytes[i].Op3 = Bytes[i].Op3Token.Address;
					}
				}
			}

			using (MemoryStream MS = new MemoryStream())
			using (BinaryWriter BW = new BinaryWriter(MS))
			{
				foreach (var Instr in Bytes)
				{
					Instr.WriteBytes(BW);
				}

				return MS.ToArray();
			}
		}
	}
}
