uint* int_table;
string temp_buffer = null;
string temp_buffer2 = null;
int input_length = 50;
int input_count = 0;

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;

voidptr alloc(uint bytes) {
	voidptr alloc_mem = bytes;
	syscall_2(SYS_Alloc, &alloc_mem);
	return alloc_mem;
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

string tmp_chr = static string[2];

void printchar(char c) {
	tmp_chr[0] = c;
	tmp_chr[1] = 0;
	print(tmp_chr);
}

void memory_copy(string dest, string source, int len) {
	uint i = 0;
	char tmp = 0;


	while (i < len) {
		tmp = source[i];
		dest[i] = tmp;
		i++;
	}
}

void memory_clear(string dest, int len) {
	uint i = 0;

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

void input_add(char c) {
	//print("AC\n");
	//syscall_2(2, c);

	if (c == '\b') {
		if (input_count > 0) {
			input_count--;
			temp_buffer[input_count] = 0;
			printchar('\b');
			printchar(' ');
			printchar('\b');
			return;
		}

		return;
	}

	// Only add if there is space for at least one more character (for newline)
	if (input_count < input_length - 1) {
		temp_buffer[input_count] = c;
		input_count++;
		printchar(c);
	}
}

int input_readline(string dst) {
	int ret = 0;
	int idx = 0;
	char chr = 0;
	dst[0] = 0;

	while (true) {
		if (input_count == 0) {
			wait;
			continue;
		}

		idx = input_count - 1;
		chr = temp_buffer[idx];
		/*printchar('\n');
		print("-chr = '");
		printchar(chr);
		print("'\n");*/
		
		if (chr == '\n') {			
			memory_copy(dst, temp_buffer, idx);
			ret = input_count - 1;
			dst[ret] = 0;
			//memory_clear(input_array, input_count);
			input_count = 0;
			temp_buffer[0] = 0;

			return ret;
		}

		wait;
	}

	return ret;
}

interrupt void handler_int0() {
	print("INT0\n");
}

interrupt void handler_int1_keyboardkey(uint key) {
}

interrupt void handler_int2_keyboardchar(uint key2) {
	input_add(key2);
}

naked void kmain() {
	temp_buffer = alloc(100);
	temp_buffer2 = alloc(100);

	int_table[0] = &handler_int0;
	int_table[1] = &handler_int1_keyboardkey;
	int_table[2] = &handler_int2_keyboardchar;

	print("Hello, Universe!\n");

	while (true) {
		print("In: ");
		input_readline(temp_buffer2);
		print("You typed: ");
		print(temp_buffer2);
		print("\n");
	}

	__asm("SYSCALL $0");
}