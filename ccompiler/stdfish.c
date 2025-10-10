void syscall(const long num);
void syscall_2(const long num, long arg1);
void dbg_break();
void __asm();

long* int_table;

char* input_array = 0;
long input_length = 0;
long input_count = 0;

void print(const char* str) {
	int i = 0;

	while (str[i] != 0) {
		syscall_2(1, str[i]);

		i++;
	}
}

void printnum(long num) {
	syscall_2(2, num);
}

void* alloc(long bytes) {
	void* ptr = bytes;
	syscall_2(4, &ptr);
	return ptr;
}

void handler_int0() {
	print("INT0\n");
}

void input_add(char c) {
	if (c == '\b') {
		if (input_count > 0) {
			input_count--;
			input_array[input_count] = 0;
			return;
		}
	}

	input_array[input_count] = c;
	input_count++;

	if (input_count >= input_length) {
		input_count--;
	}

	syscall_2(1, c);
}

void memory_copy(char* dest, char* source, long len) {
	long i = 0;

	while (i < len) {
		dest[i] = source[i];
		i++;
	}
}

void memory_clear(char* dest, long len) {
	long i = 0;

	print("clear ");
	printnum(dest);

	print("\nlen ");
	printnum(len);
	print("\n");

	while (i < len) {
		dest[i] = 0;
		i++;
	}
}

long input_readline(char* dst) {
	long ret = 0;
	long chr = 0;
	long idx = 0;
	dst[0] = 0;

readl_loop:

	if (input_count == 0) {
		__asm("WAIT");
		goto readl_loop;
	}

	idx = input_count - 1;
	chr = input_array[idx];

	if (chr == '\n') {
		memory_copy(dst, input_array, idx);
		ret = input_count - 1;
		dst[ret] = 0;
		//memory_clear(input_array, input_count);
		input_count = 0; 
		input_array[0] = 0;
		return ret;
	}
	else {
		__asm("WAIT");
	}

	goto readl_loop;
	return ret;
}

void handler_int1_keyboardkey(long key) {
	if (key == 257) {
		input_add('\n');
	}
	else if (key == 259) {
		input_add('\b');
	}
	else {
		input_add((char)key);
	}
}

void handler_int2_keyboardchar(long key2) {
	//print("Key: '");
	input_add((char)key2);

	//print("' Num: ");
	//printnum(key2);
	//print("\n");
}

void fk_init() {
	int_table[0] = &handler_int0;
	int_table[1] = &handler_int1_keyboardkey;
	int_table[2] = &handler_int2_keyboardchar;

	input_length = 0x100;
	input_count = 0;
	input_array = alloc(input_length);
}