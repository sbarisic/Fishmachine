void syscall(const long num);
void syscall_2(const long num, long arg1);
void dbg_break();

void print(const char* str);

void kmain() {
	char* str = "Hello, world!\n";
	float a = 2.4f;
	float b = 3.5f;
	float c = 0.0f;
	// Test comment

	c = a + b;

	print(str);

	syscall(0);
}

void print(const char* str) {
	int i = 0;

	while (str[i] != 0) {
		syscall_2(1, str[i]);

		i++;
	}
}