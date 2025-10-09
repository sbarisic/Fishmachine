void __asm();
void print(const char* str);
void dbg_break();

void fk_init();

void kmain() {
	char* str = "Hello, VM Kernel World!\n";

	fk_init();

	print(str);
	//__asm("SYSCALL $0");

label:
	goto label;
	dbg_break();

	__asm("SYSCALL $0");
}