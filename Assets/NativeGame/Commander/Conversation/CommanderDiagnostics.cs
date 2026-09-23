using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    // Process-only opt-in. Never enable SDK debug logging (it includes headers).
    public sealed class CommanderDiagnostics
    {
        static readonly ConditionalWeakTable<object, CommanderDiagnostics> traces =
            new ConditionalWeakTable<object, CommanderDiagnostics>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool Enabled { get; set; }
#else
        public static bool Enabled { get => false; set { } }
#endif
        readonly string id = Guid.NewGuid().ToString("N");
        readonly Guid session;
        readonly long generation;
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly string key;
        int sequence;

        public CommanderDiagnostics(Guid session, long generation)
        {
            this.session = session;
            this.generation = generation;
            CommanderSettings.Instance.TryGetApiKey(out key);
        }

        public static void Bind(object messages, CommanderDiagnostics trace) => traces.Add(messages, trace);
        public static CommanderDiagnostics For(object messages) =>
            messages != null && traces.TryGetValue(messages, out var trace) ? trace :
                new CommanderDiagnostics(Guid.Empty, 0);

        public static string Endpoint(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return "<invalid>";
            return uri.Scheme + "://" + uri.Host + ":" + uri.Port + "/<proxy-path-redacted>/chat/completions";
        }

        public void Log(string stage, string text)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Enabled) return;
            // Redact before chunking, including keys echoed by a provider or the model.
            text = text ?? "<null>";
            if (!string.IsNullOrEmpty(key)) text = text.Replace(key, "[REDACTED]");
            if (CommanderSettings.Instance.TryGetApiKey(out var current) && !string.IsNullOrEmpty(current))
                text = text.Replace(current, "[REDACTED]");
            text = Regex.Replace(text, @"(?i)(authorization|cookie|set-cookie|api[_-]?key|access[_-]?token|refresh[_-]?token|password|secret)\s*[""']?\s*[:=]\s*[^\r\n,}]+", "$1=[REDACTED]");
            text = Regex.Replace(text, @"(?i)Bearer\s+[^\s""',}]+|\bsk-[A-Za-z0-9_-]+", "[REDACTED]");
            const int size = 2400;
            int count = Math.Max(1, (text.Length + size - 1) / size);
            int number = ++sequence;
            for (int part = 0; part < count; part++)
                UnityEngine.Debug.Log($"[GF02-LLM rid={id} session={session:N} gen={generation} seq={number} ms={clock.ElapsedMilliseconds} stage={stage} part={part + 1}/{count}]\n" +
                    text.Substring(part * size, Math.Min(size, text.Length - part * size)));
#endif
        }

#if UNITY_EDITOR
        const string Menu = "Tools/Echo/GF02/LLM Diagnostics (full text)";
        [UnityEditor.MenuItem(Menu)]
        static void Toggle()
        {
            Enabled = !Enabled;
            UnityEditor.Menu.SetChecked(Menu, Enabled);
            UnityEngine.Debug.Log("[GF02-LLM] Full-text diagnostics " + (Enabled ? "ON" : "OFF"));
        }
        [UnityEditor.MenuItem(Menu, true)]
        static bool ValidateMenu() { UnityEditor.Menu.SetChecked(Menu, Enabled); return true; }
#endif
    }
}
