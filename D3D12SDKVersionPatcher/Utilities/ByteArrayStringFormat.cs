namespace D3D12SDKVersionPatcher.Utilities;

public enum ByteArrayStringFormat
{
    // no byte[] <-> string conversion
    None,

    // "0x"-prefixed hex pairs, DirectN's default (tolerant parsing: optional prefix, non-hex chars skipped)
    Hex,

    Base64
}
