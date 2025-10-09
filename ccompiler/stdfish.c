void syscall(const long num);
void syscall_2(const long num, long arg1);
void dbg_break();
void __asm();

long* int_table;

void print(const char* str) {
	int i = 0;

	while (str[i] != 0) {
		syscall_2(1, str[i]);

		i++;
	}
}

void handler_int0() {
	print("INT0\n");
}

void handler_int1_keyboardkey(long key) {
	print("INT1\n");
}

void fk_init() {
	int_table[0] = &handler_int0;
	int_table[1] = &handler_int1_keyboardkey;
}