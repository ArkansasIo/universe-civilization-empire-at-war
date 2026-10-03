/**
 * BSAT Executable Builder Script
 * Uses @yao-pkg/pkg to produce real Windows .EXE files
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const outDir = path.resolve(__dirname, '../dist-exe');
if (!fs.existsSync(outDir)) {
  fs.mkdirSync(outDir, { recursive: true });
}

console.log('====================================================');
console.log('🔨 BUILDING BSAT TOOLCHAIN WINDOWS .EXE BINARIES');
console.log('====================================================\n');

try {
  console.log('[1/2] Compiling bsat-compiler.exe for Windows x64...');
  execSync(
    'npx @yao-pkg/pkg bin/bsat-compiler.cjs --target node18-win-x64 --output dist-exe/bsat-compiler.exe',
    { stdio: 'inherit' }
  );
  console.log('[OK] Generated dist-exe/bsat-compiler.exe\n');

  console.log('[2/2] Compiling bsat-studio.exe for Windows x64...');
  execSync(
    'npx @yao-pkg/pkg bin/bsat-cli.cjs --target node18-win-x64 --output dist-exe/bsat-studio.exe',
    { stdio: 'inherit' }
  );
  console.log('[OK] Generated dist-exe/bsat-studio.exe\n');

  console.log('====================================================');
  console.log('✅ EXECUTABLE BUILD COMPLETE!');
  console.log('Artifacts generated in dist-exe/:');
  fs.readdirSync(outDir).forEach((file) => {
    const stats = fs.statSync(path.join(outDir, file));
    const sizeMB = (stats.size / 1024 / 1024).toFixed(2);
    console.log(` - ${file} (${sizeMB} MB)`);
  });
  console.log('====================================================');
} catch (err) {
  console.error('[ERROR] Failed to compile executables:', err.message);
  process.exit(1);
}
