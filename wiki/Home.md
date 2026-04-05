# Apollo Intermediate Language

Welcome to the AIL wiki. This wiki is the normative reference for the Apollo Intermediate Language virtual machine, its instruction set, standard library, and executable format.

## Pages

| Page | Description |
|------|-------------|
| [[Specification]] | Architecture & instruction set specification (v2.0) |
| [[Standard-Library]] | Kernel interrupt reference |

## What is AIL?

The Apollo Intermediate Language (AIL) is a low-level, register-based intermediate language designed for deterministic, portable execution across virtual machine implementations. It provides a stable compilation target independent of the underlying host architecture.

**Design goals:**

- **Platform independence** — the same AIL bytecode runs identically on any compliant VM
- **Simplicity** — a small, orthogonal instruction set that is straightforward to implement
- **Portability** — suitable for implementation in managed runtimes (.NET/CLR), native code (C/C++), and constrained hardware (e.g. Z80/CP/M)
- **Determinism** — no undefined behaviour; all edge cases are specified

## Repository layout

```
/source   — VM and runtime source code (.NET)
/docs     — original .docx reference documents
/wiki     — this wiki (mirror kept in the repo)
```
