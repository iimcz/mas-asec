ASEC - Acquisition Station Element Commander
===

This application used as a software module in the [**Multisystem Acquisition Station**](https://github.com/iimcz/mas-firmware.git). See the above link for details about the project, additional instructions and more information (the above link is in Czech).

This is the C# application for managing digitalization processes of older physical media, conversion of resulting digital artifacts for later emulation and managing the emulation process.

It is intended to work together with the [Emulation as a Service](https://gitlab.com/emulation-as-a-service) software, namely the modified version [here](https://github.com/iimcz/eaas-server).

Various parts managing the digitalization and conversion processes require additional dependencies. Namely:

- \[DT\] Greaseweazle - requires the [Greaseweazle](https://github.com/keirf/greaseweazle) application for recording floppy disk flux information.
- \[DT\] Ffmpeg recording - requires the [Ffmpeg](https://ffmpeg.org/) application for recording audio tracks from audio tapes.
- \[CT\] Greaseweazle - same as above, this time to convert flux recordings to floppy images.
- \[CT\] FUSE utils - the [FUSE](https://fuse-emulator.sourceforge.net/) Spectrum emulator utilities to convert the audio tape recording to a .tap file, better suited for emulation.

Where \[DT\] means Digitalization Tool and \[CT\] means Conversion Tool.

Compilation
---

All code for this application lives under the `backend` directory. To compile it, run:

```sh
dotnet build backend
```

To compile a distribution, self-contained version, run:

```sh
dotnet publish --sc -c Release backend
```

Installation
---

See the `dist` directory for additional files, such as the `systemd` unit file.

To install the application, run the `publish` command, then copy the resulting `publish` directory to `/opt/mas-asec`, the use the included `systemd` unit (modified to your needs) to have the application managed by the init system.

License
---
See [LICENSE](./LICENSE)