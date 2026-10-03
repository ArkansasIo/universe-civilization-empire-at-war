#!/usr/bin/env node

/**
 * Build the BSAT Windows launchers.
 *
 * The repository intentionally uses the dependency-free PE generator so this
 * command works on Linux/macOS CI as well as on Windows. The generated
 * launchers invoke the batch files next to them; they do not bundle Node.js.
 */

const { spawnSync } = require("node:child_process");
const path = require("node:path");

const generator = path.resolve(__dirname, "generate-pe-exe.cjs");
const result = spawnSync(process.execPath, [generator], { stdio: "inherit" });

if (result.error) {
  console.error(`[ERROR] Failed to start executable generator: ${result.error.message}`);
  process.exit(1);
}

if (result.status !== 0) {
  console.error(`[ERROR] Executable generator exited with status ${result.status}`);
  process.exit(result.status ?? 1);
}

console.log("[OK] Executable build completed successfully.");
