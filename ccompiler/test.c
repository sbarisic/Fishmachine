void syscall(const long num);
void dbg_break();


void kmain() {
	int cur_char = 55;
char* str = "Hello,world!\n";

	cur_char = str[2];

	dbg_break();

	syscall(0);
}
