#pragma once
#include <cstdint>
#include <vector>
#include <stdexcept>

namespace ail {

/// Parses and executes an AIL executable (.ila or raw bytecode).
///
/// .ila file layout (spec §8):
///   Offset 0 : 4 bytes  Magic  (0x41 0x49 0x4C 0x00 = "AIL\0")
///   Offset 4 : 2 bytes  Format version (little-endian)
///   Offset 6 : 2 bytes  Section count  (little-endian)
///   Each section:
///     Offset 0 : 2 bytes  Section type   (little-endian)
///     Offset 2 : 4 bytes  Section length (little-endian)
///     Offset 6 : N bytes  Section data
class Executable {
public:
    /// Load @p data and run it in a new VM.
    static void run(const std::vector<uint8_t>& data);

    /// Extract the code payload from @p data.
    /// Returns raw bytecode for files without the AIL magic header.
    /// Throws std::runtime_error if the .ila structure is invalid.
    static std::vector<uint8_t> extractCode(const std::vector<uint8_t>& data);

private:
    static constexpr int kDefaultRamSize = 1024 * 1024; // 1 MiB

    static bool hasIlaMagic(const std::vector<uint8_t>& data);
};

} // namespace ail
