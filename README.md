# Fishmachine

Custom virtual machine with a C-like compiler and assembler that compiles and runs a small program on a simulated CPU/GPU.

- Language/runtime: C# 12 on .NET 8
- Outputs:
  - ct.asm: assembly generated from data/FishAsm.c by the CTilde compiler
  - out.asm: combined assembly sent to the assembler
  - bytecode.bin: linked VM bytecode
  - vm_out.txt: executed instruction trace and state logs

## Overview
- FishVM: a small virtual machine with registers, stack, memory, and a simple graphics text output.
- CTilde compiler: parses a C-like language and emits Fish assembly.
- Assembler/Linker: turns assembly into VM bytecode with symbols.
- Runtime: loads bytecode, jumps to kmain, and runs with simple syscalls and interrupts.

## Features
- General-purpose registers (EAX, EBX, ECX, EDX, EBP, ESP, EDI, ESI) + flags and FPU helpers.
- Linear memory with simple allocator (stack grows, heap-like downward pointer).
- Syscalls used by the sample program:
  - 0: StopMachine
  - 1: PrintChar
  - 2: PrintNum
- Keyboard character interrupt (Int2_KeyboardChar) integration with the graphics window.

## Repository layout
- Fishmachine/ … app entry point, VM, graphics, and CTilde integration
- ccompiler/ … codegen helpers and instruction emission utilities
- Fishmachine/data/FishAsm.c … sample CTilde program compiled and run by the VM

## Build
Prerequisites: .NET 8 SDK

- Build all projects:
  - dotnet build
- Run the VM and sample program:
  - dotnet run -p Fishmachine/Fishmachine.csproj

Artifacts (in bin/Debug/net8.0 by default):
- ct.asm, out.asm, bytecode.bin, vm_out.txt

## Running and interacting
- On start, the CTilde compiler compiles Fishmachine/data/FishAsm.c to ct.asm.
- The assembler links it to bytecode.bin and the VM jumps to kmain.
- The graphics window shows output. Typing sends characters via Int2_KeyboardChar.
- The sample echoes input lines as “You typed: <line>”. Backspace is handled.

## Editing the program
- Edit Fishmachine/data/FishAsm.c (C-like syntax). Example snippets used:
  - syscall_2(1, c) prints a character
  - syscall_2(2, n) prints an unsigned number
  - __asm("SYSCALL $0") stops the VM
- Re-run the app to rebuild ct.asm/out.asm/bytecode.bin and execute.

## Debugging and logs
- Toggle options in Fishmachine/FishSettings.cs (e.g., DebugPrintSyscall, DebugPrintRegisters, FormatPrint).
- Inspect vm_out.txt for instruction/state logs and ct.asm/out.asm to see generated assembly.

## Status
- Experimental/WIP. APIs, instruction set, and CTilde syntax may change.

## Roadmap (high level)
- Broader instruction set and FPU ops coverage
- Richer I/O and device model
- Better debugger/tracing and disassembler
- CTilde language features and standard library expansion
- Tests and examples

## License
MIT License. See LICENSE.md