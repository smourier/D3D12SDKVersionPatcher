using System;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.Code;
using AsmResolver.PE.Exports;
using AsmResolver.PE.Exports.Builder;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using AsmResolver.PE.Relocations;
using AsmResolver.PE.Relocations.Builder;

namespace D3D12SDKVersionPatcher
{
    public static class D3D12SdkVersion
    {
        public static void PatchExe(string inputFilePath, int sdkVersion, string sdkPath, string outputFilePath)
        {
            ArgumentNullException.ThrowIfNull(inputFilePath);
            ArgumentNullException.ThrowIfNull(sdkPath);
            ArgumentNullException.ThrowIfNull(outputFilePath);

            var outputFile = PEFile.FromFile(inputFilePath);
            var outputImage = PEImage.FromFile(outputFile);

            // right now we don't support files that already have and export section
            if (outputImage.Exports != null)
                throw new Exception($"File {inputFilePath} already has an export section.");

            var is32bit = outputFile.FileHeader.Characteristics.HasFlag(Characteristics.Machine32Bit);

            using var ms = new MemoryStream();
            var binary = new BinaryStreamWriter(ms);

            // virtual address of D3D12SDKPath value
            binary.WriteInt64(0); // 0 for now, real virtual address written later with Patch call. align on 8 bytes anyway

            // D3D12SDKVersion value
            var offsetOfSdkVersion = (uint)binary.Offset;
            binary.WriteInt64(sdkVersion); // align on 8 bytes anyway

            // D3D12SDKPath value
            var offsetOfSdkPath = (uint)binary.Offset;
            binary.WriteAsciiString(sdkPath + '\0');

            // build a section for holding export data
            var data = ms.ToArray();
            var exportDataBuffer = new DataSegment(data).AsPatchedSegment().Patch(0, is32bit ? AddressFixupType.Absolute32BitAddress : AddressFixupType.Absolute64BitAddress, offsetOfSdkPath);

            // add export directory to file
            var exportDirectory = new ExportDirectory(Path.GetFileName(outputFilePath));
            exportDirectory.Entries.Add(new ExportedSymbol(exportDataBuffer.ToReference(0), "D3D12SDKPath"));
            exportDirectory.Entries.Add(new ExportedSymbol(exportDataBuffer.ToReference((int)offsetOfSdkVersion), "D3D12SDKVersion"));

            var exportBuffer = new ExportDirectoryBuffer();
            exportBuffer.AddDirectory(exportDirectory);

            // add sections: export & export data
            var exportSection = new PESection(".edata", SectionFlags.MemoryRead | SectionFlags.ContentInitializedData, exportBuffer);
            var exportDataSection = new PESection(".export", SectionFlags.MemoryRead | SectionFlags.ContentInitializedData, exportDataBuffer);

            outputFile.Sections.Add(exportSection);
            outputFile.Sections.Add(exportDataSection);
            outputFile.OptionalHeader.SetDataDirectory(DataDirectoryIndex.ExportDirectory, new DataDirectory(exportSection.Rva, exportSection.GetPhysicalSize()));
            outputFile.UpdateHeaders();

            // add virtual address of D3D12SDKPath value to relocations
            // 1. copy exports from original file
            var relocBuffer = new RelocationsDirectoryBuffer();
            foreach (var relocation in outputImage.Relocations)
            {
                relocBuffer.Add(relocation);
            }
            // 2. add virtual address to D3D12SDKPath value as a relocation
            relocBuffer.Add(new BaseRelocation(RelocationType.Dir64, exportDataBuffer.ToReference(0)));

            // 3. update .reloc section's content
            var reloc = outputFile.Sections.FirstOrDefault(s => s.Name == ".reloc") ?? throw new Exception($"File {inputFilePath} has no .reloc section.");
            reloc.Contents = relocBuffer;
            outputFile.UpdateHeaders();

            // commit
            outputFile.Write(outputFilePath);
        }
    }
}
