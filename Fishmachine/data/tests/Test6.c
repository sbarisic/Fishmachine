uint* int_table = static uint[3];
//string tmp_chr = static string[2];

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;

struct vec2 {
	int x;
	int y;
}

void print(string str) {
	uint i = 0;

	while (str[i] != 0) {
		syscall_2(SYS_PrintChar, str[i]);
		i++;
	}
}

void printnum(uint num) {
	syscall_2(SYS_PrintNum, num);
}

void printvec(vec2 pos) {
	printnum(pos.x);
	print(", ");
	printnum(pos.y);
	print("\n");
}

vec2 vec2_add(vec2 a, vec2 b) {
	vec2 result;
	result.x = a.x + b.x;
	result.y = a.y + b.y;
	return result;
}

void kmain() {
	vec2 pos;
	pos.x = 10;
	pos.y = 20;

	vec2 add;
	add.x = 3;
	add.y = 4;

	pos = vec2_add(pos, add);

	printvec(pos);

	print("Hello Unit Test World!\n");
	__asm("SYSCALL $0");
}