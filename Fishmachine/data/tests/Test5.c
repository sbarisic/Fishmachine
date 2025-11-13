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

void test_print(bool cond, string text) {
	print(text);

	if (cond == true) {
		print(" - True\n");
	}
	else {
		print(" - False\n");
	}
}

bool get_true() {
	print("+");
	return true;
}

bool get_false() {
	print("-");
	return false;
}

naked void kmain() {
	int_table[0] = addrof handler_int0;
	int_table[1] = addrof handler_int1_keyboardkey;
	int_table[2] = addrof handler_int2_keyboardchar;

	uint a = 3;
	uint b = 4;

	test_print((true && true), "true && true");
	test_print((true && false), "true && false");
	test_print((false && true), "false && true");
	test_print((false && false), "false && false");


	test_print((true || true), "true || true");
	test_print((true || false), "true || false");
	test_print((false || true), "false || true");
	test_print((false || false), "false || false");


	test_print((get_true() && get_true()), "get_true() && get_true()");
	test_print((get_true() && get_false()), "get_true() && get_false()");
	test_print((get_false() && get_true()), "get_false() && get_true()");
	test_print((get_false() && get_false()), "get_false() && get_false()");


	test_print((get_true() || get_true()), "get_true() || get_true()");
	test_print((get_true() || get_false()), "get_true() || get_false()");
	test_print((get_false() || get_true()), "get_false() || get_true()");
	test_print((get_false() || get_false()), "get_false() || get_false()");

	print("Hello Unit Test World!\n");

	__asm("SYSCALL $0");
}