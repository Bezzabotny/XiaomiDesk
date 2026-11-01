# Verification — 0.4.1 beta preparation

- Complete desktop suites passed from an unrelated current working directory; scratch files stay under repository .build. Includes native page rendering, gallery filters/identity/cache, bridge sessions, ADB process cancellation, transfer integrity, settings and startup store tests.
- Desktop executable compiled successfully. Android helper compiled and development signature verified. Release build correctly rejects absent signing input; runtime packaging correctly rejects a development APK unless explicitly labelled Development.
- New RED→GREEN checks: actionable Windows child-process failure message; explained empty gallery filter; release archive excludes fake startup-error.txt and .p12 fixtures.
- Dependency hash verification and extraction succeeded with the original official scrcpy5.0.1 archive. A new network download could not resolve github.com here, so the hosted download branch and GitHub Actions have not been run end-to-end.
- Fresh review found mutable dist packaging could leak local leftovers. Fixed by clean allowlisted staging; regression failed before the change and passed afterward. Documentation discrepancies about dev-key location/source distribution/JDK were corrected.
- No real device was modified in this preparation turn. Prior0.4 real-device checks remain described in README; embedded recording remains experimental, without a claim of universal0xC0000142 repair.
- Repository publication targets Bezzabotny/XiaomiDesk. Hosted workflow results should be checked separately in the Actions tab.
- Runtime release candidate reuses the exact previously signed helper0.4, SHA-25652428a93451107f2d15d6b1f47c084465a1298d5902a5d5a0b4fbf9605385082. APKv2/v3 signatures verified again. Android source is unchanged. No signing key or password is read; import and packaging check the artifact checksum. Development APKs are excluded from the release candidate.
