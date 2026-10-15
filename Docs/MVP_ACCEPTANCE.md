# Kandidat MVP lokal — checklist penerimaan

Tanggal implementasi: 14 Oktober 2026. Data seluruhnya sintetis. Status saat diserahkan: source/API compile dan data checks PASS; Editor/Play/build terbaru belum terverifikasi akibat IPC Package Manager/Licensing.

## Scene tersedia

`Assets/_Project/Scenes/Bootstrap.unity` adalah titik mulai. Frontend, Lobby, Gallery, Archive, Showcase dan Courtyard sudah tersedia sejak build 10 Oktober. SampleScene bawaan tidak berisi museum. Perubahan sesi ini tidak menimpa scene/prefab/material tersebut.

## Alur yang harus diuji pada build baru

1. Menu → Jelajahi → Lobby; WASD, mouse, Shift, E dan M berfungsi. Tidak dapat menembus dinding; target di balik tembok atau lebih jauh dari 2,5 meter tidak dapat dipilih.
2. Katalog menampilkan 9 proyek sintetis pada 2 halaman. Pilih beberapa kategori (OR), tahun/status (AND), lalu Reset. Back dari detail mempertahankan filter/halaman dan scroll.
3. Buka `Contoh Dokumentasi Aksesibel` di halaman 2 → Lihat di galeri. Galeri menampilkan 1 stan, 7 slot lain tersembunyi; halaman sebelumnya menampilkan 8 stan. Gambar cover dan label tidak bertumpuk.
4. `Contoh Atlas Catatan` → Dokumentasi → laporan dua halaman. Ganti halaman, zoom/pan/reset; hanya satu texture halaman aktif. Dokumen tanpa preview menjelaskan bahwa preview/sumber belum tersedia. Tidak ada URL Drive fiktif.
5. Proyek yang sama → Timeline. Tanggal berurutan; tombol bukti membuka dokumen. Proyek tanpa milestone menampilkan keadaan kosong.
6. Demo → video sintetis. Play, pause, seek, volume; tutup dan pindah ruang menghentikan audio. Periksa fallback ketika berkas hilang/rusak atau format tak didukung.
7. Simpan bookmark → restart → bookmark masih tersedia. ID yang dihapus dari katalog tampil tidak tersedia dan dapat dihapus dari bookmark.
8. Ubah audio, sensitivitas, FOV, kualitas, ukuran teks 100–150%. Restart mempertahankan nilai. Reset mengembalikan pengaturan tanpa menghapus bookmark. Periksa layout pada 1280×720 dan 1920×1080.
9. Arsip → terminal menampilkan preset proyek archived. Auditorium → podium/layar membuka koleksi pameran dan video. Peta menjangkau semua lima zona dan dapat kembali ke Lobby.
10. Putus jaringan: katalog, halaman dokumen, gambar, video tetap tersedia lokal. Jalankan smoke dan profiling di bawah; periksa Console/player log.

## Otomasi yang sudah disiapkan, belum dijalankan pada build terbaru

- `AtlasMvpBuild.Run`: builder dua kali dengan pembandingan hash asset, validasi scene/data/checksum, persistence dan recovery preferences, lalu build Windows.
- `-atlasMvpSmoke`: klik UI penting, settings/bookmarks, galeri halaman 2, dokumen, video, fallback, 50 siklus viewer dan 20 putaran lima zona. Preferences uji menggunakan subfolder `MvpSmoke`, terpisah dari profil normal.
- `-atlasProfile`: sesudah smoke, mengukur 120 detik sudut pandang berputar (24 detik tiap zona, warmup 2 detik). Mencatat hardware, resolusi/kualitas, median FPS, p95 frame, memori dan waktu transisi. Bukan sertifikasi build release atau uji gerak manual.

Output ke `Docs/Evidence/MVP` relatif working directory. Exit code smoke nonzero jika assertion gagal; watchdog 420 detik. Screenshot baru hanya tersedia setelah runner benar-benar dijalankan.

## Gate rilis yang masih terbuka

- Import/compile Unity dan Windows build terbaru.
- Visual/interaksi/audio manual, screenshot aktual, keterbacaan teks dan tidak ada missing material.
- Playback codec di Windows, lifecycle dan persistence dalam runtime.
- Profiling pada hardware tercatat dan evaluasi budget spesifikasi.
- Varian stan pedestal/document berbasis `displayType` masih menggunakan screen stand sebagai fallback. Model dekorasi pedestal/document existing tetap tersedia; belum menjadi tiga varian binding final.
- Tidak ada klaim koneksi Drive, konten asli, Web, atau rilis connected.
