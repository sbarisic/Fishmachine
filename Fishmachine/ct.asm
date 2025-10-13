# VariableDef BEGIN - int_table
    .globl int_table
# VariableDef END - int_table
.globl print
print:
    PUSH_REG %ebp
    MOVE_REG_REG %esp, %ebp
    # VariableDef BEGIN - i
        SUB_LONG_REG $4, %esp
    # VariableDef END - i
    # VariableAssign BEGIN
        MOVE_LONG_REG $0, %eax
        MOVE_REG_OFFSET_REG %eax, -4, %ebp
    # VariableAssign END
    # While BEGIN
        .WHILE_0002:
        # IndexOp BEGIN
            MOVE_OFFSET_REG_REG -4, %ebp, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_OFFSET_REG_REG 8, %ebp, %eax
            ADD_REG_REG %ebx, %eax
            MOVES_OFFSET_REG_REG 0, %eax, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        MOVE_LONG_REG $0, %eax
        CMP_REG_REG %eax, %ebx
        JUMP_IF_ZERO_LONG .ENDWHILE_0004
        # syscall_2 BEGIN
            # IndexOp BEGIN
                MOVE_OFFSET_REG_REG -4, %ebp, %eax
                MOVE_REG_REG %eax, %ebx
                MOVE_OFFSET_REG_REG 8, %ebp, %eax
                ADD_REG_REG %ebx, %eax
                MOVES_OFFSET_REG_REG 0, %eax, %eax
            # IndexOp END
            PUSH_REG %eax
            MOVE_LONG_REG $1, %eax
            PUSH_REG %eax
            SYSCALL_2 
        # syscall_2 END
        # MathOp BEGIN (+)
            PUSH_REG %ebx
            MOVE_OFFSET_REG_REG -4, %ebp, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG $1, %eax
            ADD_REG_REG %ebx, %eax
            POP_REG %ebx
        # MathOp END (+)
        MOVE_REG_OFFSET_REG %eax, -4, %ebp
        JUMP_LONG .WHILE_0002
        .ENDWHILE_0004:
    # While END
    LEAVE 
    RET 
.globl handler_int0
handler_int0:
    PUSH_REG %ebp
    MOVE_REG_REG %esp, %ebp
    # FuncCall BEGIN - print
        MOVE_LONG_REG .L_0007, %eax
        PUSH_REG %eax
        MOVE_LONG_REG print, %eax
        CALL_REG %eax
        ADD_LONG_REG $4, %esp
    # FuncCall END - print
    LEAVE 
    RET 
.globl handler_int1_keyboardkey
handler_int1_keyboardkey:
    PUSH_REG %ebp
    MOVE_REG_REG %esp, %ebp
    LEAVE 
    RET 
.globl handler_int2_keyboardchar
handler_int2_keyboardchar:
    PUSH_REG %ebp
    MOVE_REG_REG %esp, %ebp
    # VariableDef BEGIN - tst
        SUB_LONG_REG $4, %esp
    # VariableDef END - tst
    # VariableAssign BEGIN
        MOVE_LONG_REG .L_000A, %eax
        MOVE_REG_OFFSET_REG %eax, -8, %ebp
    # VariableAssign END
    # Expr_AssignValue BEGIN
        # IndexOp BEGIN
            MOVE_LONG_REG $0, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_OFFSET_REG_REG -8, %ebp, %eax
            ADD_REG_REG %ebx, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        MOVE_OFFSET_REG_REG 8, %ebp, %eax
        MOVEBYTE_REG_OFFSET_REG %eax, 0, %ebx
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
    # Expr_AssignValue END
    # Expr_AssignValue BEGIN
        # IndexOp BEGIN
            MOVE_LONG_REG $1, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_OFFSET_REG_REG -8, %ebp, %eax
            ADD_REG_REG %ebx, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        MOVES_LONG_REG $45, %eax
        MOVEBYTE_REG_OFFSET_REG %eax, 0, %ebx
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
    # Expr_AssignValue END
    # Expr_AssignValue BEGIN
        # IndexOp BEGIN
            MOVE_LONG_REG $2, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_OFFSET_REG_REG -8, %ebp, %eax
            ADD_REG_REG %ebx, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        MOVE_LONG_REG $0, %eax
        MOVEBYTE_REG_OFFSET_REG %eax, 0, %ebx
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
    # Expr_AssignValue END
    # FuncCall BEGIN - print
        MOVE_OFFSET_REG_REG -8, %ebp, %eax
        PUSH_REG %eax
        MOVE_LONG_REG print, %eax
        CALL_REG %eax
        ADD_LONG_REG $4, %esp
    # FuncCall END - print
    LEAVE 
    RET 
.globl kmain
kmain:
    PUSH_REG %ebp
    MOVE_REG_REG %esp, %ebp
    # Expr_AssignValue BEGIN
        # IndexOp BEGIN
            MOVE_LONG_REG $0, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG $4, %eax
            MUL_REG %ebx
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG int_table, %eax
            ADD_REG_REG %ebx, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        # Expr_AddressOfOp BEGIN
            MOVE_LONG_REG handler_int0, %eax
        # Expr_AddressOfOp END
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
    # Expr_AssignValue END
    # Expr_AssignValue BEGIN
        # IndexOp BEGIN
            MOVE_LONG_REG $1, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG $4, %eax
            MUL_REG %ebx
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG int_table, %eax
            ADD_REG_REG %ebx, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        # Expr_AddressOfOp BEGIN
            MOVE_LONG_REG handler_int1_keyboardkey, %eax
        # Expr_AddressOfOp END
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
    # Expr_AssignValue END
    # Expr_AssignValue BEGIN
        # IndexOp BEGIN
            MOVE_LONG_REG $2, %eax
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG $4, %eax
            MUL_REG %ebx
            MOVE_REG_REG %eax, %ebx
            MOVE_LONG_REG int_table, %eax
            ADD_REG_REG %ebx, %eax
        # IndexOp END
        MOVE_REG_REG %eax, %ebx
        # Expr_AddressOfOp BEGIN
            MOVE_LONG_REG handler_int2_keyboardchar, %eax
        # Expr_AddressOfOp END
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
        MOVE_REG_OFFSET_REG %eax, 0, %ebx
    # Expr_AssignValue END
    # FuncCall BEGIN - print
        MOVE_LONG_REG .L_000C, %eax
        PUSH_REG %eax
        MOVE_LONG_REG print, %eax
        CALL_REG %eax
        ADD_LONG_REG $4, %esp
    # FuncCall END - print
    # While BEGIN
        .WHILE_000D:
        NOP 
        JUMP_LONG .WHILE_000D
        .ENDWHILE_000F:
    # While END
    SYSCALL $0
    LEAVE 
    RET 
.L_0007:
    .String "INT0\n"
.L_000A:
    .String "Hello"
.L_000C:
    .String "Hello, World!\n"
