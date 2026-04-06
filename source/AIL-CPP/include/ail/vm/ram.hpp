#pragma once
#include <cstdint>
#include <stdexcept>
#include <vector>

namespace ail {

/// Flat RAM with a read-only code guard.
/// Bytes below RAMLimit are the loaded executable and may not be written by
/// program instructions (only readable); bytes at or above RAMLimit are
/// data memory that programs may freely read and write.
class RAM {
public:
    explicit RAM(int size);

    /// Raw memory array; accessible to the VM for instruction fetch.
    std::vector<uint8_t> memory;

    /// One past the last byte of the loaded executable.
    /// SetByte() refuses writes below this address.
    int ramLimit = 0;

    /// Copy @p data into memory starting at offset 0 and set ramLimit.
    void load(const std::vector<uint8_t>& data);

    /// Write @p value to @p address (must be >= ramLimit).
    void setByte(int address, uint8_t value);

    /// Read the byte at @p address.
    uint8_t getByte(int address) const;

    /// Read @p length bytes starting at @p address.
    std::vector<uint8_t> getSection(int address, int length) const;

    /// Write @p data into memory starting at @p address.
    void setSection(int address, const std::vector<uint8_t>& data);
};

} // namespace ail
