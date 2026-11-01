# 0.4.1 beta release preparation

User requested direct preparation and polishing for GitHub. Retain native C#5/Java architecture and existing behavior; label the embedded-recording path experimental until verified outside the controlled harness.

- [x] Portable repository: source/, scripts/, docs/, ignored .build/dist/tools. No device-specific instructions, keys, dumps, cached photos or binaries in Git.
- [x] Portable compiler/test scripts; explicit release signing inputs and development-key separation.
- [x] Check gallery lifecycle and improve empty-state feedback; actionable stream error; remove obsolete overview and duplicate UI text.
- [x] Add README, third-party notices, proposed MIT license, issue template, reproducible build/package scripts, checksums and Windows CI. Release workflow creates drafts only.
- [x] Run full tests and builds, scan tracked files, review, create source and runtime archives. Push only to the repository the user identifies and with available authentication.

Review focus: escaped paths outside repository; signing secrets/debug key accidentally shipped; stale gallery results affecting new page; CI secrets on pull requests; public release documentation promises beyond verified behavior.

The candidate reuses the verified existing helper0.4, with unchanged Android source and no signing credential access. Embedded recording remains experimental. Hosted CI still needs a real GitHub run.
