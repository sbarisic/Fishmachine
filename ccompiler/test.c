void __asm();
void print(const char* str);
void dbg_break();

long* int_table;
void int1_handler();

void kmain() {
	char* str = "Hello, linker world!\n";
	// Test comment

	int_table[1] = &int1_handler;
	dbg_break();

	__asm("SYSCALL $2");
	dbg_break();

	print(str);
	__asm("SYSCALL $0");
}

void int1_handler() {
	print("INT 1 occurred!\n");
}