uint* int_table = static uint[3];
//string tmp_chr = static string[2];

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;

void print(string str) {
	uint i = 0;

	while (str[i] != 0) {
		syscall_2(SYS_PrintChar, str[i]);
		i++;
	}
}

struct vec2 {
	int x;
	int y;
}

struct vecs {
	int x;
	int y;
	string str;

	__ctor() {
		print("vecs constructor called\n");
		this.x = 100;
		this.y = 69;
		this.str = "default string";
	}
}

void printnum(uint num) {
	syscall_2(SYS_PrintNum, num);
}

void printvec(vec2 pos) {
	print("vec2: ");
	printnum(pos.x);
	print(", ");
	printnum(pos.y);
	print("\n");
}

void printvecs(vecs vv) {
	print("vecs: ");
	printnum(vv.x);
	print(", ");
	printnum(vv.y);
	print(", ");

	if (vv.str != null) {
		print(vv.str);
	}
	else {
		print("null");
	}

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

void vec2_mul(vec2 a, int ml, vec2* result) {
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
}

void test_switch(int i) {
	switch (i) {

	case 0:
		print("case 0\n");
		break;

	case 1:
	case 2:
		print("case 1 or 2\n");
		break;

	default:
		print("case default\n");
		break;
	}
}

void kmain() {
	vec2 a;
	vec2 b;
	vec2 tst;
	string teststring = "Hello test string";

	a.x = 1;
	b.x = 2;

	a.y = 5;
	b.y = 4;

	int result = 0;
	funcptr f2 = addrof func2;

	string str = "hello";

	switch (str) {
	case "hello":
		print("string case hello\n");
		break;

	case "world":
		print("string case world\n");
		break;

	default:
		print("string case default\n");
		break;
	}

	test_switch(0);
	test_switch(1);
	test_switch(2);
	test_switch(3);

	vecs vv = new vecs;
	printvecs(vv);

	vv.x = 10;
	vv.y = 20;
	vv.str = teststring;

	printvecs(vv);

	f2();
	f2 = addrof func3;
	f2(9);

	result = addf(2, 3);
	printnum(result);
	print("\n");

	vec2 pos = new vec2;
	printvec(pos);

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

	/*print("f2 addr: ");
	int f2_addr = addrof func2;
	printnum(f2_addr);
	print("\n");*/



	__asm("SYSCALL $0");
}