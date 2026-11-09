<div align="center">

# Mey Drive

**Assetto Corsa için fareyle direksiyon, klavyeyle pedal. FreePIE gerekmez.**

[**İndir**](../../releases/latest) · [English](README.en.md)

<img src="docs/mey-drive-v1.1-tr.png" alt="Mey Drive penceresi" width="440">

</div>

Mey Drive fareni direksiyona, W / S / C tuşlarını gaz, fren ve debriyaja çevirip doğrudan vJoy'a gönderir.
Direksiyon ortada yavaş, uçlara doğru hızlı döner; pedallar yumuşak basılır. Döngü sabit 5 ms'dir, oyun
tam ekrandayken de kaymaz.

- Hassasiyeti kaydırıcıyla ya da fare tekeriyle ayarla, anında geçer.
- **Ctrl + 0** imleci gizler ve sol alt köşeye kilitler. Kısayolu uygulamadan değiştirebilirsin.
- **Assetto Corsa ile başlat:** açıkken Mey Drive tepside bekler; Content Manager veya oyun açılınca script kendiliğinden başlar, kapanınca durur.
- Canlı direksiyon göstergesi ve tepsi menüsü.
- Türkçe ve İngilizce: pencerenin üstündeki **TR / EN** ile seç.

## Kurulum

1. [Son sürümden](../../releases/latest) **MeyDrive.exe**'yi indir ve çalıştır.
2. **BAŞLAT**'a bas. vJoy kurulu değilse Mey Drive onu kendisi kurup ayarlar; Windows bir kez izin sorar.
3. Content Manager → Ayarlar → Kontroller'de girişi *Wheel*, cihazı *vJoy Device* yap:
   direksiyon **X**, gaz **Y**, debriyaj **Z**, fren **Rz**.

FreePIE açıksa kapat; vJoy'u aynı anda tek program kullanabilir. Mey Drive, [vJoy](https://github.com/BrunnerInnovation/vJoy)'un (MIT) kurulum dosyasını içinde taşır.

| Tuş | Ne yapar |
|---|---|
| Fare | direksiyon |
| W / S / C | gaz / fren / debriyaj |
| B, Sol Shift, Sol Ctrl, C, Z, Space, E, Q | vJoy buton 1–8 |
| Ctrl + 0 | imleci gizle / göster |

## Sorular

<details>
<summary><b>Windows "Bilgisayarınız korundu" diyor.</b></summary>

Uygulama ücretli bir sertifikayla imzalı değil. *Ek bilgi*'ye, sonra *Yine de çalıştır*'a bas.
</details>

<details>
<summary><b>Güvenli mi?</b></summary>

Kodun hepsi burada. Her sürüm bir bilgisayarda değil, GitHub Actions'ta bu koddan derlenir ve SHA-256'sıyla gelir.
</details>

<details>
<summary><b>Nasıl kaldırırım?</b></summary>

*Assetto Corsa ile başlat*'ı kapat, tepsi menüsünden *Kapat*'ı seç, exe'yi ve `%LOCALAPPDATA%\MeyDrive` klasörünü (ayarlar) sil.
</details>

## Lisans

[MIT](LICENSE) © weojiro
