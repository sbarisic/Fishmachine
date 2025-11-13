uint* int_table = static uint[3];
//string tmp_chr = static string[2];

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;

struct vec2 {
	byte a;
	byte b;
	byte c;
	byte d;
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
	print("\n");
}

void test2(int vr, int br) {
	printnum(vr);
	printnum(br);
}

void test(vec2* pos) {
	printnum(pos.a);
	printnum(pos.b);
	printnum(pos.c);
	printnum(pos.d);

	print("Hai\n");
}

void kmain() {
	vec2 pos;


	pos.a = 1;
	pos.b = 2;
	pos.c = 3;
	pos.d = 4;
	//test(pos);

	//pos.a = pos.c;

	test(addrof pos);

	print("Hello Unit Test World!\n");
	__asm("SYSCALL $0");
}