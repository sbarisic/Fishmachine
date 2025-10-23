uint* int_table = static uint[3];
string tmp_chr = static string[2];

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;
define SYS_PrintFloat = 6;

void print(string str) {
	uint i = 0;

	while (str[i] != 0) {
		syscall_2(SYS_PrintChar, str[i]);
		i++;
	}
}

void printnum(uint num) {
	syscall_2(SYS_PrintNum, num);
	print("\n");
}

void printfloat(float flt) {
	syscall_2(SYS_PrintFloat, flt);
	print("\n");
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
	int_table[0] = addrof handler_int0;
	int_table[1] = addrof handler_int1_keyboardkey;
	int_table[2] = addrof handler_int2_keyboardchar;

	float a = 6.0f;
	float b = 7.0f;
	float result = 0.0f;

	print("a (should be 6.0): ");
	printfloat(a);

	print("b (should be 7.0): ");
	printfloat(b);

	print("result (should be 0.0): ");
	printfloat(result);

	result = a + b;
	print("result = a + b (should be 13): ");
	printfloat(result);

	result = b - a;
	print("result = b - a (should be 1): ");
	printfloat(result);

	result = a * b;
	print("result = a * b (should be 42): ");
	printfloat(result);

	result = b / a;
	print("result = b / a (should be 1.16666666667): ");
	printfloat(result);

	//wait;
	__asm("SYSCALL $0");
}