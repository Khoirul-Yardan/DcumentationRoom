# ATLAS — Museum Kepulauan Ide

Implementasi awal museum Roblox Studio dengan lima pulau: atrium orbital, galeri, perpustakaan arsip, forum, dan taman. Aset native Roblox gratis; 12 proyek contoh, 8 stan, katalog, pembaca dokumen teks, model 3D, bookmark sesi, dan peta perjalanan cepat.

## Buka dan coba

1. Di Roblox Studio pilih **Open from File**, lalu buka `D:\TESTRoblox\Place1.rbxl`.
2. Lima zona sudah tersimpan sebagai model di `Workspace > AtlasMuseum`.
3. Tekan **Play / F5**. Pilih **Mulai menjelajah** atau **Buka katalog proyek**.
4. Gunakan **WASD**, **E** pada stan, **K** untuk katalog, **M** untuk peta. Pada touch/gamepad gunakan kontrol Roblox dan tombol layar.
5. Untuk validasi data di Command Bar: `require(game.ServerStorage.AtlasValidation).run()`.

**Status:** 37 pemeriksaan struktural/konten lulus. Pembukaan file, kompilasi Luau, dan Play di Studio belum berhasil dijalankan dari lingkungan otomasi ini. File ini belum dipublikasikan. Detail ada di [laporan pengujian](docs/TEST_REPORT.md).

## File utama

- `Place1.rbxl`: place hasil adaptasi, format binary Roblox.
- `backups/Place1.original.rbxl`: backup asli sebelum perubahan.
- [Spesifikasi Roblox lengkap](ROBLOX_PROJECT_MUSEUM_MASTER_SPEC.md).
- [Panduan edit dan build](docs/BUILD_AND_RUN.md).
- [Pratinjau geometri](docs/museum-geometry-preview.png): visualisasi teknis, bukan screenshot Studio.
- `src/`: source Luau dan katalog JSON yang ditanam ke place.

Semua proyek/dokumen saat ini **sintetis dan berlabel contoh**. Drive belum diimpor. Bookmark/pengaturan hanya disimpan selama sesi. Aset Creator Store pihak ketiga dan playback video belum ditambahkan.

## Rebuild

```powershell
python tools/sync_catalog.py
python tools/build_museum.py
python tools/validate_place.py
```

Builder mengganti `Place1.rbxl` dari template backup dan source. **Cadangkan perubahan manual Studio terlebih dahulu.** Tidak perlu menjalankan builder hanya untuk bermain.
