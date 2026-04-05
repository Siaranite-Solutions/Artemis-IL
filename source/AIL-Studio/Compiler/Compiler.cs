using System;
using System.Collections.Generic;
using System.Text;

namespace AIL_Studio.Compiler
{
    /// <summary>
    /// Two-pass assembler for Artemis-IL.
    ///
    /// Syntax (per line):
    ///   ; or // comment
    ///   label:
    ///   MNEMONIC [param1 [, param2]]
    ///
    /// Values: decimal, 0x hex, or 'X' / '\n' char literals.
    /// Registers: PC IP SP SS A AL AH B BL BH C CL CH X Y (case-insensitive).
    /// Labels may be used as jump targets.
    /// </summary>
    public class Compiler
    {
        private readonly string _source;

        /// <summary>Raw bytecode produced after <see cref="Compile"/>.</summary>
        public byte[] ByteCode { get; private set; } = Array.Empty<byte>();

        public Compiler(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Assembles the source and returns the raw bytecode.
        /// Throws <see cref="BuildException"/> on error.
        /// </summary>
        public byte[] Compile()
        {
            if (string.IsNullOrWhiteSpace(_source))
                throw new BuildException("Source cannot be empty.");

            string[] lines = _source.Replace("\r", "").Split('\n');

            // Pass 1 — discover labels and compute byte offsets.
            // DB pseudo-instructions emit raw bytes (not 6-byte instructions), so we
            // track actual byte offsets rather than instruction-index × 6.
            var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int byteOffset = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string stripped = StripComment(lines[i]).Trim();
                if (stripped.Length == 0)
                    continue;
                if (stripped.EndsWith(':'))
                {
                    string name = stripped[..^1].Trim();
                    if (name.Length == 0)
                        throw new BuildException("Empty label name.", i + 1);
                    if (labels.ContainsKey(name))
                        throw new BuildException($"Duplicate label '{name}'.", i + 1);
                    labels[name] = byteOffset;
                }
                else
                {
                    string[] tokens = Tokenise(stripped);
                    if (tokens.Length == 0)
                        continue;
                    if (tokens[0].Equals("DB", StringComparison.OrdinalIgnoreCase))
                        byteOffset += CountDbBytes(tokens, i + 1);
                    else
                        byteOffset += 6;
                }
            }

            // Pass 2 — emit bytecode
            var bytes = new List<byte>(byteOffset);
            for (int i = 0; i < lines.Length; i++)
            {
                string stripped = StripComment(lines[i]).Trim();
                if (stripped.Length == 0 || stripped.EndsWith(':'))
                    continue;

                string[] tokens = Tokenise(stripped);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0].Equals("DB", StringComparison.OrdinalIgnoreCase))
                    bytes.AddRange(AssembleDb(tokens, i + 1));
                else
                    bytes.AddRange(AssembleLine(tokens, labels, i + 1));
            }

            ByteCode = bytes.ToArray();
            return ByteCode;
        }

        // ── Line assembly ─────────────────────────────────────────────────────

        private static byte[] AssembleLine(string[] tokens, Dictionary<string, int> labels, int lineNum)
        {
            string mnemonic = tokens[0].ToUpperInvariant();
            byte opcode = Instruction.GetOpcode(mnemonic);
            if (opcode == 0 && !Instruction.IsMnemonic(mnemonic))
                throw new BuildException($"Unknown mnemonic '{tokens[0]}'.", lineNum);

            // Classify each operand
            bool reg1 = false, reg2 = false;
            byte  p1b = 0;
            int   p1i = 0;
            int   p2i = 0;

            if (tokens.Length >= 2)
                (reg1, p1b, p1i) = ParseParam(tokens[1], labels, lineNum, isParam1: true);

            if (tokens.Length >= 3)
                (reg2, _, p2i) = ParseParam(tokens[2], labels, lineNum, isParam1: false);
            else if (!reg1)
                // Single non-register operand: propagate its full integer value into
                // param2 so that instructions like JMP/JMT/JMF that read param2 for
                // their target address work correctly.
                p2i = p1i;

            // Determine addressing mode
            AddressMode mode = (reg1, reg2) switch
            {
                (true,  true)  => AddressMode.RegisterRegister,
                (false, true)  => AddressMode.ValueRegister,
                (true,  false) => AddressMode.RegisterValue,
                _              => AddressMode.ValueValue,
            };

            return Instruction.Encode(opcode, mode, p1b, p2i);
        }

        /// <summary>
        /// Parses a single operand token.
        /// Returns (isRegister, byteValue, intValue).
        /// </summary>
        private static (bool isReg, byte byteVal, int intVal) ParseParam(
            string token, Dictionary<string, int> labels, int lineNum, bool isParam1)
        {
            // Register?
            if (Registers.IsRegister(token))
            {
                byte rb = Registers.GetByte(token);
                return (true, rb, rb);
            }

            // Char literal?
            if (CharValue.Check(token))
            {
                byte cv = CharValue.Parse(token);
                return (false, cv, cv);
            }

            // Hex / decimal literal?
            if (TryParseInteger(token, out int numVal))
            {
                byte bv = isParam1 ? (byte)(numVal & 0xFF) : (byte)0;
                return (false, bv, numVal);
            }

            // Label reference?
            if (labels.TryGetValue(token, out int addr))
                return (false, 0, addr);

            string paramNum = isParam1 ? "1" : "2";
            throw new BuildException($"Invalid value for parameter {paramNum}: '{token}'.", lineNum);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Removes everything from the first comment marker to end-of-line.</summary>
        private static string StripComment(string line)
        {
            bool inChar = false;
            bool inString = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inString)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '"') inString = false;
                }
                else if (inChar)
                {
                    // Inside a char literal: skip an escaped character then wait for closing '
                    if (c == '\\') { i++; continue; }  // skip next char
                    if (c == '\'') inChar = false;
                }
                else
                {
                    if (c == '"')  { inString = true; continue; }
                    if (c == '\'') { inChar   = true; continue; }
                    if (c == ';') return line[..i];
                    if (c == '/' && i + 1 < line.Length && line[i + 1] == '/') return line[..i];
                }
            }
            return line;
        }

        /// <summary>
        /// Splits a line into tokens, treating commas and whitespace outside of
        /// char or string literals as separators.  Char literals ('X', '\n', ' ')
        /// and string literals ("Hello") are kept as single atomic tokens.
        /// </summary>
        private static string[] Tokenise(string line)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];

                if (c == '\'')
                {
                    // Char literal — collect up to and including the closing '
                    sb.Append(c);
                    i++;
                    while (i < line.Length)
                    {
                        char ch = line[i];
                        sb.Append(ch);
                        i++;
                        if (ch == '\\' && i < line.Length)
                        {
                            // Escaped char: include the next char verbatim
                            sb.Append(line[i]);
                            i++;
                        }
                        else if (ch == '\'')
                        {
                            break; // closing quote
                        }
                    }
                }
                else if (c == '"')
                {
                    // String literal — collect up to and including the closing "
                    sb.Append(c);
                    i++;
                    while (i < line.Length)
                    {
                        char ch = line[i];
                        sb.Append(ch);
                        i++;
                        if (ch == '\\' && i < line.Length)
                        {
                            sb.Append(line[i]);
                            i++;
                        }
                        else if (ch == '"')
                        {
                            break; // closing quote
                        }
                    }
                }
                else if (c == ' ' || c == '\t' || c == ',')
                {
                    // Separator: flush current token if any
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                    i++;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            return tokens.ToArray();
        }

        private static bool TryParseInteger(string token, out int value)
        {
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(token[2..], System.Globalization.NumberStyles.HexNumber,
                    null, out value))
                    return true;
            }
            else if (int.TryParse(token, out value))
            {
                return true;
            }
            value = 0;
            return false;
        }

        // ── DB pseudo-instruction ─────────────────────────────────────────────

        /// <summary>
        /// Counts the raw bytes that a DB directive will emit, for pass-1 byte-offset tracking.
        /// </summary>
        private static int CountDbBytes(string[] tokens, int lineNum)
        {
            int count = 0;
            for (int i = 1; i < tokens.Length; i++)
                count += DbTokenByteCount(tokens[i], lineNum);
            return count;
        }

        /// <summary>
        /// Emits the raw bytes for a DB directive.
        /// Tokens after "DB" may be:
        ///   "Hello"  — string literal (emits each char)
        ///   'H'      — char literal
        ///   0x48     — hex byte
        ///   72       — decimal byte
        /// </summary>
        private static IEnumerable<byte> AssembleDb(string[] tokens, int lineNum)
        {
            for (int i = 1; i < tokens.Length; i++)
            {
                string tok = tokens[i];
                if (tok.StartsWith('"') && tok.EndsWith('"') && tok.Length >= 2)
                {
                    // String literal — emit each character (with escape handling)
                    string content = tok[1..^1];
                    for (int j = 0; j < content.Length; j++)
                    {
                        if (content[j] == '\\' && j + 1 < content.Length)
                        {
                            yield return EscapeToChar(content[j + 1]);
                            j++;
                        }
                        else
                        {
                            yield return (byte)content[j];
                        }
                    }
                }
                else if (CharValue.Check(tok))
                {
                    yield return CharValue.Parse(tok);
                }
                else if (TryParseInteger(tok, out int numVal))
                {
                    yield return (byte)(numVal & 0xFF);
                }
                else
                {
                    throw new BuildException($"DB: invalid byte value '{tok}'.", lineNum);
                }
            }
        }

        private static int DbTokenByteCount(string tok, int lineNum)
        {
            if (tok.StartsWith('"') && tok.EndsWith('"') && tok.Length >= 2)
            {
                // Count characters in the string literal (escape sequences count as 1)
                string content = tok[1..^1];
                int count = 0;
                for (int j = 0; j < content.Length; j++)
                {
                    if (content[j] == '\\' && j + 1 < content.Length)
                        j++;
                    count++;
                }
                return count;
            }
            if (CharValue.Check(tok) || TryParseInteger(tok, out _))
                return 1;
            throw new BuildException($"DB: invalid byte value '{tok}'.", lineNum);
        }

        private static byte EscapeToChar(char escape) => escape switch
        {
            'n'  => (byte)'\n',
            'r'  => (byte)'\r',
            't'  => (byte)'\t',
            'a'  => (byte)'\a',
            'b'  => (byte)'\b',
            'v'  => (byte)'\v',
            '0'  => 0,
            '\'' => (byte)'\'',
            '"'  => (byte)'"',
            '\\' => (byte)'\\',
            _    => (byte)escape,
        };
    }
}
