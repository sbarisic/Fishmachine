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

void func_call() {
	print("func_call() - PASS\n");
	return;

	print("return; - FAILED\n");
}

naked void kmain() {
	int_table[0] = addrof handler_int0;
	int_table[1] = addrof handler_int1_keyboardkey;
	int_table[2] = addrof handler_int2_keyboardchar;

	print("Hello Unit Test World!\n");

	if (true) {
		print("if (true) - True\n");
	}
	else {
		print("if (true) - False\n");
	}

	while (true) {
		print("while (true) - True\n");
		break;
		print("break; - FAILED\n");
	}
	print("break; - PASSED\n");

	func_call();
	print("return; - PASSED\n");

	print("End of Test1\n");
	__asm("SYSCALL $0");
}