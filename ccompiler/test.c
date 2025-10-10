void __asm();
void print(const char* str);
void printnum(long num);
void dbg_break();
long input_readline(char* dst);

void fk_init();

void kmain() {
	char* str = "Hello, VM Kernel World!\n";
	char* temp[64];
	long count = 0;

	fk_init();

	print(str);

	while (1) {
		print("read: ");
		//__asm("SYSCALL $0");

		count = input_readline(temp);
		print("Read chars: ");
		printnum(count);
		print("\n");
		print(temp);
		print("\n");
	}

label:
	goto label;
	dbg_break();

	__asm("SYSCALL $0");
}