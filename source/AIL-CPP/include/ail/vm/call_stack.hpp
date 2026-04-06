#pragma once
#include <array>
#include <stdexcept>

namespace ail {

/// Subroutine call stack.
/// Stores return addresses pushed by CLL/CLT/CLF; popped by RET.
class CallStack {
public:
    CallStack() = default;

    /// Push @p returnAddress onto the call stack.
    void call(int returnAddress);

    /// Pop and return the top return address.
    /// Throws std::underflow_error if the stack is empty.
    int ret();

private:
    static constexpr int kCapacity = 255;
    std::array<int, kCapacity> m_locations{};
    int m_index = 0;
};

} // namespace ail
