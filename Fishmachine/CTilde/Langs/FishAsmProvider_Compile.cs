using CodeGeneration;
using CTilde.Expr;
using CTilde.FishAsm;
using Fishmachine.CTilde.FishAsm;
using Fishmachine.VM;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTilde.Langs
{
	public partial class FishAsmProvider : LangProvider
	{
		public override void Compile(Expression Ex)
		{
			switch (Ex)
			{
				case Expr_Block Block:
					{
						foreach (var E in Block.Expressions)
						{
							Compile(E);
						}
						break;
					}

				/*case Expr_ClassDef ClassDef:
					{
						AppendLine("typedef struct {");

						foreach (var E in ClassDef.Variables)
						{
							Compile(E);
						}

						AppendLine("}} {0};", ClassDef.Name);

						foreach (var F in ClassDef.Functions)
						{
							Compile(F);
						}
						break;
					}*/

				case Expr_ClassDef ClassDef:
					{
						break;
					}

				case Expr_EnumDef EnumDef:
					{
						break;
					}

				case Expr_StructDef StructDef:
					{
						EmitRaw(".struct {0}", StructDef.Name);
						FishStructDef Struc = State.Types.DefineStruct(StructDef.Name);
						Indent();

						foreach (Expr_VariableDef V in StructDef.Variables)
						{
							int Size = Expr_TypeDef.GetRawTypeSize(State.Types, V.Type);
							Struc.Field(V.Ident.Identifier, Size, V.Type);

							EmitRaw(".field {0} {1}", V.Ident.Identifier, Size);
						}

						Unindent();
						EmitRaw(".endstruct");
						break;
					}

				case Expr_FuncDef FuncDef:
					{
						// Interrupt handler wrappers (preserve regs, forward args)
						if (FuncDef.FuncName != null && FuncDef.Interrupt)
						{
							string implName = FuncDef.FuncName + "_imp";

							EmitRaw(".globl {0}", implName);
							State.DefineLabel(implName, FuncDef.FuncReturnTypeDef, true, true);

							if (FuncDef.FuncBody != null)
							{
								State.ClearVarOffsets();
								State.ClearArgOffset();

								EmitRaw("{0}:", implName);
								Indent();
								State.IsInsideFunctionDef = true;

								if (!FuncDef.Naked)
								{
									EmitInstruction(FishInst.PUSH_REG, Reg.EBP);
									EmitInstruction(FishInst.MOVE_REG_REG, Reg.ESP, Reg.EBP);
								}

								Compile(FuncDef.FuncParams);

								State.IsInsideFunctionDef = false;
								State.IsInsideFunctionBody = true;

								Compile(FuncDef.FuncBody);

								if (!FuncDef.Naked)
								{
									EmitRaw("# EmitReturn for interrupt impl {0}", implName);
									EmitReturn();
								}
								State.IsInsideFunctionBody = false;
								Unindent();
							}

							EmitRaw(".globl {0}", FuncDef.FuncName);
							State.DefineLabel(FuncDef.FuncName, FuncDef.FuncReturnTypeDef, true, true);
							EmitRaw("{0}:", FuncDef.FuncName);
							Indent();
							EmitInstruction(FishInst.SOFTINT_DISABLE);

							EmitInstruction(FishInst.PUSH_REG, Reg.EBP);
							EmitInstruction(FishInst.MOVE_REG_REG, Reg.ESP, Reg.EBP);

							Reg[] saveRegs = new[] { Reg.EAX, Reg.EBX, Reg.ECX, Reg.EDX, Reg.ESI, Reg.EDI };
							foreach (var r in saveRegs)
								EmitInstruction(FishInst.PUSH_REG, r);

							//int argCount = FuncDef.FuncParams != null ? FuncDef.FuncParams.Definitions.Count : 0;
							int argCount = FuncDef.FuncParams != null ? FuncDef.FuncParams.Definitions.Count : 0;

							// was: for (int i = 0; i < argCount; i++)
							for (int i = argCount - 1; i >= 0; i--)
							{
								EmitInstruction(FishInst.MOVE_OFFSET_REG_REG, (8 + i * 4), Reg.EBP, Reg.EAX);
								EmitInstruction(FishInst.PUSH_REG, Reg.EAX);
							}

							EmitInstruction(FishInst.MOVE_LONG_REG, implName, Reg.EAX);
							EmitInstruction(FishInst.CALL_REG, Reg.EAX);

							if (argCount > 0)
								EmitInstruction(FishInst.ADD_LONG_REG, (uint)(argCount * 4), Reg.ESP);

							for (int i = saveRegs.Length - 1; i >= 0; i--)
								EmitInstruction(FishInst.POP_REG, saveRegs[i]);

							EmitInstruction(FishInst.SOFTINT_ENABLE);
							EmitInstruction(FishInst.LEAVE);

							EmitRaw("# RET for interrupt impl {0}", FuncDef.FuncName);
							EmitInstruction(FishInst.RET);

							Unindent();
							break;
						}

						// Normal function
						EmitRaw(".globl {0}", FuncDef.FuncName);
						State.DefineLabel(FuncDef.FuncName, FuncDef.FuncReturnTypeDef, true, true);

						if (FuncDef.FuncBody != null)
						{
							State.ClearVarOffsets();
							State.ClearArgOffset();

							EmitRaw("{0}:", FuncDef.FuncName);
							Indent();
							State.IsInsideFunctionDef = true;

							//EmitInstruction(FishInst.SOFTINT_DISABLE);

							if (!FuncDef.Naked)
							{
								EmitInstruction(FishInst.PUSH_REG, Reg.EBP);
								EmitInstruction(FishInst.MOVE_REG_REG, Reg.ESP, Reg.EBP);
							}

							Compile(FuncDef.FuncParams);

							State.IsInsideFunctionDef = false;
							State.IsInsideFunctionBody = true;

							Compile(FuncDef.FuncBody);

							//EmitInstruction(FishInst.SOFTINT_ENABLE);

							if (!FuncDef.Naked)
							{
								EmitRaw("# EmitReturn for function {0}", FuncDef.FuncName);
								EmitReturn();
							}
							State.IsInsideFunctionBody = false;
							Unindent();
						}
						break;
					}

				case Expr_Module Module:
					{
						foreach (var E in Module.Expressions)
							Compile(E);

						EmitLabels();
						break;
					}

				case Expr_ParamsDef ParamsDef:
					{
						for (int i = 0; i < ParamsDef.Definitions.Count; i++)
						{
							ParamDefData ParamDef = ParamsDef.Definitions[i];
							int Size = Expr_TypeDef.GetRawTypeSize(State.Types, ParamDef.ParamType);

							// Check if this is a struct larger than 4 bytes - these are passed by reference
							Expr_TypeDef ActualParamType = ParamDef.ParamType;
							if (Size > 4 && !Expr_TypeDef.IsPointerType(ParamDef.ParamType))
							{
								// Create a pointer version of the type for internal use
								ActualParamType = new Expr_TypeDef();
								ActualParamType.Type = ParamDef.ParamType.Type;
								ActualParamType.IsPointer = true;
								ActualParamType.IsArray = false;
								ActualParamType.ArraySize = 0;

								EmitRaw("#: Param '{0}' is struct size={1}, treating as pointer internally", ParamDef.Name, Size);
								// Register as 4-byte pointer parameter
								State.DefineVar(ParamDef.Name, 4, true, ActualParamType, false, true);
							}
							else
							{
								// Normal parameter
								State.DefineVar(ParamDef.Name, Size, true, ParamDef.ParamType, false, true);
							}
						}

						break;
					}

				case Expr_TypeDef TypeDef:
					{
						string T = TypeDef.Type;

						if (TypeDef.IsPointer)
							T += "*";
						else if (TypeDef.IsArray)
							T += "[]";

						Append(T);
						break;
					}

				case Expr_StaticValue StaticValueExpr:
					{

						EmitRaw("# Expr_StaticValue BEGIN - {0}", StaticValueExpr.ToSourceStr());
						Indent();

						string StatVar = State.DefineFreeLabel("STATVAR", StaticValueExpr.TypeDefExpr, false, false);
						EmitRaw("{0}:", StatVar);

						/*Expr_TypeDef ValType = Expr_TypeDef.MakeByte();
						if (Expr_TypeDef.IsPointerType(StaticValueExpr.TypeDefExpr))
							ValType = Expr_TypeDef.MakeArray(ValType, StaticValueExpr.TypeDefExpr.ArraySize);

						string StatVarVal = State.DefineFreeLabel("STATVAR_VAL", ValType, false);
						EmitRaw(".long {0}", StatVarVal);
						EmitRaw("{0}:", StatVarVal);*/

						if (Expr_TypeDef.IsPointerType(StaticValueExpr.TypeDefExpr))
						{
							int ElementSize = Expr_TypeDef.GetDerefTypeSize(State.Types, StaticValueExpr.TypeDefExpr);
							EmitRaw(".Raw {0}, {1}", StaticValueExpr.TypeDefExpr.ArraySize * ElementSize, 0);
						}


						Unindent();
						EmitRaw("# Expr_StaticValue END - {0}", StaticValueExpr.ToSourceStr());
						break;
					}

				case Expr_VariableDef VariableDef:
					{
						EmitRaw("# VariableDef BEGIN - {0}", VariableDef.Ident.Identifier);
						Indent();

						if (!State.IsInsideFunctionBody)
						{
							State.DefineLabel(VariableDef.Ident.Identifier, VariableDef.Type, false, true);
							EmitRaw(".globlvar {0}", VariableDef.Ident.Identifier);

							int Size = Expr_TypeDef.GetRawTypeSize(State.Types, VariableDef.Type);
							State.DefineVar(VariableDef.Ident.Identifier, Size, false, VariableDef.Type, true, false);
						}
						else
						{
							int Size = Expr_TypeDef.GetRawTypeSize(State.Types, VariableDef.Type);
							State.DefineVar(VariableDef.Ident.Identifier, Size, false, VariableDef.Type, false, false);

							EmitInstruction(FishInst.SUB_LONG_REG, (uint)Size, Reg.ESP);
						}

						Unindent();
						EmitRaw("# VariableDef END - {0}", VariableDef.Ident.Identifier);
						break;
					}

				case Expr_AssignedVariableDef AssVariableDef:
					{
						EmitRaw("# Expr_AssignedVariableDef BEGIN - {0}", AssVariableDef.VariableDef.Ident.Identifier);
						Indent();

						int Size = Expr_TypeDef.GetRawTypeSize(State.Types, AssVariableDef.VariableDef.Type);
						bool Global = State.IsInsideFunctionBody ? false : true;

						// IMPORTANT: If assigned a static value, use the static array type instead of the variable's declared type
						Expr_TypeDef VarType = AssVariableDef.VariableDef.Type;
						if (AssVariableDef.AssignmentValue is Expr_StaticValue StaticVal)
						{
							VarType = StaticVal.TypeDefExpr;
						}

						State.DefineVar(AssVariableDef.VariableDef.Ident.Identifier, Size, false, VarType, Global, false);

						if (State.IsInsideFunctionBody)
						{
							EmitInstruction(FishInst.SUB_LONG_REG, (uint)Size, Reg.ESP);
						}
						else
						{
							State.DefineLabel(AssVariableDef.VariableDef.Ident.Identifier, VarType, false, true);
							EmitRaw(".globlvar {0}", AssVariableDef.VariableDef.Ident.Identifier);
							EmitRaw("{0}:", AssVariableDef.VariableDef.Ident.Identifier);

							string StatVar = State.DefineFreeLabel("VARMEM", VarType, false, false);
							EmitRaw(".long {0}", StatVar);
							EmitRaw("{0}:", StatVar);
						}

						Unindent();
						EmitRaw("# Expr_AssignedVariableDef END - {0}", AssVariableDef.VariableDef.Ident.Identifier);

						EmitRaw("# VariableAssign '{0}' BEGIN", AssVariableDef.VariableDef.Ident.Identifier);
						Indent();

						Compile(AssVariableDef.AssignmentValue);

						if (State.IsInsideFunctionBody)
						{
							int VarID = State.GetVarOffset(AssVariableDef.VariableDef.Ident.Identifier);
							EmitInstruction(FishInst.MOVE_REG_OFFSET_REG, Reg.EAX, VarID, Reg.EBP);
						}

						Unindent();
						EmitRaw("# VariableAssign '{0}' END", AssVariableDef.VariableDef.Ident.Identifier);
						break;
					}

				case Expr_AssignValue AssValue:
					{
						EmitRaw("# Expr_AssignValue '{0} = {1}' BEGIN", AssValue.LExpr.ToSourceStr(), AssValue.ValueExpr.ToSourceStr());
						Indent();

						if (AssValue.LExpr is Expr_IndexOp IndexOp)
						{
							EmitRaw("# AssignValue IndexOp");

							// IMPORTANT: Set flag BEFORE compiling IndexOp
							bool prevAddrOnly = State.IndexEmitOnlyAddress;
							State.IndexEmitOnlyAddress = true;  // ← Force address mode

							Compile(IndexOp);  // ← Should put address in EAX

							State.IndexEmitOnlyAddress = prevAddrOnly;  // ← Restore flag

							EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.EBX);  // Save address to EBX

							Compile(AssValue.ValueExpr);  // Get value in EAX

							if (IndexOp.LExpr is Expr_Identifier Id)
							{
								Expr_TypeDef VarType = State.GetVarType(Id.Identifier);
								int CopyBytes;

								// Check if this is struct member access
								if (IndexOp.IndexValExpr is Expr_MemberAccessOp MemAcc)
								{
									// This is struct member access - get the field's type and size
									if (State.Types.TryGetType(VarType.Type, out FishTypeDef FT))
									{
										if (FT is FishStructDef FST)
										{
											Expr_TypeDef fieldType = FST.GetFieldType(MemAcc.MemberName);
											CopyBytes = Expr_TypeDef.GetRawTypeSize(State.Types, fieldType);

											EmitRaw("#: Struct field '{0}.{1}' size={2}", Id.Identifier, MemAcc.MemberName, CopyBytes);
										}
										else
										{
											throw new NotImplementedException("Expected struct type");
										}
									}
									else
									{
										throw new NotImplementedException("Could not find struct type");
									}
								}
								else
								{
									// This is array/pointer indexing - use deref size
									CopyBytes = Expr_TypeDef.GetDerefTypeSize(State.Types, VarType);
								}

								// EBX already contains the correct address (base + offset), so pass offset=0
								EmitStoreToAddress(CopyBytes, 0, Reg.EAX, Reg.EBX, Expr_TypeDef.IsUnsigned(VarType.Type), false);
							}
							else
								throw new NotImplementedException();
						}
						else if (AssValue.LExpr is Expr_Identifier IdentOp)
						{
							EmitStoreToIdent(AssValue.ToSourceStr(), AssValue.ValueExpr, IdentOp);
						}
						/*else if (AssValue.LExpr is Expr_MemberAccessOp FieldOp)
						{
							//EmitStoreToIdent(FieldOp.ToSourceStr(), AssValue.ValueExpr, FieldOp.InstanceName + "." + FieldOp.MemberName);
							throw new NotImplementedException();
						}*/
						else
							throw new NotImplementedException();


						Unindent();
						EmitRaw("# Expr_AssignValue '{0} = {1}' END", AssValue.LExpr.ToSourceStr(), AssValue.ValueExpr.ToSourceStr());
						break;
					}

				/*case Expr_MemberAccessOp FieldOp:
					{
						//EmitReadFromIdent(FieldOp.ToSourceStr(), FieldOp.InstanceName + "." + FieldOp.MemberName);
						throw new NotImplementedException();
						break;
					}*/

				case Expr_AssignVariable AssVariable:
					{
						EmitStoreToIdent(AssVariable.ToSourceStr(), AssVariable.AssignmentValue, AssVariable.Variable);
						break;
					}

				case Expr_Identifier id:
					{
						//EmitReadFromIdent(id.ToSourceStr(), id);

						EmitRaw("# Expr_Identifier '{0}' BEGIN", id.Identifier);
						Indent();

						var t = State.GetVarType(id.Identifier);

						if (t == null)
							throw new NotImplementedException();

						//bool isGlobal = State.IsVarGlobal(id.Identifier);
						bool isPointer = Expr_TypeDef.IsPointerType(t);

						int sz = Expr_TypeDef.GetRawTypeSize(State.Types, t);

						// For structs larger than 4 bytes, we pass by address, not by value
						// So when referencing a struct variable, we get its address
						if (sz > 4 && !isPointer)
						{
							EmitRaw("#: Struct '{0}' size={1}, loading address instead of value", id.Identifier, sz);
							// Load address of struct instead of its value
							FetchIdentifier(id.Identifier, 0, isPointer, Reg.EAX, true, false);
						}
						else
						{
							// Normal value loading for primitives and pointers
							FetchIdentifier(id.Identifier, sz, isPointer, Reg.EAX, true, false);
						}

						Unindent();
						EmitRaw("# Expr_Identifier '{0}' END", id.Identifier);
						break;
					}

				case Expr_MemberAccessOp FieldOp:
					{
						EmitRaw("# Expr_MemberAccessOp '{0}.{1}' BEGIN", FieldOp.VariableName, FieldOp.MemberName);
						Indent();

						Expr_TypeDef t = State.GetVarType(FieldOp.VariableName);

						if (t == null)
							throw new NotImplementedException();

						if (State.Types.TryGetType(t.Type, out FishTypeDef FT))
							if (FT is FishStructDef FST)
							{
								int Offset = FST.GetFieldOffset(FieldOp.MemberName);
								EmitInstruction(FishInst.MOVE_LONG_REG, (uint)Offset, Reg.EAX);
							}

						//bool isGlobal = State.IsVarGlobal(id.Identifier);
						//bool isPointer = Expr_TypeDef.IsPointerType(t);

						//int sz = Expr_TypeDef.GetRawTypeSize(State.Types, t);

						//if (State.IsVarGlobal(id.Identifier))
						//	sz = 0;
						//awd
						//FetchIdentifier(id.Identifier, sz, isPointer, Reg.EAX, true, false);

						Unindent();
						EmitRaw("# Expr_MemberAccessOp '{0}.{1}' END", FieldOp.VariableName, FieldOp.MemberName);
						break;
					}

				case Expr_ConstNull NullEx:
					{
						EmitRaw("# Expr_ConstNull BEGIN");
						Indent();

						if (State.IsInsideFunctionBody)
						{
							EmitInstruction(FishInst.MOVE_LONG_REG, (uint)0, Reg.EAX);
						}
						else
							EmitRaw(".long {0}", 0);

						Unindent();
						EmitRaw("# Expr_ConstNull END");
						break;
					}

				case Expr_ConstNumber NumberEx:
					{
						EmitRaw("# Expr_ConstNumber BEGIN");
						Indent();

						uint Num = 0;

						if (NumberEx.NumberLiteral.StartsWith("0x"))
							Num = Convert.ToUInt32(NumberEx.NumberLiteral.Substring(2), 16);

						else
							Num = uint.Parse(NumberEx.NumberLiteral);

						if (State.IsInsideFunctionBody)
						{
							EmitInstruction(FishInst.MOVE_LONG_REG, Num, Reg.EAX);
						}
						else
							EmitRaw(".long {0}", Num);

						Unindent();
						EmitRaw("# Expr_ConstNumber END");
						break;
					}

				case Expr_ConstString StringEx:
					{
						EmitRaw("# Expr_ConstString BEGIN");
						Indent();

						string LblName = State.DefineLabel(null, StringEx.StringLiteral);
						EmitInstruction(FishInst.MOVE_LONG_REG, LblName, Reg.EAX);

						Unindent();
						EmitRaw("# Expr_ConstString END");
						break;
					}

				case Expr_ConstChar CharEx:
					{
						EmitRaw("# Expr_ConstChar BEGIN");
						Indent();

						EmitInstruction(FishInst.MOVES_LONG_REG, (uint)CharEx.CharLiteral, Reg.EAX);

						Unindent();
						EmitRaw("# Expr_ConstChar END");
						break;
					}

				case Expr_BinaryOp BinaryExp:
					{
						EmitRaw("# Expr_BinaryOp BEGIN ({0})", BinaryExp.ToSourceStr());
						Indent();

						EmitInstruction(FishInst.PUSH_REG, Reg.ECX);

						Compile(BinaryExp.RExpr);                 // EAX = LHS
						EmitInstruction(FishInst.PUSH_REG, Reg.EAX);

						Compile(BinaryExp.LExpr);                 // EAX = RHS
						EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.ECX); // ECX = RHS

						EmitInstruction(FishInst.POP_REG, Reg.EAX);  // EAX = LHS

						switch (BinaryExp.Op)
						{
							case BinaryOp.And:
								EmitInstruction(FishInst.BOLAND_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L && R
								break;

							case BinaryOp.Or:
								EmitInstruction(FishInst.BOLOR_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L || R
								break;

							case BinaryOp.BitwiseAnd:
								EmitInstruction(FishInst.BINAND_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L & R
								break;

							case BinaryOp.BitwiseOr:
								EmitInstruction(FishInst.BINOR_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L | R
								break;

							case BinaryOp.BitwiseXor:
								EmitInstruction(FishInst.BINXOR_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L ^ R
								break;

							default:
								throw new NotImplementedException();
						}

						EmitInstruction(FishInst.POP_REG, Reg.ECX);


						Unindent();
						EmitRaw("# Expr_BinaryOp END ({0})", BinaryExp.ToSourceStr());

						break;
					}

				// FloatExpr
				case Expr_ConstDecimal DecimalEx:
					{
						EmitRaw("# Expr_ConstDecimal BEGIN");
						Indent();

						float fNum = 0.0f;
						fNum = float.Parse(DecimalEx.NumberLiteral.TrimEnd('f'));

						if (State.IsInsideFunctionBody)
						{
							string LblName = State.DefineLabel(null, DecimalEx.NumberLiteral);
							//EmitInstruction(FishInst.FLOAT_LOAD_LONG, LblName);

							EmitInstruction(FishInst.MOVE_LONG_REG, LblName, Reg.EAX);
							EmitInstruction(FishInst.MOVE_OFFSET_REG_REG, 0, Reg.EAX, Reg.EAX);
						}
						else
							EmitRaw(".float {0}", fNum);

						Unindent();
						EmitRaw("# Expr_ConstDecimal END");
						break;
					}

				case Expr_MathOp MathExp:
					{
						EmitRaw("# MathOp BEGIN ({0})", MathExp.OpString);
						Indent();

						if (!MathExp.IsFloat)
						{
							Expr_TypeDef LType = GetExpressionType(MathExp.LExpr);
							Expr_TypeDef RType = GetExpressionType(MathExp.RExpr);

							if (Expr_TypeDef.IsFloatType(LType) || Expr_TypeDef.IsFloatType(RType))
								MathExp.IsFloat = true;
						}

						if (MathExp.IsFloat)
						{
							Compile(MathExp.RExpr);
							EmitInstruction(FishInst.FLOAT_PUSH_REG, Reg.EAX);

							Compile(MathExp.LExpr);
							EmitInstruction(FishInst.FLOAT_PUSH_REG, Reg.EAX);

							switch (MathExp.Op)
							{
								case MathOperation.Add:
									EmitInstruction(FishInst.FLOAT_ADD);
									break;
								case MathOperation.Sub:
									EmitInstruction(FishInst.FLOAT_SUB);
									break;
								case MathOperation.Mul:
									EmitInstruction(FishInst.FLOAT_MUL);
									break;
								case MathOperation.Div:
									EmitInstruction(FishInst.FLOAT_DIV);
									break;
								default:
									throw new NotImplementedException();
							}

							//EmitInstruction(FishInst.FLOAT_STORE_OFFSET_REG, Reg.ST0, Reg.EAX);
							//EmitInstruction(FishInst.MOVE_REG_REG, Reg.ST0, Reg.EAX);
							EmitInstruction(FishInst.FLOAT_POP_REG, Reg.EAX);



							//throw new NotImplementedException();
						}
						else
						{
							// LHS then RHS in ECX; avoid EBX (used by global loads)
							if (MathExp.Op == MathOperation.Sub && MathExp.RExpr is Expr_ConstNumber rnum)
							{
								// Optimize: L - imm
								Compile(MathExp.LExpr);
								uint imm = uint.Parse(rnum.NumberLiteral);
								EmitInstruction(FishInst.SUB_LONG_REG, "$" + imm, Reg.EAX);
							}
							else
							{
								EmitInstruction(FishInst.PUSH_REG, Reg.ECX);

								Compile(MathExp.LExpr);                 // EAX = LHS
								EmitInstruction(FishInst.PUSH_REG, Reg.EAX);

								Compile(MathExp.RExpr);                 // EAX = RHS
								EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.ECX); // ECX = RHS

								EmitInstruction(FishInst.POP_REG, Reg.EAX);  // EAX = LHS

								switch (MathExp.Op)
								{
									case MathOperation.Add:
										EmitInstruction(FishInst.ADD_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L + R
										break;

									case MathOperation.Sub:
										EmitInstruction(FishInst.SUB_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L - R
										break;

									case MathOperation.Mul:
										EmitInstruction(FishInst.MUL_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L * R
										break;

									case MathOperation.Div:
										EmitInstruction(FishInst.DIV_REG_REG, Reg.ECX, Reg.EAX);   // EAX = L / R
										break;

									default:
										throw new NotImplementedException();
								}

								EmitInstruction(FishInst.POP_REG, Reg.ECX);
							}
						}

						Unindent();
						EmitRaw("# MathOp END ({0})", MathExp.OpString);

						break;
					}

				case Expr_ComparisonOp CompExpr:
					{
						EmitRaw("# Expr_ComparisonOp BEGIN");
						Indent();

						// EAX = LHS, ECX = RHS; compare as EAX - ECX
						Compile(CompExpr.LExpr);
						EmitInstruction(FishInst.PUSH_REG, Reg.EAX);

						Compile(CompExpr.RExpr);
						EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.ECX);

						EmitInstruction(FishInst.POP_REG, Reg.EAX);

						EmitRaw("#: {0}", CompExpr.ToSourceStr());
						EmitRaw("#: EAX - ECX semantics");

						// IMPORTANT: pass (ECX, EAX) so assembler prints "CMP %EAX, %ECX"
						//EmitInstruction(FishInst.CMP_REG_REG, Reg.ECX, Reg.EAX);

						EmitInstruction(FishInst.CMP_REG_REG, Reg.EAX, Reg.ECX);

						string CmpTrueLabel = State.DefineFreeLabel("COMPARE_TRUE", null, false, false);
						string CmpEndLabel = State.DefineFreeLabel("COMPARE_END", null, false, false);
						EmitBranch(CompExpr.Op, false, CmpTrueLabel);
						EmitInstruction(FishInst.MOVE_LONG_REG, (uint)0, Reg.ECX); // false
						EmitInstruction(FishInst.JUMP_LONG, CmpEndLabel);
						EmitRaw("{0}:", CmpTrueLabel);
						EmitInstruction(FishInst.MOVE_LONG_REG, (uint)1, Reg.ECX); // false
						EmitRaw("{0}:", CmpEndLabel);

						EmitInstruction(FishInst.MOVE_REG_REG, Reg.ECX, Reg.EAX);

						Unindent();
						EmitRaw("# Expr_ComparisonOp END");
						break;
					}

				case Expr_IfElseStatement IfExpr:
					{
						EmitRaw("# If BEGIN");
						Indent();

						string EndLblName = State.DefineFreeLabel("ENDIF", null, false, false);
						string ElseLblName = EndLblName;

						if (IfExpr.ElseBody != null)
							ElseLblName = State.DefineFreeLabel("ELSE", null, false, false);

						EmitRaw("#: {0}", IfExpr.ConditionValue.ToSourceStr());

						/*Compile(IfExpr.ConditionValue); // sets flags for EAX vs ECX
						if (IfExpr.ConditionValue is Expr_ComparisonOp Cmp)
						{
							EmitBranch(Cmp.Op, true, ElseLblName);
						}
						else if (IfExpr.ConditionValue is Expr_BinaryOp BinOp)
						{
							EmitInstruction(FishInst.CMP_REG_REG, Reg.EAX, Reg.EAX);
							EmitInstruction(FishInst.JUMP_IF_ZERO_LONG, ElseLblName);

							//throw new NotImplementedException();
						}*/

						EmitTestBranch(IfExpr.ConditionValue, true, ElseLblName);

						//State.PushBreakLabel(EndLblName);
						Compile(IfExpr.Body);
						//State.PopBreakLabel();

						if (IfExpr.ElseBody != null)
						{
							EmitInstruction(FishInst.JUMP_LONG, EndLblName);
							Unindent();
							EmitRaw("# Else BEGIN");
							Indent();
							EmitRaw("{0}:", ElseLblName);

							//State.PushBreakLabel(EndLblName);
							Compile(IfExpr.ElseBody);
							//State.PopBreakLabel();

							Unindent();
							EmitRaw("# Else END");
							Indent();
						}

						EmitRaw("{0}:", EndLblName);

						Unindent();
						EmitRaw("# If END");
						break;
					}

				case Expr_WhileStatement WhileExpr:
					{
						EmitRaw("# While BEGIN '{0}'", WhileExpr.ToSourceStr());
						Indent();

						string LblName = State.DefineFreeLabel("WHILE", null, false, false);
						string EndLblName = State.DefineFreeLabel("ENDWHILE", null, false, false);
						EmitRaw("{0}:", LblName);
						State.PushLoopLabel(LblName);

						EmitTestBranch(WhileExpr.ConditionValue, true, EndLblName);

						State.PushBreakLabel(EndLblName);

						if (WhileExpr.Body.Expressions.Count > 0)
						{
							EmitInstruction(FishInst.PUSH_REG, Reg.EBX);
							EmitInstruction(FishInst.PUSH_REG, Reg.EAX);
							Compile(WhileExpr.Body);
							EmitInstruction(FishInst.POP_REG, Reg.EAX);
							EmitInstruction(FishInst.POP_REG, Reg.EBX);
						}

						State.PopBreakLabel();
						State.PopLoopLabel();

						EmitInstruction(FishInst.JUMP_LONG, LblName);
						EmitRaw("{0}:", EndLblName);

						Unindent();
						EmitRaw("# While END '{0}'", WhileExpr.ToSourceStr());
						break;
					}

				case Expr_ForStatement ForExpr:
					{
						EmitRaw("# For BEGIN '{0}'", ForExpr.ToSourceStr());
						Indent();

						Compile(ForExpr.Exp1); // initialization

						string LblName = State.DefineFreeLabel("FOR", null, false, false);
						string EndLblName = State.DefineFreeLabel("ENDFOR", null, false, false);
						EmitRaw("{0}:", LblName);
						State.PushLoopLabel(LblName);

						EmitTestBranch(ForExpr.Exp2, true, EndLblName);

						State.PushBreakLabel(EndLblName);

						if (ForExpr.Body.Expressions.Count > 0)
						{
							EmitInstruction(FishInst.PUSH_REG, Reg.EBX);
							EmitInstruction(FishInst.PUSH_REG, Reg.EAX);
							Compile(ForExpr.Body);
							EmitInstruction(FishInst.POP_REG, Reg.EAX);
							EmitInstruction(FishInst.POP_REG, Reg.EBX);
						}

						Compile(ForExpr.Exp3); // iteration

						State.PopBreakLabel();
						State.PopLoopLabel();

						EmitInstruction(FishInst.JUMP_LONG, LblName);
						EmitRaw("{0}:", EndLblName);

						Unindent();
						EmitRaw("# For END '{0}'", ForExpr.ToSourceStr());
						break;
					}

				case Expr_BreakExpr BreakExpr:
					{
						string BreakLbl = State.PeekBreakLabel();
						EmitInstruction(FishInst.JUMP_LONG, BreakLbl);

						break;
					}

				case Expr_WaitExpr WaitExpr:
					{
						EmitInstruction(FishInst.WAIT);
						break;
					}

				case Expr_ContinueExpr ContinueExpr:
					{
						EmitRaw("# Continue");
						string BreakLbl = State.PeekLoopLabel();
						EmitInstruction(FishInst.JUMP_LONG, BreakLbl);

						break;
					}

				case Expr_AddressOfOp AddrOfExpr:
					{
						EmitRaw("# Expr_AddressOfOp BEGIN");
						Indent();

						if (AddrOfExpr.ValExpr is Expr_Identifier Ident)
						{
							if (State.IsVarGlobal(Ident.Identifier))
							{
								bool IsFunc = State.IsVarFunction(Ident.Identifier);

								if (IsFunc)
								{
									EmitInstruction(FishInst.MOVE_LONG_REG, Ident.Identifier, Reg.EAX);
								}
								else
								{
									throw new NotImplementedException();
								}
							}
							else
							{
								FetchIdentifier(Ident.Identifier, 0, false, Reg.EAX, true, false);
							}
						}
						else
							throw new NotImplementedException();

						Unindent();
						EmitRaw("# Expr_AddressOfOp END");
						break;
					}

				case Expr_IndexOp IndexExpr:
					{
						EmitRaw("# IndexOp BEGIN");
						Indent();
						EmitInstruction(FishInst.PUSH_REG, Reg.EBX);

						// DIAGNOSTIC: Print the flag value immediately
						EmitRaw("#: DIAGNOSTIC - IndexEmitOnlyAddress = {0}", State.IndexEmitOnlyAddress ? 1 : 0);

						// IMPORTANT: Capture the flag at the START, before compiling child expressions
						bool onlyAddress = State.IndexEmitOnlyAddress;

						EmitRaw("#: EAX = {0}", IndexExpr.IndexValExpr.ToSourceStr());
						Compile(IndexExpr.IndexValExpr); // index in EAX

						if (IndexExpr.LExpr is Expr_Identifier id)
						{
							Expr_TypeDef idType = State.GetVarType(id.Identifier);
							int elemSize = 1;
							bool isMemberAccess = IndexExpr.IndexValExpr is Expr_MemberAccessOp;

							if (!isMemberAccess)
								elemSize = Expr_TypeDef.GetDerefTypeSize(State.Types, idType);

							EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.EBX); // EBX = index/offset

							// For struct member access, the offset is already in bytes - don't scale it
							// For array indexing, scale by element size
							if (!isMemberAccess && elemSize > 1)
							{
								EmitRaw("#: EBX = EBX * {0}", elemSize);
								EmitInstruction(FishInst.MOVE_LONG_REG, "$" + elemSize, Reg.EAX);
								EmitInstruction(FishInst.MUL_REG, Reg.EBX);
								EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.EBX); // EBX = scaled index
							}

							bool isGlobal = State.IsVarGlobal(id.Identifier);
							bool isPointerType = Expr_TypeDef.IsPointerType(idType);
							bool isArrayType = idType.IsArray;

							EmitRaw("#: {0}[EBX] OnlyAddress: {1} (isGlobal:{2}, isArray:{3}, isPointer:{4})",
								id.Identifier, onlyAddress ? 1 : 0, isGlobal ? 1 : 0, isArrayType ? 1 : 0, isPointerType ? 1 : 0);

							if (onlyAddress)
							{
								// We want the address of the element
								if (isGlobal && isArrayType)
								{
									// Static array: compute address as intermediate + offset
									FetchIdentifier(id.Identifier, 0, /*treatAsPointer*/ false, Reg.EAX, true, true);
								}
								else if (isGlobal && isPointerType)
								{
									// Pointer variable: dereference to get allocated memory, then add offset
									FetchIdentifier(id.Identifier, 0, /*treatAsPointer*/ true, Reg.EAX, true, true);
								}
								else
								{
									// Local variable (pointer or array) or struct member access
									FetchIdentifier(id.Identifier, 0, isPointerType, Reg.EAX, true, true);
								}
							}
							else
							{
								// We want the value at the indexed location
								// For struct member access, use the field size from the struct definition
								int accessSize = elemSize;
								if (isMemberAccess)
								{
									Expr_MemberAccessOp memOp = IndexExpr.IndexValExpr as Expr_MemberAccessOp;
									if (State.Types.TryGetType(idType.Type, out FishTypeDef FT))
										if (FT is FishStructDef FST)
										{
											Expr_TypeDef fieldType = FST.GetFieldType(memOp.MemberName);
											accessSize = Expr_TypeDef.GetRawTypeSize(State.Types, fieldType);
										}
								}
								FetchIdentifier(id.Identifier, accessSize, isPointerType, Reg.EAX, true, true);
							}
						}
						else
						{
							EmitInstruction(FishInst.MOVE_REG_REG, Reg.EAX, Reg.EBX);
							Compile(IndexExpr.LExpr);
							EmitInstruction(FishInst.ADD_REG_REG, Reg.EBX, Reg.EAX);
						}


						EmitInstruction(FishInst.POP_REG, Reg.EBX);
						Unindent();
						EmitRaw("# IndexOp END");
						break;
					}

				case Expr_IncDecOp IncDecExp:
					{
						EmitRaw("# Expr_IncDecOp BEGIN");
						Indent();

						if (IncDecExp.LExpr is Expr_Identifier Id)
						{
							string name = Id.Identifier;
							Expr_TypeDef nameType = State.GetVarType(name);
							int sz = 0;
							bool ispointer = Expr_TypeDef.IsPointerType(nameType);

							if (ispointer)
								sz = Expr_TypeDef.GetDerefTypeSize(State.Types, nameType);
							else
							 sz = Expr_TypeDef.GetRawTypeSize(State.Types, nameType);

							FetchIdentifier(name, sz, ispointer, Reg.EAX, false, false);

							if (IncDecExp.Inc)
								EmitInstruction(FishInst.ADD_LONG_REG, "$" + 1, Reg.EAX);
							else
								EmitInstruction(FishInst.SUB_LONG_REG, "$" + 1, Reg.EAX);

							bool isGlobal = State.IsVarGlobal(name);
							StoreIdentifier(name, sz, ispointer, Reg.EAX, false, isGlobal);
						}
						else
							throw new NotImplementedException();

						Unindent();
						EmitRaw("# Expr_IncDecOp END");
						break;
					}

				case Expr_ReturnStatement ReturnExp:
					{
						EmitRaw("# Expr_ReturnStatement BEGIN");
						Indent();

						if (ReturnExp.RetValExpr != null)
						{
							Compile(ReturnExp.RetValExpr);

							// Check if we're returning a large struct
							if (State.Types.TryGetType(ReturnExp.RetTypeDef.Type, out FishTypeDef FTD))
							{
								EmitRaw("# RETURN STRUCT STATEMENT - size={0}", FTD.Size);
								// EAX contains address of local struct
								// Hidden parameter at 8(%EBP) contains address of return buffer
								EmitInstruction(FishInst.MOVE_OFFSET_REG_REG, 8, Reg.EBP, Reg.EDX);
								EmitCopyBytes(FTD.Size, Reg.EAX, Reg.EDX);
								// Leave EAX pointing to the return buffer (though caller already has this)
								EmitInstruction(FishInst.MOVE_REG_REG, Reg.EDX, Reg.EAX);
							}
						}

						EmitReturn();

						Unindent();
						EmitRaw("# Expr_ReturnStatement END");
						break;
					}

				case Expr_FuncCall FuncCallExp:
					{
						EmitRaw("# Expr_FuncCall BEGIN");
						Indent();

						if (FuncCallExp.Function.Identifier == "__asm")
						{
							foreach (var Arg in FuncCallExp.Arguments)
							{
								if (Arg is Expr_ConstString S)
								{
									EmitRaw(S.RawString);
								}
								else
								{
									throw new NotImplementedException("Only string literals are supported in __asm");
								}
							}
						}
						else if (FuncCallExp.Function.Identifier == "syscall_2")
						{
							EmitRaw("# syscall_2 BEGIN");
							Indent();

							if (FuncCallExp.Arguments.Count != 2)
								throw new Exception("syscall_2 requires exactly 2 arguments");

							Expr_ConstNumber NumExp = FuncCallExp.Arguments[0] as Expr_ConstNumber;
							Expression A0 = FuncCallExp.Arguments[1];

							Compile(A0);
							EmitInstruction(FishInst.PUSH_REG, Reg.EAX);
							EmitInstruction(FishInst.MOVE_LONG_REG, "$" + NumExp.NumberLiteral, Reg.EAX);
							EmitInstruction(FishInst.PUSH_REG, Reg.EAX);

							EmitInstruction(FishInst.SYSCALL_2);

							Unindent();
							EmitRaw("# syscall_2 END");
						}
						else
						{
							EmitRaw("# FuncCall BEGIN - '{0}'", FuncCallExp.Function.ToSourceStr());
							Indent();

							Expr_TypeDef FuncType = State.GetVarType(FuncCallExp.Function.Identifier);


							//if (FuncCallExp.Function.Identifier == "printfloat")
							//	Debugger.Break();

							// Check if function returns a large struct - if so, allocate buffer and pass as hidden param
							bool returnsLargeStruct = false;
							int returnBufferSize = 0;
							if (State.Types.TryGetType(FuncType.Type, out FishTypeDef FT))
							{
								returnsLargeStruct = true;
								returnBufferSize = FT.Size;
								EmitRaw("#: Function returns large struct size={0}, allocating return buffer", FT.Size);
								
								// Allocate space on stack for return value
								EmitInstruction(FishInst.SUB_LONG_REG, (uint)FT.Size, Reg.ESP);
								// Push address of return buffer as hidden first parameter
								EmitInstruction(FishInst.PUSH_REG, Reg.ESP);
							}

							// was: for (int i = 0; i < FuncCallExp.Arguments.Count; i++)
							for (int i = FuncCallExp.Arguments.Count - 1; i >= 0; i--)
							{
								Expression arg = FuncCallExp.Arguments[i];
								EmitCallArg(arg);
							}

							int CleanupSize = FuncCallExp.Arguments.Count * 4;
							if (returnsLargeStruct)
							{
								// Add hidden return buffer parameter to cleanup
								CleanupSize += 4;
							}

							if (FuncType.Type == "funcptr")
							{
								// load function pointer into EAX
								Compile(FuncCallExp.Function);
							}
							else
							{
								EmitInstruction(FishInst.MOVE_LONG_REG, FuncCallExp.Function.Identifier, Reg.EAX);
							}

							EmitInstruction(FishInst.CALL_REG, Reg.EAX);
							EmitInstruction(FishInst.ADD_LONG_REG, (uint)(CleanupSize), Reg.ESP);

							// If returning large struct, the result is now on the stack
							// Pop it into EAX (actually just get the address)
							if (returnsLargeStruct)
							{
								EmitRaw("#: Return buffer is on stack, leaving address in EAX");
								EmitInstruction(FishInst.MOVE_REG_REG, Reg.ESP, Reg.EAX);
							}

							Unindent();
							EmitRaw("# FuncCall END - '{0}'", FuncCallExp.Function.ToSourceStr());
						}

						Unindent();
						EmitRaw("# Expr_FuncCall END");
						break;
					}

				case Expr_DefineExpr DefineExpr:
					{
						// Skip, compile time use only
						break;
					}

				default:
					{
						throw new NotImplementedException("Could not compile expression of type " + Ex.GetType());
					}
			}
		}
	}
}