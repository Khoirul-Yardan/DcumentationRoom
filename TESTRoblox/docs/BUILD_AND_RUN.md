# Menjalankan, mengedit, dan membangun ulang

## Menjalankan tanpa tool tambahan

Buka `Place1.rbxl` melalui Roblox Studio **Open from File**. Scene sudah berisi objek editabel. Play/F5 menjalankan `AtlasServer` dan `AtlasClient`. Tidak perlu Rojo, Lune, Python, HTTP Services, API Services, DataStore, atau publish untuk mencoba versi awal secara lokal di Studio. Studio/akun/asset caching tetap mengikuti kebutuhan platform Roblox; ini bukan build executable offline.

World root: `Workspace.AtlasMuseum`. Source runtime ada pada `ServerScriptService.AtlasServer`, `StarterPlayer.StarterPlayerScripts.AtlasClient`, dan `ReplicatedStorage.Atlas`. Konten publik berada pada ModuleScript `Catalog`.

## Validasi di Studio

Dalam **Edit mode**, buka Command Bar dan jalankan:

```lua
require(game.ServerStorage.AtlasValidation).run()
```

Assertion ini membaca katalog serta struktur dan menguji query/paging Luau. Tidak mempublikasikan, menulis DataStore, atau memindahkan karakter. Setelah itu tekan Play dan jalankan matriks manual pada `TEST_REPORT.md`. Assertion Studio ini **belum dijalankan oleh agent**.

## Mengganti konten

Edit `src/catalog.json`, lalu:

```powershell
python tools/sync_catalog.py
python tools/build_museum.py
python tools/validate_place.py
```

`sync_catalog.py` memeriksa field publik dan memperbarui ModuleScript. `build_museum.py` membuat ulang museum dari **backup asli**, lalu menulis `Place1.rbxl`. Script tidak membaca perubahan manual di place hasil. Cadangkan file hasil edit Studio sebelum rebuild. Jangan menjalankan `make_catalog.py` untuk memperbarui konten nyata: tool itu khusus mereset 12 fixture sintetis.

Untuk perubahan langsung tanpa rebuild, edit ModuleScript/script/geometri di Studio lalu Save. Jika ingin source disk tetap menjadi sumber utama, salin perubahan Luau kembali ke `src/` dan perbarui generator untuk geometri.

## Dependensi builder

- Python 3.10+.
- Library Zstd yang sudah disertakan Git for Windows: `C:\Program Files\Git\mingw64\bin\libzstd.dll`.
- Alternatif lokasi: set environment variable `ATLAS_ZSTD_DLL` ke library kompatibel.
- Tidak membutuhkan instalasi pip/network untuk build atau validasi.
- Render pratinjau opsional memakai numpy/matplotlib yang sudah ada: `python tools/render_preview.py`.

Patcher mempertahankan byte chunk lama yang tidak diubah. Class Part template diganti dengan baseplate yang diadaptasi + geometri museum. Referent seluruh 79 objek awal tetap ada; properti yang sengaja diubah meliputi baseplate, spawn, lighting, kamera, streaming, decal/texture baseplate, dan konfigurasi avatar. File output adalah binary RBXL, bukan XML dengan ekstensi palsu.

## Publikasi

Belum dilakukan. Selesaikan Play test, dua client, mobile, dan review konten terlebih dahulu. Publikasikan melalui Studio hanya setelah pemilik memilih experience tujuan. Jangan mengaktifkan akses DataStore produksi untuk tes.

## Memulihkan input asli

Backup berada di `backups/Place1.original.rbxl` (74.211 byte). Buka langsung sebagai place terpisah untuk membandingkan. Jika ingin memulihkan nama `Place1.rbxl`, simpan salinan hasil museum terlebih dahulu agar kedua versi tetap tersedia.
