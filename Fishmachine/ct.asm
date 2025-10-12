.globl print
print:
    PUSH_REG %ebp
    MOVE_REG_REG %esp, %ebp
    SUB_LONG_REG $4, %esp
    MOVE_LONG_REG $0, %eax
    MOVE_REG_OFFSET_REG %eax, -4, %ebp
    .WHILE_0000:
    MOVE_OFFSET_REG_REG -4, %ebp, %eax
    MOVE_REG_REG %eax, %ebx
    MOVE_OFFSET_REG_REG 8, %ebp, %eax
    ADD_REG_REG %ebx, %eax
    MOVES_OFFSET_REG_REG 0, %eax, %eax
    MOVE_REG_REG %eax, %ebx
    MOVE_LONG_REG $0, %eax
    CMP_REG_REG %eax, %ebx
    JUMP_IF_ZERO_LONG .ENDWHILE_0001
    MOVE_OFFSET_REG_REG -4, %ebp, %eax
    MOVE_REG_REG %eax, %ebx
    MOVE_OFFSET_REG_REG 8, %ebp, %eax
    ADD_REG_REG %ebx, %eax
    MOVES_OFFSET_REG_REG 0, %eax, %eax
    PUSH_REG %eax
    MOVE_LONG_REG $1, %eax
    PUSH_REG %eax
    SYSCALL_2 
    MOVE_OFFSET_REG_REG -4, %ebp, %eax
    MOVE_REG_REG %eax, %ebx
    MOVE_LONG_REG $1, %eax
    ADD_REG_REG %ebx, %eax
    MOVE_REG_OFFSET_REG %eax, -4, %ebp
    JUMP_LONG .WHILE_0000
    .ENDWHILE_0001:
    LEAVE 
    RET 
.globl kmain
kmain:
    MOVE_LONG_REG .L_0002, %eax
    PUSH_REG %eax
    MOVE_LONG_REG print, %eax
    CALL_REG %eax
    ADD_LONG_REG $4, %esp
    SYSCALL $0
.L_0002:
    .String "Hello, World!\n"
