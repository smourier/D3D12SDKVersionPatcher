using System;
using System.Collections.Generic;
using System.Globalization;

namespace D3D12SDKVersionPatcher.Utilities;

public class CommandLine
{
    public static CommandLine Current { get; } = From(Environment.GetCommandLineArgs());
    public static CommandLine From(IReadOnlyList<string> args)
    {
        var cmdLine = new CommandLine
        {
            CurrentDirectory = Environment.CurrentDirectory
        };
        if (args != null)
        {
            for (var i = 0; i < args.Count; i++)
            {
                if (i == 0)
                    continue;

                var arg = args[i].Nullify();
                if (arg == null)
                    continue;

                if (string.Equals(arg, "/?", StringComparison.Ordinal) ||
                    string.Equals(arg, "-?", StringComparison.Ordinal) ||
                    string.Equals(arg, "/HELP", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-HELP", StringComparison.OrdinalIgnoreCase))
                {
                    cmdLine.HelpRequested = true;
                }

                var named = false;
                if (arg[0] == '-' || arg[0] == '/')
                {
                    arg = arg[1..];
                    named = true;
                }

                string? name;
                string? value;
                int pos = arg.IndexOf(':');
                if (pos < 0)
                {
                    name = arg.Nullify();
                    value = null;
                }
                else
                {
                    name = arg[..pos].Trim().Nullify();
                    value = arg[(pos + 1)..].Trim().Nullify();
                }

                if (named)
                {
                    if (name != null)
                    {
                        cmdLine._namedArguments[name] = value;
                    }
                }
                else
                {
                    cmdLine._positionArguments[i - 1] = arg.Nullify()!;
                }
            }
        }
        return cmdLine;
    }

    private readonly Dictionary<string, string?> _namedArguments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _positionArguments = [];

    private CommandLine()
    {
    }

    public IReadOnlyDictionary<string, string?> NamedArguments => _namedArguments;
    public IReadOnlyDictionary<int, string> PositionArguments => _positionArguments;
    public bool HelpRequested { get; private set; }
    public string CurrentDirectory { get; internal set; } = null!;

    public static string CommandLineWithoutExe
    {
        get
        {
            var line = Environment.CommandLine;
            var inParens = false;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ' && !inParens)
                    return line[(i + 1)..].TrimStart();

                if (line[i] == '"')
                {
                    inParens = !inParens;
                }
            }
            return line;
        }
    }

    public static T? GetArgument<T>(IEnumerable<string> arguments, string name, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (arguments == null)
            return defaultValue;

        foreach (var arg in arguments)
        {
            if (arg.StartsWith('-') || arg.StartsWith('/'))
            {
                var pos = arg.IndexOfAny(['=', ':'], 1);
                var argName = pos < 0 ? arg[1..] : arg[1..pos];
                if (string.Compare(name, argName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var value = pos < 0 ? string.Empty : arg[(pos + 1)..].Trim();
                    if (value.Length == 0)
                    {
                        if (typeof(T) == typeof(bool)) // special case for bool args: if it's there, return true
                            return (T)(object)true;

                        return defaultValue;
                    }
                    return ChangeType(value, defaultValue);
                }
            }
        }
        return defaultValue;
    }

    public string? GetNullifiedArgument(string name, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!_namedArguments.TryGetValue(name, out var s))
            return defaultValue.Nullify();

        return s.Nullify();
    }

    public string? GetNullifiedArgument(int index, string? defaultValue = null)
    {
        if (!_positionArguments.TryGetValue(index, out var s))
            return defaultValue.Nullify();

        return s.Nullify();
    }

    public T? GetArgument<T>(int index, T? defaultValue = default, IFormatProvider? provider = null)
    {
        if (!_positionArguments.TryGetValue(index, out var s))
            return defaultValue;

        return ChangeType(s, defaultValue, provider);
    }

    public T? GetArgument<T>(string name, T? defaultValue = default, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!_namedArguments.TryGetValue(name, out var s))
            return defaultValue;

        if (typeof(T) == typeof(bool) && string.IsNullOrEmpty(s))
            return (T)(object)true;

        return ChangeType(s, defaultValue, provider);
    }

    public bool HasArgument(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _namedArguments.TryGetValue(name, out _);
    }

    public static T ParseInvariant<T>(string text) where T : IParsable<T> => T.Parse(text, CultureInfo.InvariantCulture);
    public static bool TryParse<T>(ReadOnlySpan<char> text, out T? result, IFormatProvider? provider = null) where T : ISpanParsable<T>
        => T.TryParse(text, provider ?? CultureInfo.InvariantCulture, out result);

    public static T? ChangeType<T>(object? value, T? defaultValue = default, IFormatProvider? provider = null)
    {
        if (value is null || value is DBNull)
            return defaultValue;

        if (value is T typed)
            return typed;

        provider ??= CultureInfo.CurrentCulture;

        // unwrap Nullable<T> so the conversion targets the underlying type
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
            if (target.IsEnum)
            {
                if (value is string enumText)
                    return (T)Enum.Parse(target, enumText, ignoreCase: true);

                return (T)Enum.ToObject(target, value);
            }

            // Convert.ChangeType is AOT-safe: it dispatches through IConvertible, no reflection
            if (value is IConvertible)
                return (T)Convert.ChangeType(value, target, provider);
        }
        catch
        {
            // continue
        }

        return defaultValue;
    }
}