void syscall(const long num);
void syscall_2(const long num, long arg1);
void dbg_break();
void __asm();

long* int_table;

long input_array = 0;
long input_length = 0;
long input_count = 0;

void print(const char* str) {
	int i = 0;

	while (str[i] != 0) {
		syscall_2(1, str[i]);

		i++;
	}
}

void printnum(long num) {
	syscall_2(2, num);
}

void* alloc(long bytes) {
	void* ptr = bytes;
	syscall_2(4, &ptr);
	return ptr;
}

void handler_int0() {
	print("INT0\n");
}

void handler_int1_keyboardkey(long key) {
	if (key == 257) {
		syscall_2(1, '\n');
	}
	else if (key == 259) {
		syscall_2(1, '\b');
	}
	else {
		syscall_2(1, key);
	}
}

void handler_int2_keyboardchar(long key2) {
	print("Key: '");
	syscall_2(1, key2);

	print("' Num: ");
	printnum(key2);
	print("\n");
}

void fk_init() {
	int_table[0] = &handler_int0;
	int_table[1] = &handler_int1_keyboardkey;
	int_table[2] = &handler_int2_keyboardchar;

	input_length = 0x100;
	input_count = 0;
	input_array = alloc(input_length);

	print("input_array: ");
	printnum(input_array);
	print("\n");
}