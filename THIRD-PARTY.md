# Third-party components

XiaomiDesk is independent of Xiaomi and Microsoft. The Windows runtime ZIP bundles unmodified components from the official scrcpy5.0.1 Windows x64 release:
https://github.com/Genymobile/scrcpy/releases/tag/v5.0.1

The download is pinned to SHA-256 b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5.

- scrcpy and its Android server: Apache2.0, https://github.com/Genymobile/scrcpy/tree/v5.0.1 ; license included as tools/LICENSE.txt.
- Android Debug Bridge: Apache2.0, https://android.googlesource.com/platform/packages/modules/adb/ .
- SDL3: zlib license, https://github.com/libsdl-org/SDL .
- FFmpeg libraries: licensing/source information https://ffmpeg.org/legal.html ; upstream Windows dependency build configuration https://github.com/Genymobile/scrcpy/blob/v5.0.1/app/deps/README.md .
- libusb: LGPL2.1, https://github.com/libusb/libusb .

The third-party binaries are copied without modification from the pinned upstream release. Source and corresponding build recipes are linked above; XiaomiDesk's MIT license does not replace these licenses. Runtime binaries remain separate DLLs/files.

The XiaomiDesk Android helper uses API35, minAPI26 and Java8 bytecode, built with JDK17+ and Android build-tools35.0.0. It has no third-party Java dependencies. The Windows executable uses .NET Framework. XiaomiDesk source and build scripts are distributed through the Git repository/source archive, separately from the lightweight runtime ZIP. Signing keys are never included.
