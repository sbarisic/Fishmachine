uint* int_table = static uint[3];
//string tmp_chr = static string[2];

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;

struct vec2 {
	uint x;
	uint y;
}

void print(string str) {
	uint i = 0;

	while (str[i] != 0) {
		syscall_2(SYS_PrintChar, str[i]);
		i++;
	}
}

naked void kmain() {
	print("Hello Unit Test World!\n");

	vec2 pos;
	pos.x = 0;

	__asm("SYSCALL $0");
}