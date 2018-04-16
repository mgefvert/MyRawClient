using System;

namespace MyRawClient.Enumerations
{
    [Flags]
    public enum CapabilityFlags : uint
    {
        // ReSharper disable InconsistentNaming
        ClientLongPassword = 1,
        ClientFoundRows = 2,
        ClientLongFlag = 4,
        ClientConnectWithDB = 8,
        ClientNoSchema = 0x10,
        ClientCompress = 0x20,
        ClientODBC = 0x40,
        ClientLocalFiles = 0x80,
        ClientIgnoreSpace = 0x100,
        ClientProtocol41 = 0x200,
        ClientInteractive = 0x400,
        ClientSSL = 0x800,
        ClientIgnoreSIGPIPE = 0x1000,
        ClientTransactions = 0x2000,
        ClientSecureConnection = 0x8000,
        ClientMultiStatements = 0x1_0000,
        ClientMultiResults = 0x2_0000,
        ClientPSMultiResults = 0x4_000,
        ClientPluginAuth = 0x8_0000,
        ClientConnectAttrs = 0x10_0000,
        ClientPluginAuthLenEncCLientData = 0x20_0000,
        ClientCanHandleExpiredPasswords = 0x40_0000,
        ClientSessionTrack = 0x80_0000,
        ClientDeprecateEOF = 0x100_0000
        // ReSharper restore InconsistentNaming
    }
}
