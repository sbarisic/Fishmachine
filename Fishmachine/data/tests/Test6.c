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

void vec2_add(vec2 a, vec2 b, vec2* result) {
	result.x = a.x + b.x;
	result.y = a.y + b.y;
}

vec2 vec2_add2(vec2 a, vec2 b) {
	vec2 result;
	result.x = a.x + b.x;
	result.y = a.y + b.y;
	return result;
}

/*void vec2_mul(vec2 a, int ml, vec2* result) {
	result.x = a.x * ml;
	result.y = a.y * ml;
}

void func1() {
	print("Func1 called!\n");
}

void func2() {
	print("Func2 called!\n");
}

void func3(int num) {
	print("Func3 called with num: ");
	printnum(num);
	print("\n");
}

int addf(int a, int b) {
	return (a + b);
}*/

void kmain() {
	vec2 a;
	vec2 b;
	vec2 tst;

	a.x = 1;
	b.x = 2;

	a.y = 5;
	b.y = 4;
	
	tst = vec2_add2(a, b);
	printvec(tst);

	/*int result = 0;
	funcptr f2 = addrof func2;

	f2();
	f2 = addrof func3;
	f2(9);

	result = addf(2, 3);
	printnum(result);
	print("\n");

	vec2 pos;
	pos.x = 10;
	pos.y = 20;

	vec2 add;
	add.x = 3;
	add.y = 4;

	print("Structs\n");

	vec2_add(pos, add, addrof pos);
	printvec(pos);

	vec2_mul(pos, 2, addrof pos);
	printvec(pos);

	pos.x = 1;
	pos.y = 1;
	add = vec2_add2(pos, add);
	printvec(add);

	print("Function pointers\n");

	func1();

	print("f2 addr: ");
	int f2_addr = addrof func2;
	printnum(f2_addr);
	print("\n");*/




	__asm("SYSCALL $0");
}