void syscall_2(const long num, long arg1);

void print(const char* str) {
	int i = 0;

	while (str[i] != 0) {
		syscall_2(1, str[i]);

		i++;
	}
}