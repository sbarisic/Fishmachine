void syscall(const long num);
void syscall_2(const long num, long arg1);
void dbg_break();


void kmain() {
	int i = 0;
	long cur_char = 55;
	char* str = "Hello,world!\n";

	for (i = 0; i < 5; i++) {
		syscall_2(1, 'H');
	}

	dbg_break();
	syscall(0);
}
