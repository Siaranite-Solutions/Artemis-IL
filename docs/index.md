# Artemis-IL Documentation

This site mirrors the [GitHub Wiki](../../wiki) for the Artemis Intermediate Language project.

## Contents

| Page | Description |
|------|-------------|
| [Home / Getting Started](../../wiki/Home) | Project overview, build instructions, and quick-start guide |
| [Specification](../../wiki/Specification) | AIL v2.0 architecture: memory model, instruction encoding, registers, and full instruction reference |
| [Standard Library](../../wiki/Standard-Library) | Kernel (`KEI`) and software (`SWI`) interrupt reference with examples |

## Source code

Source code is kept in the `/source/` folder. The `/wiki/` folder in this repository is a mirror of the wiki pages.

| Project | Description |
|---------|-------------|
| `source/Artemis-VM` | Core VM library — netstandard2.0, usable from any .NET host |
| `source/AIL-Runtime` | Command-line runtime — load and execute `.ila` files or raw bytecode |
| `source/AIL-Studio` | WinForms IDE — assembler, decompiler, and step-through debugger (Windows only) |
| `source/AIL-Tests` | xUnit test suite covering the VM, assembler, and decompiler |
