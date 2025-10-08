void __asm();
void print(const char* str);

void kmain() {
	char* str = "Hello, linker world!\n";
	float a = 2.4f;
	float b = 3.5f;
	float c = 0.0f;
	// Test comment

	int x = 52;
	int y = 8;
	int z = x + y;

	c = a + b;
	__asm("SYSCALL $5", x);
	__asm("SYSCALL $5", y);
	__asm("SYSCALL $5", z);

	print(str);
	__asm("SYSCALL $0");
}
