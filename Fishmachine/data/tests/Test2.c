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

naked void kmain() {
	int_table[0] = &handler_int0;
	int_table[1] = &handler_int1_keyboardkey;
	int_table[2] = &handler_int2_keyboardchar;

	uint a = 3;
	uint b = 4;

	test_print((true), "true");
	test_print((false), "false");
	test_print((1 == 1), "1 == 1");
	test_print((1 != 1), "1 != 1");
	test_print((1 == 0), "1 == 0");
	test_print((1 != 0), "1 != 0");
	test_print((1 > 1), "1 > 1");
	test_print((1 < 1), "1 < 1");
	test_print((1 >= 1), "1 >= 1");
	test_print((1 <= 1), "1 <= 1");
	test_print((1 > 2), "1 > 2");
	test_print((1 < 2), "1 < 2");
	test_print((1 >= 2), "1 >= 2");
	test_print((1 <= 2), "1 <= 2");
	test_print((a == 3), "a == 3");
	test_print((b == 4), "b == 4");
	test_print(((a + b) == 7), "(a + b) == 7");
	test_print(((a + 1) == 4), "(a + 1) == 4");
	test_print(((a - 1) == 2), "(a - 1) == 2");
	test_print(((a * 1) == 3), "(a * 1) == 3");
	test_print(((b / 2) == 2), "(b / 2) == 2");
	test_print((a == 9), "a == 9");
	test_print((b == 9), "b == 9");
	test_print(((a + b) == 9), "(a + b) == 9");
	test_print(((a + 1) == 9), "(a + 1) == 9");
	test_print(((a - 1) == 9), "(a - 1) == 9");
	test_print(((a * 1) == 9), "(a * 1) == 9");
	test_print(((b / 2) == 9), "(b / 2) == 9");


	print("Hello Unit Test World!\n");

	__asm("SYSCALL $0");
}