namespace Echo.NativeGame.GameFlow.Results
{
    // NativeNarrative owns these facts; terminal capture only reads their key and text.
    public readonly struct NativeBehaviorRecord
    {
        public string Key { get; }
        public string Text { get; }

        public NativeBehaviorRecord(string key, string text)
        {
            Key = key ?? string.Empty;
            Text = text ?? string.Empty;
        }
    }
}
