uint* int_table = static uint[3];
string tmp_chr = static string[2];

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;

void print(string str) {
	uint i = 0;

	while (str[i] != 0) {
		syscall_2(SYS_PrintChar, str[i]);
		i++;
	}
}

interrupt void handler_int0() {
	print("INT0\n");
}

interrupt void handler_int1_keyboardkey(uint key) {
}

interrupt void handler_int2_keyboardchar(uint key2) {
	tmp_chr[0] = key2;
	tmp_chr[1] = 0;
	print(tmp_chr);
}

naked void kmain() {
	int_table[0] = &handler_int0;
	int_table[1] = &handler_int1_keyboardkey;
	int_table[2] = &handler_int2_keyboardchar;

	print("Hello Unit Test World!\n");

	__asm("SYSCALL $0");
}