<div align="center">

# Mey Drive

**Mouse steering and keyboard pedals for Assetto Corsa. No FreePIE needed.**

[**Download**](../../releases/latest) · [Türkçe](README.md)

<img src="docs/mey-drive-v1.1-en.png" alt="The Mey Drive window" width="440">

</div>

Mey Drive turns your mouse into a steering wheel and W / S / C into throttle, brake and clutch, and sends them
straight to vJoy. Steering is slow around the centre and faster towards the ends; the pedals press smoothly. The
loop runs every 5 ms, also while the game is full screen.

- Set the sensitivity with the slider or the mouse wheel; it applies at once.
- **Ctrl + 0** hides the cursor and locks it to the bottom-left corner. Change the shortcut in the app.
- **Start with Assetto Corsa:** Mey Drive waits in the tray; the script starts by itself when Content Manager or the game opens, and stops when they close.
- Live steering meter and tray menu.
- Turkish and English: pick **TR / EN** at the top of the window.

## Install

1. Download **MeyDrive.exe** from the [latest release](../../releases/latest) and run it.
2. Press **START**. If vJoy is missing, Mey Drive installs and sets it up by itself; Windows asks for permission once.
3. In Content Manager → Settings → Controls, set the input to *Wheel* and the device to
   *vJoy Device*: steering **X**, throttle **Y**, clutch **Z**, brake **Rz**.

Close FreePIE if it is open; only one program can use vJoy at a time. Mey Drive carries the installer of [vJoy](https://github.com/BrunnerInnovation/vJoy) (MIT).

| Key | What it does |
|---|---|
| Mouse | steering |
| W / S / C | throttle / brake / clutch |
| B, Left Shift, Left Ctrl, C, Z, Space, E, Q | vJoy buttons 1–8 |
| Ctrl + 0 | hide / show the cursor |

## Questions

<details>
<summary><b>Windows says "Windows protected your PC".</b></summary>

The app is not signed with a paid certificate. Choose *More info* and then *Run anyway*.
</details>

<details>
<summary><b>Is it safe?</b></summary>

The code is all here. Every release is built from it by GitHub Actions, not on a PC, and comes with its SHA-256.
</details>

<details>
<summary><b>How do I remove it?</b></summary>

Turn off *Start with Assetto Corsa*, choose *Close* in the tray menu, then delete the exe and the `%LOCALAPPDATA%\MeyDrive` folder (settings).
</details>

## License

[MIT](LICENSE) © weojiro
