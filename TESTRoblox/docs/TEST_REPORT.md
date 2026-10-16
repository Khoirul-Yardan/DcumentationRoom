# Laporan pengujian — 16 Oktober 2026

**Hasil: 37 pemeriksaan binary/konten lulus. Belum ada bukti Studio load, kompilasi Luau, atau Play test.** Hasil ini tidak menyatakan seluruh MVP sudah siap rilis.

## Artefak terukur

| Metrik | Hasil |
|---|---|
| File utama | `Place1.rbxl`, 255.623 byte, genuine binary RBXL |
| SHA-256 file utama | `95d87b48d13148320d7e4a6177a4161580cfd2cff2b36b6a6f8dbe7f15f13918` |
| Total instance | 1.205 |
| BasePart | 987: 954 Part, 32 Seat, 1 SpawnLocation |
| Instance input dipertahankan | 79 referent; parent baseplate sengaja dipindah |
| Zona | 5, ditambah Landscape dan Bridges |
| Stan fisik | 8 |
| Prompt | 15; LOS aktif; jarak 12 studs |
| Kursi forum | 32 |
| Katalog | 12 proyek sintetis public/published, 12 dokumen teks / 24 halaman |
| Script | 1 server Script, 1 LocalScript, 3 ModuleScript termasuk validator Studio |
| Backup input | 74.211 byte; SHA-256 `fae69a9c69dc2e9f117ba58ef431a7c60bfabea4a185f5ca1452c87ed6afe7a8` |

## Pemeriksaan yang benar-benar dijalankan

```powershell
python -m py_compile tools/rbxl.py tools/build_museum.py tools/sync_catalog.py tools/make_catalog.py tools/validate_place.py tools/render_preview.py
python tools/sync_catalog.py
python tools/build_museum.py
python tools/validate_place.py
python tools/render_preview.py
```

- Kompilasi sintaks seluruh tool Python berhasil.
- Input Zstd berhasil didekompresi; output dapat dibaca kembali oleh validator.
- Jumlah class/instance header, referent unik, parent resolve, dan graf tanpa siklus benar.
- Semua referent asli dipertahankan; parent asli selain baseplate sama.
- Source setiap Script/LocalScript/ModuleScript sama dengan file source di disk.
- Semua Part anchored; ukuran positif/finite; rotasi CFrame orthonormal.
- Kelima zona, delapan stand, lima belas prompt, dan 32 Seat tersedia.
- Sampel jalur berjalan X/Z menunjukkan keempat koneksi jembatan memiliki lantai kontinu pada Y=4.
- StreamingEnabled false tertanam eksplisit.
- Validasi katalog menerima 12 proyek/12 dokumen, menolak fixture duplikat, private, kategori unknown, dan tanggal invalid.
- Codec binary diuji roundtrip untuk tipe property yang dipakai, termasuk referent delta/zigzag, CFrame, Vector3, UDim2, dan UTF-8.
- Preview teknis dibuat dari geometri yang dibaca kembali dari output binary dan diperiksa visual. Ini bukan render engine Roblox, tidak menampilkan font/shader/lighting persis seperti Studio.

Rincian assertion otomatis tersedia di `validation-results.json`. Validator binary dan writer berbagi kode codec; roundtrip tidak setara dengan kompatibilitas loader Studio yang independen.

## Yang belum dijalankan

| Gate | Status / alasan |
|---|---|
| Studio open/save roundtrip | Belum; editor tidak berhasil tersedia di sesi otomasi |
| Kompilasi Luau dan module assertions | Belum; tidak ada runtime Luau terhubung |
| Play end-to-end | Belum |
| Input modal, ray LOS, respawn, seated travel | Belum |
| Dua pemain / paging independen | Belum |
| Touch, gamepad, layar kecil, fokus keyboard | Belum |
| Render asli Studio / screenshot lima zona | Belum |
| FPS, memori, stress viewer/travel | Belum |
| Drive, DataStore, backend, video | Fitur belum diimplementasikan |
| Publikasi Roblox | Tidak dilakukan |

StudioMCP menjawab initialize, tetapi mengembalikan `tools: []`. Screenshot desktop gagal (`The handle is invalid`). Tidak ada screenshot hitam yang dipakai sebagai bukti keberhasilan.

## Checklist berikutnya di Studio

1. Buka place, pastikan semua model terbaca; Save As salinan untuk menguji loader/serializer Studio.
2. Jalankan `require(game.ServerStorage.AtlasValidation).run()` di Command Bar Edit mode. Uji ini membaca struktur dan menjalankan filter/paging Luau pada fixture 0, 1, 8, 9, dan 100 item, pencarian `%` literal, filter gabungan, duplicate IDs, dan draft snapshot. Belum dieksekusi oleh agent.
3. Play/F5: menu → mulai → stan → ringkasan → dokumen → halaman 2 → kembali → model 3D → tutup.
4. Katalog: cari “Orbit”, gabungkan kategori/tahun/status, cari `%`, reset, simpan/batalkan bookmark, buka halaman 2.
5. Galeri: ganti halaman; empat slot kosong harus hilang; kembali halaman 1; ulangi dengan dua client agar halaman berbeda tidak saling menimpa.
6. Peta: uji lima tujuan, cooldown, ID invalid, travel dari kursi, jatuh di laut, dan respawn ketika panel terbuka.
7. Uji portrait/landscape dan gamepad; cek tombol 48 px, teks besar, scroll, fokus, dan pemulihan kontrol.
8. Ulangi buka/tutup Viewport 50 kali dan travel 20 kali; ukur memori/FPS pada perangkat sasaran.

## Batas implementasi

Semua isi proyek contoh, bukan inventaris Drive. Dokumen adalah teks; tidak ada parser PDF/Office. Video dan foto asli belum tersedia. Bookmark/pengaturan sesi tidak persisten. Kandidat Forest Pack tidak diimpor. Perubahan Studio manual tidak digabung otomatis oleh builder. Rilis menunggu gate runtime dan review konten.
