# AIL Specification — Version 2.0 (Cross-Platform Edition)

> This page is the normative specification for AIL-compliant virtual machines.
> It supersedes all prior drafts. Platform-specific implementation guidance is provided in the appendices.

---

## Table of Contents

1. [Memory](#1-memory)
2. [Instruction Encoding](#2-instruction-encoding)
   - [2.1 Addressing Modes](#21-addressing-modes)
3. [Registers](#3-registers)
4. [The Stack](#4-the-stack)
5. [Program Flow](#5-program-flow)
6. [Instruction Reference](#6-instruction-reference)
   - [6.1 Register & Memory Operations](#61-register--memory-operations)
   - [6.2 Arithmetic](#62-arithmetic)
   - [6.3 Bitwise Operations](#63-bitwise-operations)
   - [6.4 Flow Control](#64-flow-control)
   - [6.5 Stack Manipulation](#65-stack-manipulation)
   - [6.6 I/O](#66-io)
   - [6.7 Interrupts](#67-interrupts)
7. [Quick Reference Table](#7-quick-reference-table)
8. [Executable Format](#8-executable-format)

---

## 1. Memory

AIL defines a 16-bit address space providing 65,536 (64 KB) bytes of byte-addressable memory. All multi-byte values are stored in **little-endian** format. The address space is partitioned as follows:

| Start    | End       | Purpose                                                   |
|----------|-----------|-----------------------------------------------------------|
| `0x0000` | `0x01FD`  | Interrupt Vector Table (IVT) — 255 × 2-byte entries       |
| `0x01FE` | `0x01FF`  | Reserved                                                  |
| `0x0200` | `SP − 1`  | General-purpose program memory (code and data)            |
| `SP`     | `0xFFFF`  | Stack (grows downward from top of memory)                 |

The IVT occupies the first 510 bytes (`0x0000`–`0x01FD`), providing space for 255 two-byte handler addresses. Bytes `0x01FE`–`0x01FF` are reserved for future use.

The boundary between program memory and the stack is dynamic: the stack grows downward from the top of memory, controlled by the Stack Segment (SS) and Stack Pointer (SP) registers. Programs must not write into the stack region and the stack must not overflow into program memory.

---

## 2. Instruction Encoding

Every AIL instruction is exactly **48 bits (6 bytes)** wide with the following fixed layout:

| Bits   | Width   | Field        | Description                                          |
|--------|---------|--------------|------------------------------------------------------|
| 47–42  | 6 bits  | Opcode       | Identifies the instruction                           |
| 41–40  | 2 bits  | Address Mode | How parameters are interpreted (see §2.1)            |
| 39–32  | 8 bits  | Parameter 1  | First operand (register byte or immediate)           |
| 31–0   | 32 bits | Parameter 2  | Second operand (register byte or 32-bit immediate)   |

### 2.1 Addressing Modes

The two-bit address mode field controls how the VM interprets each parameter:

| Binary | Hex    | Mode                        |
|--------|--------|-----------------------------|
| `00`   | `0x00` | Register : Register         |
| `01`   | `0x01` | Value : Register            |
| `10`   | `0x02` | Register : Value            |
| `11`   | `0x03` | Value : Value               |

---

## 3. Registers

The VM must provide the following registers. All are represented by a single byte in the range `0xF0`–`0xFE`.

| Register | Byte   | Width   | Description                                      |
|----------|--------|---------|--------------------------------------------------|
| `PC`     | `0xF0` | 8-bit   | Program Counter — address of the next instruction |
| `IP`     | `0xF1` | 8-bit   | Instruction Pointer — current execution point    |
| `SP`     | `0xF2` | 8-bit   | Stack Pointer — top of stack (read-only)         |
| `SS`     | `0xF3` | 8-bit   | Stack Segment — base address of the stack        |
| `A`      | `0xF4` | 16-bit  | General purpose (composed of AL + AH)            |
| `AL`     | `0xF5` | 8-bit   | Lower byte of A                                  |
| `AH`     | `0xF6` | 8-bit   | Higher byte of A                                 |
| `B`      | `0xF7` | 16-bit  | General purpose (composed of BL + BH)            |
| `BL`     | `0xF8` | 8-bit   | Lower byte of B                                  |
| `BH`     | `0xF9` | 8-bit   | Higher byte of B                                 |
| `C`      | `0xFA` | 16-bit  | General purpose (composed of CL + CH)            |
| `CL`     | `0xFB` | 8-bit   | Lower byte of C                                  |
| `CH`     | `0xFC` | 8-bit   | Higher byte of C                                 |
| `X`      | `0xFD` | 32-bit  | General purpose                                  |
| `Y`      | `0xFE` | 32-bit  | General purpose                                  |

**SP is read-only.** It holds the absolute address of the top of the stack and must not be written by program code.

---

## 4. The Stack

The stack grows **downward** from the top of memory. Its base address is set by the Stack Segment (`SS`) register, which the program may configure; the VM sets a sensible default on startup. The Stack Pointer (`SP`) tracks the current top of the stack and is updated automatically by `PSH` and `POP`.

---

## 5. Program Flow

Execution begins at the first byte of program memory and proceeds sequentially — one 6-byte instruction at a time — until a flow-control instruction is encountered or a `KEI 0x02` (halt) interrupt is issued.

Example (assembly pseudocode):

```
NOP
JMP halt
halt:
CLI
HLT
```

`NOP` is at relative address `0x00`, `JMP` at `0x01`, `CLI` at `0x03`, and so on. A `JMP halt` is encoded as `0x10 <address-of-halt>`.

---

## 6. Instruction Reference

### 6.1 Register & Memory Operations

#### MOV — Move `0x01`
| | |
|-|-|
| **Parameters** | `[dest: register]`, `[src: register or value]` |
| **Addressing modes** | `RegReg`, `RegVal` |
| **Description** | Copies `src` into `dest`. The destination must be a register; the source may be a register or an immediate value. |

#### MOM — Move to Memory `0x3A`
| | |
|-|-|
| **Parameters** | `[src: register or value]`, `[dest: memory address]` |
| **Addressing modes** | `RegVal`, `ValVal` |
| **Description** | Writes `src` to the memory address given by `dest`. |

#### MOE — Move from Memory `0x3B`
| | |
|-|-|
| **Parameters** | `[dest: register]`, `[src: memory address]` |
| **Addressing modes** | `ValVal` |
| **Description** | Reads the byte at `src` in memory and places it into `dest`. |

#### SWP — Swap `0x02`
| | |
|-|-|
| **Parameters** | `[reg1: register]`, `[reg2: register]` |
| **Addressing modes** | `RegReg` |
| **Description** | Swaps the values stored in the two registers. |

#### TEQ — Test Equal `0x1A`
| | |
|-|-|
| **Parameters** | `[reg1: register]`, `[reg2: register]` |
| **Description** | Sets the logic flag if `reg1 == reg2`. The next conditional instruction will act on this result. |

#### TNE — Test Not Equal `0x1B`
| | |
|-|-|
| **Parameters** | `[reg1: register]`, `[reg2: register]` |
| **Description** | Sets the logic flag if `reg1 != reg2`. |

#### TLT — Test Less Than `0x1C`
| | |
|-|-|
| **Parameters** | `[reg1: register]`, `[reg2: register]` |
| **Description** | Sets the logic flag if `reg1 < reg2`. |

#### TMT — Test More Than `0x1D`
| | |
|-|-|
| **Parameters** | `[reg1: register]`, `[reg2: register]` |
| **Description** | Sets the logic flag if `reg1 > reg2`. |

---

### 6.2 Arithmetic

#### ADD — Add `0x04`
| | |
|-|-|
| **Parameters** | `[dest: register]`, `[src: register or value]` |
| **Addressing modes** | `RegReg`, `RegVal` |
| **Description** | `dest = dest + src` |

#### SUB — Subtract `0x05`
| | |
|-|-|
| **Parameters** | `[dest: register]`, `[src: register or value]` |
| **Addressing modes** | `RegReg`, `RegVal` |
| **Description** | `dest = dest - src` |

#### INC — Increment `0x08`
| | |
|-|-|
| **Parameters** | `[reg: register]` |
| **Description** | `reg++` |

#### DEC — Decrement `0x09`
| | |
|-|-|
| **Parameters** | `[reg: register]` |
| **Description** | `reg--` |

#### MUL — Multiply `0x30`
| | |
|-|-|
| **Parameters** | `[dest: register]`, `[src: register or value]` |
| **Addressing modes** | `RegReg`, `RegVal` |
| **Description** | `dest = dest * src` |

#### DIV — Divide `0x31`
| | |
|-|-|
| **Parameters** | `[dest: register]`, `[src: register or value]` |
| **Addressing modes** | `RegReg`, `RegVal` |
| **Description** | `dest = dest / src` (integer division) |

---

### 6.3 Bitwise Operations

#### SHL — Shift Left `0x06`
| | |
|-|-|
| **Parameters** | `[src: register]`, `[positions: value]` |
| **Description** | Shifts `src` left by `positions` bits. Equivalent to multiplying by 2 per position. |

#### SHR — Shift Right `0x07`
| | |
|-|-|
| **Parameters** | `[src: register]`, `[positions: value]` |
| **Description** | Shifts `src` right by `positions` bits. Equivalent to integer division by 2 per position. |

#### ROL — Rotate Left `0x0E`
| | |
|-|-|
| **Parameters** | `[src: register]`, `[positions: value]` |
| **Description** | Rotates `src` left by `positions` bits. Bits shifted off the left are appended on the right. |

#### ROR — Rotate Right `0x0F`
| | |
|-|-|
| **Parameters** | `[src: register]`, `[positions: value]` |
| **Description** | Rotates `src` right by `positions` bits. Bits shifted off the right are appended on the left. |

#### AND — Bitwise AND `0x0A`
| | |
|-|-|
| **Parameters** | `[srcA: register]`, `[srcB: register or value]` |
| **Description** | `srcA = srcA & srcB` |

#### BOR — Bitwise OR `0x0B`
| | |
|-|-|
| **Parameters** | `[srcA: register]`, `[srcB: register or value]` |
| **Description** | `srcA = srcA \| srcB` |

#### XOR — Bitwise XOR `0x0C`
| | |
|-|-|
| **Parameters** | `[srcA: register]`, `[srcB: register or value]` |
| **Description** | `srcA = srcA ^ srcB` |

#### NOT — Bitwise NOT `0x0D`
| | |
|-|-|
| **Parameters** | `[src: register]` |
| **Description** | `src = ~src` |

---

### 6.4 Flow Control

#### JMP — Jump `0x10`
| | |
|-|-|
| **Parameters** | `[dest: register, address, or label]` |
| **Description** | Unconditionally sets the program counter to `dest`. Labels are resolved to addresses by the assembler. |

#### CLL — Call `0x11`
| | |
|-|-|
| **Parameters** | `[dest: register, address, or label]` |
| **Description** | Pushes the address of the next instruction onto the call stack, then jumps to `dest`. |

#### RET — Return `0x12`
| | |
|-|-|
| **Parameters** | *(none)* |
| **Description** | Pops the top of the call stack and resumes execution there. |

#### JMT — Jump if True `0x13`
| | |
|-|-|
| **Parameters** | `[dest: register, address, or label]` |
| **Description** | Jumps to `dest` if the previous test instruction set the logic flag to true. |

#### JMF — Jump if False `0x14`
| | |
|-|-|
| **Parameters** | `[dest: register, address, or label]` |
| **Description** | Jumps to `dest` if the previous test instruction set the logic flag to false. |

#### CLT — Call if True `0x17`
| | |
|-|-|
| **Parameters** | `[dest: register, address, or label]` |
| **Description** | Like `CLL`, but only executes if the logic flag is true. |

#### CLF — Call if False `0x18`
| | |
|-|-|
| **Parameters** | `[dest: register, address, or label]` |
| **Description** | Like `CLL`, but only executes if the logic flag is false. |

---

### 6.5 Stack Manipulation

#### PSH — Push `0x20`
| | |
|-|-|
| **Parameters** | `[data: register or value]` |
| **Description** | Pushes `data` onto the stack and decrements `SP`. |

#### POP — Pop `0x21`
| | |
|-|-|
| **Parameters** | `[dest: register]` |
| **Description** | Pops the top value off the stack into `dest` and increments `SP`. |

---

### 6.6 I/O

#### INB — Receive Byte `0x24`
| | |
|-|-|
| **Parameters** | `[port]`, `[dest: register]` |
| **Description** | Reads a byte from `port` into `dest`. |

#### INW — Receive Word `0x25`
| | |
|-|-|
| **Parameters** | `[port]`, `[dest: register]` |
| **Description** | Reads a 16-bit word from `port` into `dest`. |

#### IND — Receive Double Word `0x26`
| | |
|-|-|
| **Parameters** | `[port]`, `[dest: register]` |
| **Description** | Reads a 32-bit double word from `port` into `dest`. |

#### OUB — Send Byte `0x27`
| | |
|-|-|
| **Parameters** | `[port]`, `[src: register or value]` |
| **Description** | Writes a byte from `src` to `port`. |

#### OUW — Send Word `0x28`
| | |
|-|-|
| **Parameters** | `[port]`, `[src: register or value]` |
| **Description** | Writes a 16-bit word from `src` to `port`. |

#### OUD — Send Double Word `0x29`
| | |
|-|-|
| **Parameters** | `[port]`, `[src: register or value]` |
| **Description** | Writes a 32-bit double word from `src` to `port`. |

---

### 6.7 Interrupts

#### SWI — Software Interrupt `0x2A`
| | |
|-|-|
| **Parameters** | `[interrupt number]` |
| **Description** | Invokes the software interrupt handler for the given number. |

#### KEI — Kernel Interrupt `0x2B`
| | |
|-|-|
| **Parameters** | `[interrupt number]` |
| **Description** | Invokes the kernel interrupt handler for the given number. See [[Standard-Library]] for defined interrupt numbers. |

---

## 7. Quick Reference Table

| Mnemonic | Opcode  | Category            | Summary                        |
|----------|---------|---------------------|--------------------------------|
| `MOV`    | `0x01`  | Register/Memory     | Copy value into register       |
| `SWP`    | `0x02`  | Register/Memory     | Swap two registers             |
| `ADD`    | `0x04`  | Arithmetic          | dest = dest + src              |
| `SUB`    | `0x05`  | Arithmetic          | dest = dest − src              |
| `SHL`    | `0x06`  | Bitwise             | Shift left                     |
| `SHR`    | `0x07`  | Bitwise             | Shift right                    |
| `INC`    | `0x08`  | Arithmetic          | dest++                         |
| `DEC`    | `0x09`  | Arithmetic          | dest--                         |
| `AND`    | `0x0A`  | Bitwise             | Bitwise AND                    |
| `BOR`    | `0x0B`  | Bitwise             | Bitwise OR                     |
| `XOR`    | `0x0C`  | Bitwise             | Bitwise XOR                    |
| `NOT`    | `0x0D`  | Bitwise             | Bitwise NOT                    |
| `ROL`    | `0x0E`  | Bitwise             | Rotate left                    |
| `ROR`    | `0x0F`  | Bitwise             | Rotate right                   |
| `JMP`    | `0x10`  | Flow Control        | Unconditional jump             |
| `CLL`    | `0x11`  | Flow Control        | Call subroutine                |
| `RET`    | `0x12`  | Flow Control        | Return from subroutine         |
| `JMT`    | `0x13`  | Flow Control        | Jump if true                   |
| `JMF`    | `0x14`  | Flow Control        | Jump if false                  |
| `CLT`    | `0x17`  | Flow Control        | Call if true                   |
| `CLF`    | `0x18`  | Flow Control        | Call if false                  |
| `TEQ`    | `0x1A`  | Register/Memory     | Test equal                     |
| `TNE`    | `0x1B`  | Register/Memory     | Test not equal                 |
| `TLT`    | `0x1C`  | Register/Memory     | Test less than                 |
| `TMT`    | `0x1D`  | Register/Memory     | Test more than                 |
| `PSH`    | `0x20`  | Stack               | Push onto stack                |
| `POP`    | `0x21`  | Stack               | Pop from stack                 |
| `INB`    | `0x24`  | I/O                 | Receive byte from port         |
| `INW`    | `0x25`  | I/O                 | Receive word from port         |
| `IND`    | `0x26`  | I/O                 | Receive double word from port  |
| `OUB`    | `0x27`  | I/O                 | Send byte to port              |
| `OUW`    | `0x28`  | I/O                 | Send word to port              |
| `OUD`    | `0x29`  | I/O                 | Send double word to port       |
| `SWI`    | `0x2A`  | Interrupts          | Software interrupt             |
| `KEI`    | `0x2B`  | Interrupts          | Kernel interrupt               |
| `MUL`    | `0x30`  | Arithmetic          | dest = dest × src              |
| `DIV`    | `0x31`  | Arithmetic          | dest = dest ÷ src              |
| `MOM`    | `0x3A`  | Register/Memory     | Write register/value to memory |
| `MOE`    | `0x3B`  | Register/Memory     | Read memory into register      |

---

## 8. Executable Format

An AIL executable (`.ila`) is a binary file composed of a header followed by one or more sections.

### File Header

| Offset | Size    | Field           | Description                      |
|--------|---------|-----------------|----------------------------------|
| 0      | 4 bytes | Magic           | `0x41 0x49 0x4C 0x00` (`AIL\0`) |
| 4      | 2 bytes | Version         | Format version (little-endian)   |
| 6      | 2 bytes | Section count   | Number of sections               |

### Section Entry

| Offset | Size    | Field           | Description                      |
|--------|---------|-----------------|----------------------------------|
| 0      | 2 bytes | Section type    | Type identifier                  |
| 2      | 4 bytes | Section length  | Length of section data in bytes  |
| 6      | N bytes | Section data    | Raw section content              |
