int syscall();
void exit();
void print(const char* str);

void kmain() {
	print("Hello_Fishmachine_World!\n");

	exit();
}

void exit() {
	syscall(0);
}

void print(const char* str) {
	int idx = 0;

	while (1) {
		if (str[idx] == 0)
			break;

		syscall(1, str[idx]);
		idx++;
	}
}
