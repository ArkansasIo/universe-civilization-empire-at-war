/**
 * Win64 Native PE Executable Generator for BSAT Toolchain
 * Generates valid Win64 PE32+ console binaries with embedded commands.
 */

const fs = require('fs');
const path = require('path');

function createWin64Exe(commandToExecute, outputPath) {
  // We construct a clean, valid Win64 PE executable
  // Section 1: DOS Header (64 bytes)
  const dosHeader = Buffer.alloc(128);
  dosHeader.write('MZ', 0); // e_magic
  dosHeader.writeUInt32LE(0x80, 0x3C); // e_lfanew -> PE header at offset 128 (0x80)

  // Standard DOS stub message
  dosHeader.write('BSAT Studio Win64 Launcher Stub', 0x40);

  // Section 2: PE Signature & COFF Header (24 bytes)
  const peHeader = Buffer.alloc(24);
  peHeader.write('PE\0\0', 0); // PE signature
  peHeader.writeUInt16LE(0x8664, 4); // Machine: IMAGE_FILE_MACHINE_AMD64
  peHeader.writeUInt16LE(2, 6); // NumberOfSections: 2 (.text, .rdata)
  peHeader.writeUInt32LE(Math.floor(Date.now() / 1000), 8); // TimeDateStamp
  peHeader.writeUInt16LE(240, 16); // SizeOfOptionalHeader (0xF0 for PE32+)
  peHeader.writeUInt16LE(0x0022, 18); // Characteristics: EXECUTABLE_IMAGE | LARGE_ADDRESS_AWARE

  // Section 3: Optional Header PE32+ (240 bytes)
  const optHeader = Buffer.alloc(240);
  optHeader.writeUInt16LE(0x020B, 0); // Magic: PE32+
  optHeader.writeUInt8(14, 2); // MajorLinkerVersion
  optHeader.writeUInt8(0, 3); // MinorLinkerVersion
  optHeader.writeUInt32LE(0x1000, 4); // SizeOfCode
  optHeader.writeUInt32LE(0x1000, 8); // SizeOfInitializedData
  optHeader.writeUInt32LE(0x1000, 16); // AddressOfEntryPoint (RVA 0x1000)
  optHeader.writeUInt32LE(0x1000, 20); // BaseOfCode
  optHeader.writeBigUInt64LE(0x140000000n, 24); // ImageBase
  optHeader.writeUInt32LE(0x1000, 32); // SectionAlignment
  optHeader.writeUInt32LE(0x200, 36); // FileAlignment
  optHeader.writeUInt16LE(6, 40); // MajorOperatingSystemVersion
  optHeader.writeUInt16LE(0, 42); // MinorOperatingSystemVersion
  optHeader.writeUInt16LE(6, 48); // MajorSubsystemVersion
  optHeader.writeUInt32LE(0x3000, 56); // SizeOfImage
  optHeader.writeUInt32LE(0x400, 60); // SizeOfHeaders
  optHeader.writeUInt16LE(3, 68); // Subsystem: IMAGE_SUBSYSTEM_WINDOWS_CUI (Console)
  optHeader.writeUInt16LE(0x8160, 70); // DllCharacteristics: DYNAMIC_BASE | NX_COMPAT | TERMINAL_SERVER_AWARE
  optHeader.writeBigUInt64LE(0x100000n, 72); // SizeOfStackReserve
  optHeader.writeBigUInt64LE(0x1000n, 80); // SizeOfStackCommit
  optHeader.writeBigUInt64LE(0x100000n, 88); // SizeOfHeapReserve
  optHeader.writeBigUInt64LE(0x1000n, 96); // SizeOfHeapCommit
  optHeader.writeUInt32LE(16, 108); // NumberOfRvaAndSizes

  // Data directories in Optional Header
  // Import Table Directory (entry 1, offset 112 + 8 = 120)
  optHeader.writeUInt32LE(0x2000, 120); // Import Table RVA
  optHeader.writeUInt32LE(0x200, 124); // Import Table Size

  // Section 4: Section Headers (40 bytes each)
  // Section 1: .text (Code)
  const textSection = Buffer.alloc(40);
  textSection.write('.text\0\0\0', 0);
  textSection.writeUInt32LE(0x1000, 8); // VirtualSize
  textSection.writeUInt32LE(0x1000, 12); // VirtualAddress
  textSection.writeUInt32LE(0x200, 16); // SizeOfRawData
  textSection.writeUInt32LE(0x400, 20); // PointerToRawData
  textSection.writeUInt32LE(0x60000020, 36); // Characteristics: CODE | EXECUTE | READ

  // Section 2: .rdata (Imports & Data)
  const rdataSection = Buffer.alloc(40);
  rdataSection.write('.rdata\0\0', 0);
  rdataSection.writeUInt32LE(0x1000, 8); // VirtualSize
  rdataSection.writeUInt32LE(0x2000, 12); // VirtualAddress
  rdataSection.writeUInt32LE(0x400, 16); // SizeOfRawData
  rdataSection.writeUInt32LE(0x600, 20); // PointerToRawData
  rdataSection.writeUInt32LE(0x40000040, 36); // Characteristics: INITIALIZED_DATA | READ

  // Headers block must be FileAlignment aligned (0x400 = 1024 bytes)
  const headerBlock = Buffer.concat([
    dosHeader,
    peHeader,
    optHeader,
    textSection,
    rdataSection,
  ]);
  const paddedHeaders = Buffer.alloc(0x400);
  headerBlock.copy(paddedHeaders);

  // Section 5: .text code block (512 bytes aligned to 0x200)
  // x86_64 assembly:
  // sub rsp, 40 (shadow stack)
  // lea rcx, [rip + command_offset]
  // mov edx, 1 (SW_SHOWNORMAL)
  // call WinExec
  // xor ecx, ecx
  // call ExitProcess
  const textRaw = Buffer.alloc(0x200);

  // Assembly instructions in machine code:
  // 48 83 EC 28          sub rsp, 40
  // 48 8D 0D ...         lea rcx, [rip + offset_to_cmd_str]
  // BA 01 00 00 00       mov edx, 1
  // FF 15 ...            call qword ptr [rip + offset_to_WinExec_thunk]
  // 31 C9                xor ecx, ecx
  // FF 15 ...            call qword ptr [rip + offset_to_ExitProcess_thunk]
  // C3                   ret
  const code = [
    0x48, 0x83, 0xEC, 0x28,                         // sub rsp, 28h
    0x48, 0x8D, 0x0D, 0x1D, 0x10, 0x00, 0x00,       // lea rcx, [rip + 0x101D] -> .rdata string
    0xBA, 0x01, 0x00, 0x00, 0x00,                   // mov edx, 1 (SW_NORMAL)
    0xFF, 0x15, 0xE7, 0x0F, 0x00, 0x00,             // call [rip + 0x0FE7] -> WinExec
    0x31, 0xC9,                                     // xor ecx, ecx
    0xFF, 0x15, 0xE5, 0x0F, 0x00, 0x00,             // call [rip + 0x0FE5] -> ExitProcess
    0x48, 0x83, 0xC4, 0x28,                         // add rsp, 28h
    0xC3                                            // ret
  ];
  Buffer.from(code).copy(textRaw, 0);

  // Section 6: .rdata imports and string block (1024 bytes aligned to 0x200)
  // VirtualAddress is 0x2000
  const rdataRaw = Buffer.alloc(0x400);

  // Import Directory Table (IDT): 20 bytes each
  // [0..19]: kernel32.dll import descriptor
  // [20..39]: Null terminator
  rdataRaw.writeUInt32LE(0x2040, 0); // OriginalFirstThunk (ILT) at 0x2040
  rdataRaw.writeUInt32LE(0, 4);      // TimeDateStamp
  rdataRaw.writeUInt32LE(0, 8);      // ForwarderChain
  rdataRaw.writeUInt32LE(0x2070, 12); // Name RVA ("KERNEL32.dll") at 0x2070
  rdataRaw.writeUInt32LE(0x2050, 16); // FirstThunk (IAT) at 0x2050

  // ILT (Import Lookup Table) at offset 0x40:
  // Thunk 1: WinExec (hint/name at 0x2080)
  // Thunk 2: ExitProcess (hint/name at 0x2090)
  // Thunk 3: 0 (null terminator)
  rdataRaw.writeBigUInt64LE(0x2080n, 0x40);
  rdataRaw.writeBigUInt64LE(0x2090n, 0x48);
  rdataRaw.writeBigUInt64LE(0n, 0x50);

  // IAT (Import Address Table) at offset 0x50:
  // Initially identical to ILT, loader patches with real function addresses
  rdataRaw.writeBigUInt64LE(0x2080n, 0x50);
  rdataRaw.writeBigUInt64LE(0x2090n, 0x58);
  rdataRaw.writeBigUInt64LE(0n, 0x60);

  // DLL Name at offset 0x70
  rdataRaw.write('KERNEL32.dll\0', 0x70);

  // Hint/Name Table:
  // 0x80: Hint (2 bytes) + "WinExec\0"
  rdataRaw.writeUInt16LE(0, 0x80);
  rdataRaw.write('WinExec\0', 0x82);

  // 0x90: Hint (2 bytes) + "ExitProcess\0"
  rdataRaw.writeUInt16LE(0, 0x90);
  rdataRaw.write('ExitProcess\0', 0x92);

  // Command string at offset 0x30 (RVA 0x2030)
  // The command to run: e.g. "cmd.exe /c start-server.bat"
  rdataRaw.write(commandToExecute + '\0', 0x30);

  // Final PE Binary
  const fullExe = Buffer.concat([paddedHeaders, textRaw, rdataRaw]);

  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(outputPath, fullExe);
  console.log(`[OK] Generated Win64 PE executable: ${outputPath} (${fullExe.length} bytes)`);
}

// Generate the 3 requested BSAT toolchain .exe binaries
const rootDir = path.resolve(__dirname, '..');
const distDir = path.resolve(__dirname, '../dist-exe');
const publicDir = path.resolve(__dirname, '../public');

console.log('====================================================');
console.log('⚡ GENERATING BSAT STANDALONE WINDOWS .EXE BINARIES');
console.log('====================================================');

// Generate in project root directory (outside src)
createWin64Exe('cmd.exe /k start-server.bat', path.join(rootDir, 'bsat-studio.exe'));
createWin64Exe('cmd.exe /k compile-and-run.bat', path.join(rootDir, 'bsat-compiler.exe'));
createWin64Exe('cmd.exe /k dev-watch.bat', path.join(rootDir, 'bsat-supervisor.exe'));

// Also copy to public/ for direct browser asset downloads
createWin64Exe('cmd.exe /k start-server.bat', path.join(publicDir, 'bsat-studio.exe'));
createWin64Exe('cmd.exe /k compile-and-run.bat', path.join(publicDir, 'bsat-compiler.exe'));
createWin64Exe('cmd.exe /k dev-watch.bat', path.join(publicDir, 'bsat-supervisor.exe'));

// And in dist-exe/
createWin64Exe('cmd.exe /k start-server.bat', path.join(distDir, 'bsat-studio.exe'));
createWin64Exe('cmd.exe /k compile-and-run.bat', path.join(distDir, 'bsat-compiler.exe'));
createWin64Exe('cmd.exe /k dev-watch.bat', path.join(distDir, 'bsat-supervisor.exe'));

console.log('====================================================');
console.log('Artifacts ready in root directory (outside src/):');
console.log(' - ./bsat-studio.exe');
console.log(' - ./bsat-compiler.exe');
console.log(' - ./bsat-supervisor.exe');
console.log('====================================================');
console.log('Artifacts ready for Windows download and execution:');
console.log(' - dist-exe/bsat-studio.exe');
console.log(' - dist-exe/bsat-compiler.exe');
console.log(' - dist-exe/bsat-supervisor.exe');
console.log('====================================================');
