#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PricingService.Domain
{
    /// <summary>
    /// Bo tinh bieu thuc nho dung cho cac luat tinh gia (tariff rules).
    /// Ho tro: + - * /, so sanh, && || !, toan tu ?:, ngoac, so thap phan (hau to M/m), chuoi "..." va ten bien.
    /// </summary>
    public static class ExpressionEvaluator
    {
        public static object Evaluate(string expression, IDictionary<string, object> variables)
        {
            var parser = new Parser(expression, variables);
            return parser.ParseAll();
        }

        public static decimal EvaluateDecimal(string expression, IDictionary<string, object> variables)
        {
            var value = Evaluate(expression, variables);
            if (value is decimal)
                return (decimal)value;
            throw new InvalidOperationException("Expression '" + expression + "' does not return a number.");
        }

        public static bool EvaluateBool(string expression, IDictionary<string, object> variables)
        {
            var value = Evaluate(expression, variables);
            if (value is bool)
                return (bool)value;
            throw new InvalidOperationException("Expression '" + expression + "' does not return true/false.");
        }

        private sealed class Parser
        {
            private readonly string text;
            private readonly IDictionary<string, object> variables;
            private int pos;

            public Parser(string text, IDictionary<string, object> variables)
            {
                this.text = text ?? string.Empty;
                this.variables = variables ?? new Dictionary<string, object>();
                this.pos = 0;
            }

            public object ParseAll()
            {
                var value = ParseTernary();
                SkipSpaces();
                if (pos < text.Length)
                    throw Error("unexpected character '" + text[pos] + "'");
                return value;
            }

            private object ParseTernary()
            {
                var condition = ParseOr();
                SkipSpaces();
                if (Peek('?'))
                {
                    pos++;
                    var whenTrue = ParseTernary();
                    Expect(':');
                    var whenFalse = ParseTernary();
                    return AsBool(condition) ? whenTrue : whenFalse;
                }

                return condition;
            }

            private object ParseOr()
            {
                var left = ParseAnd();
                while (true)
                {
                    SkipSpaces();
                    if (!Match("||"))
                        break;
                    var right = ParseAnd();
                    left = AsBool(left) || AsBool(right);
                }

                return left;
            }

            private object ParseAnd()
            {
                var left = ParseComparison();
                while (true)
                {
                    SkipSpaces();
                    if (!Match("&&"))
                        break;
                    var right = ParseComparison();
                    left = AsBool(left) && AsBool(right);
                }

                return left;
            }

            private object ParseComparison()
            {
                var left = ParseAdditive();
                SkipSpaces();
                if (Match("=="))
                    return object.Equals(left, ParseAdditive());
                if (Match("!="))
                    return !object.Equals(left, ParseAdditive());
                if (Match(">="))
                    return Compare(left, ParseAdditive()) >= 0;
                if (Match("<="))
                    return Compare(left, ParseAdditive()) <= 0;
                if (Match(">"))
                    return Compare(left, ParseAdditive()) > 0;
                if (Match("<"))
                    return Compare(left, ParseAdditive()) < 0;
                return left;
            }

            private object ParseAdditive()
            {
                var left = ParseMultiplicative();
                while (true)
                {
                    SkipSpaces();
                    if (Peek('+'))
                    {
                        pos++;
                        left = AsDecimal(left) + AsDecimal(ParseMultiplicative());
                    }
                    else if (Peek('-'))
                    {
                        pos++;
                        left = AsDecimal(left) - AsDecimal(ParseMultiplicative());
                    }
                    else
                    {
                        break;
                    }
                }

                return left;
            }

            private object ParseMultiplicative()
            {
                var left = ParseUnary();
                while (true)
                {
                    SkipSpaces();
                    if (Peek('*'))
                    {
                        pos++;
                        left = AsDecimal(left) * AsDecimal(ParseUnary());
                    }
                    else if (Peek('/'))
                    {
                        pos++;
                        left = AsDecimal(left) / AsDecimal(ParseUnary());
                    }
                    else
                    {
                        break;
                    }
                }

                return left;
            }

            private object ParseUnary()
            {
                SkipSpaces();
                if (Peek('-'))
                {
                    pos++;
                    return -AsDecimal(ParseUnary());
                }

                if (Peek('!'))
                {
                    pos++;
                    return !AsBool(ParseUnary());
                }

                return ParsePrimary();
            }

            private object ParsePrimary()
            {
                SkipSpaces();
                if (pos >= text.Length)
                    throw Error("unexpected end of expression");

                var c = text[pos];

                if (c == '(')
                {
                    pos++;
                    var inner = ParseTernary();
                    Expect(')');
                    return inner;
                }

                if (c == '"')
                    return ParseString();

                if (char.IsDigit(c) || c == '.')
                    return ParseNumber();

                if (char.IsLetter(c) || c == '_')
                    return ParseIdentifier();

                throw Error("unexpected character '" + c + "'");
            }

            private object ParseString()
            {
                pos++; // dau " mo dau
                var start = pos;
                while (pos < text.Length && text[pos] != '"')
                    pos++;
                if (pos >= text.Length)
                    throw Error("string literal is not closed");
                var value = text.Substring(start, pos - start);
                pos++; // dau " dong
                return value;
            }

            private object ParseNumber()
            {
                var start = pos;
                while (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '.'))
                    pos++;
                var raw = text.Substring(start, pos - start);
                decimal value;
                if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
                    throw Error("invalid number '" + raw + "'");
                if (pos < text.Length && (text[pos] == 'M' || text[pos] == 'm'))
                    pos++;
                return value;
            }

            private object ParseIdentifier()
            {
                var start = pos;
                while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_'))
                    pos++;
                var name = text.Substring(start, pos - start);

                if (name == "true")
                    return true;
                if (name == "false")
                    return false;

                object raw;
                if (!variables.TryGetValue(name, out raw))
                    throw Error("unknown variable '" + name + "'");
                return Normalize(raw);
            }

            private static object Normalize(object raw)
            {
                if (raw == null)
                    return string.Empty;
                if (raw is decimal || raw is string || raw is bool)
                    return raw;
                if (raw is int || raw is long || raw is short || raw is byte || raw is double || raw is float)
                    return Convert.ToDecimal(raw, CultureInfo.InvariantCulture);
                return raw.ToString();
            }

            private static int Compare(object left, object right)
            {
                if (left is decimal && right is decimal)
                    return decimal.Compare((decimal)left, (decimal)right);
                if (left is string && right is string)
                    return string.CompareOrdinal((string)left, (string)right);
                throw new InvalidOperationException("Cannot compare values of different types.");
            }

            private static decimal AsDecimal(object value)
            {
                if (value is decimal)
                    return (decimal)value;
                throw new InvalidOperationException("A number was expected in the expression.");
            }

            private static bool AsBool(object value)
            {
                if (value is bool)
                    return (bool)value;
                throw new InvalidOperationException("A true/false value was expected in the expression.");
            }

            private void SkipSpaces()
            {
                while (pos < text.Length && char.IsWhiteSpace(text[pos]))
                    pos++;
            }

            private bool Peek(char c)
            {
                return pos < text.Length && text[pos] == c;
            }

            private bool Match(string token)
            {
                if (string.CompareOrdinal(text, pos, token, 0, token.Length) == 0)
                {
                    pos += token.Length;
                    return true;
                }

                return false;
            }

            private void Expect(char c)
            {
                SkipSpaces();
                if (!Peek(c))
                    throw Error("'" + c + "' was expected");
                pos++;
            }

            private Exception Error(string message)
            {
                return new InvalidOperationException("Invalid expression '" + text + "': " + message);
            }
        }
    }
}