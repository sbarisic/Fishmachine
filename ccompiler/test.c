void syscall(const long num);
void syscall_2(const long num, long arg1);
void dbg_break();


void kmain() {
	long cur_char = 55;
	char* str = "Hello,world!\n";

	cur_char = str[0];
	syscall_2(1, cur_char);

	dbg_break();
	syscall(0);
}
