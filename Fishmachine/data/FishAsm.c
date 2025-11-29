uint* int_table = static uint[4];
string tmp_chr = static string[2];
string temp_buffer = null;
string temp_buffer2 = null;

int input_length = 50;
int input_count = 0;

define SYS_StopMachine = 0;
define SYS_PrintChar = 1;
define SYS_PrintNum = 2;
define SYS_SoftwareInterrupt = 3;
define SYS_Alloc = 4;
define SYS_Cls = 5;

voidptr alloc(uint bytes) {
	voidptr alloc_mem = bytes;
	syscall_2(SYS_Alloc, addrof alloc_mem);
	return alloc_mem;
}

void clear_screen() {
	syscall_2(SYS_Cls, 0);
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

void printnum2(uint num) {
	printnum(num);
	print("\n");
}

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

	//print("clear ");
	//printnum2(dest);

	//print("len ");
	//printnum2(len);

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
			input_count = input_count - 1;
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
		input_count = input_count + 1;

		__asm("DBG_MEM");
		printchar(c);
	}
}

uint cmp(string stra, string strb) {
	uint i = 0;

	while (((stra[i] != 0) && (strb[i] != 0)) == true) {
		if (stra[i] != strb[i]) {
			return 0;
		}

		i = i + 1;
	}

	// both must end at same position
	if (((stra[i] == 0) && (strb[i] == 0)) == true) {
		return 1;
	}

	return 0;
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
			memory_clear(temp_buffer, input_count);
			input_count = 0;
			temp_buffer[0] = 0;

			__asm("DBG_MEM");
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

interrupt attribute("retaddr") void handler_exception(uint exc) {
	print("Exception! ");
	printnum(exc);
	print("\n");
	__asm("SYSCALL $0");
}

naked void kmain() {
	//int_table = 32;
	//__asm("DBG_BREAK");

	int_table[0] = addrof handler_int0;
	int_table[1] = addrof handler_int1_keyboardkey;
	int_table[2] = addrof handler_int2_keyboardchar;
	int_table[3] = addrof handler_exception;
	byte* bptr = 0;

	//__asm("SYSCALL $0");

	__asm("DBG_MEM");
	temp_buffer = alloc(100);
	temp_buffer2 = alloc(100);

	__asm("DBG_MEM");
	print("Hello, Universe!\n");

	while (true) {
		print("In: ");
		input_readline(temp_buffer2);

		if (cmp(temp_buffer2, "exit") == true) {
			print("Exiting...\n");
			break;
		}
		else if (cmp(temp_buffer2, "clear") == true) {
			clear_screen();
		}
		else if (cmp(temp_buffer2, "null") == true) {
			bptr = 0;
			bptr[0] = 32;
		}
		else if (cmp(temp_buffer2, "test") == true) {
			print("This is a test!\n");
		}
		else if (cmp(temp_buffer2, "long") == true) {
			print("This is a test!\n");
			print(" THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG\n");
			print(" the quick brown fox jumps over the laz dog\n");
			print(" 0123456789 !@#$%^&*()_+-=[]{}|;':,.<>/?`~\n");
			print(" Lorem ipsum dolor sit amet, consectetur adipiscing elit,\n"); 
			print(" sed do eiusmod tempor incididunt\n");
			print(" ut labore et dolore magna aliqua.\n");
			print(" Ut enim ad minim veniam,\n");
			print(" quis nostrud exercitation ullamco laboris\n");
			print(" nisi ut aliquip ex ea commodo consequat.\n");
			print(" Duis aute irure dolor in reprehenderit\n");
			print(" in voluptate velit esse cillum dolore eu fugiat nulla pariatur.\n");
			print(" Excepteur sint occaecat cupidatat non proident, sunt in\n");
			print(" culpa qui officia deserunt mollit anim id est laborum.\n");
			print(" sed do eiusmod tempor incididunt\n");
			print(" ut labore et dolore magna aliqua.\n");
			print(" Ut enim ad minim veniam,\n");
			print(" quis nostrud exercitation ullamco laboris\n");
			print(" nisi ut aliquip ex ea commodo consequat.\n");
			print(" Duis aute irure dolor in reprehenderit\n");
			print(" in voluptate velit esse cillum dolore eu fugiat nulla pariatur.\n");
			print(" Excepteur sint occaecat cupidatat non proident, sunt in\n");
			print(" culpa qui officia deserunt mollit anim id est laborum.\n");
		}
		else if (cmp(temp_buffer2, "barely") == true) {
			bptr = 0x101;
			bptr[0] = 32;
		}
		else if (cmp(temp_buffer2, "forloop") == true) {
			for (uint i = 0; i < 5; i = i + 1) {
				print("Forloop! ");
			}

			print("\nDone!\n");
		}
		else {
			print("You typed: ");
			print(temp_buffer2);
			print("\n");
		}
	}

	__asm("SYSCALL $0");
}