#pragma once
#include <cstdint>
#include <vector>
#include <array>
#include "ail/vm/ram.hpp"
#include "ail/vm/call_stack.hpp"
#include "ail/address_mode.hpp"

namespace ail {

/// AIL virtual machine.
///
/// Execution model (spec §5):
///   - IP  = address of the currently-executing instruction
///   - PC  = address of the *next* instruction (IP + 6 after each step)
///   - Instructions are 6 bytes: byte0=(opcode<<2)|addrmode, byte1=param1, bytes2-5=param2
///
/// The stack is a separate 256-byte array; SP starts at 0xFF and grows downward.
class VM {
public:
    /// Construct a VM loaded with @p code and a RAM of @p ramSize bytes.
    VM(const std::vector<uint8_t>& code, int ramSize = 1024 * 1024);

    /// Run until a halt condition (opcode 0x00 or KEI 0x02).
    void execute();

    /// Execute exactly one instruction.
    void tick();

    /// Jump to @p addr and execute from there.
    void executeAt(uint8_t addr);

    /// Request the VM to stop after the current instruction completes.
    void halt();

    bool running = false;

    // ── Registers (spec §3) ──────────────────────────────────────────────────
    uint8_t  PC = 1;   ///< Program Counter — next instruction
    uint8_t  IP = 0;   ///< Instruction Pointer — current instruction
    uint8_t  SP = 0xFF;///< Stack Pointer (read-only to programs)
    uint8_t  SS = 0;   ///< Stack Segment
    uint8_t  AL = 0;
    uint8_t  AH = 0;
    uint8_t  BL = 0;
    uint8_t  BH = 0;
    uint8_t  CL = 0;
    uint8_t  CH = 0;
    int32_t  X  = 0;   ///< 32-bit general purpose
    int32_t  Y  = 0;   ///< 32-bit general purpose

    RAM ram;

    /// 256-byte hardware stack; indexed by SP.
    std::array<uint8_t, 256> stackMemory{};

    /// Result of the most recent test instruction (TEQ/TNE/TLT/TMT).
    bool lastLogic = false;

    // ── Helpers (used by VM and interrupt handlers) ──────────────────────────

    /// Write @p value to the register identified by @p reg.
    void setRegister(uint8_t reg, int32_t value);

    /// Read the current value of the register identified by @p reg.
    int32_t getRegister(uint8_t reg) const;

    /// Set both halves of a 16-bit split register (A, B, or C).
    void setSplit(char which, int32_t value);

    /// Get the combined 16-bit value of a split register.
    int32_t getSplit(char which) const;

    // ── Instruction dispatch ─────────────────────────────────────────────────

    /// Decode and execute the opcode.  Called from execute()/tick().
    void parseOpcode(uint8_t opcode);

private:
    AddressMode m_addrMode = AddressMode::RegReg;
    int         m_adMode   = 0;
    CallStack   m_callStack;

    void getAddressMode(uint8_t b);

    /// Read a little-endian 32-bit integer from RAM at @p offset.
    int32_t get32(int offset) const;
};

} // namespace ail
