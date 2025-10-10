void __asm();
void print(const char* str);
void printnum(long num);
void dbg_break();
long input_readline(char* dst);
void* alloc(long bytes);

void fk_init();

char* temp = 0;

void kmain() {
	char* str = "Hello, VM Kernel World!\n";
	long count = 0;
	long i = 0;
	long print_chars = 60;

	fk_init();
	temp = alloc(print_chars + 2);

	i = 0;
	while (i < print_chars + 2) {
		temp[i] = 0;
		i++;
	}

	i = 0;
	while (i < print_chars) {
		temp[i] = 'A' + i;
		i++;
	}

	temp[i++] = '\n';
	temp[i] = 0;
	print(temp);
	print("Done!\n");

	/*temp = alloc(64);

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
	}*/

label:
	__asm("WAIT");
	goto label;
	dbg_break();

	__asm("SYSCALL $0");
}