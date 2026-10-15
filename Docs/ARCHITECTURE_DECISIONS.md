# Keputusan implementasi M0/M1

## Tambahan kandidat MVP — 14 Oktober 2026

- Host Application diperluas untuk preferences, arsip, pameran dan lihat-di-galeri. Preferences DTO tetap tanpa Unity; Infrastructure menyimpan JSON lokal secara atomik dan menyimpan salinan diagnostik jika rusak. Composition root menerapkan audio, input, FOV dan kualitas. Tidak ada dependency sirkular atau package baru.
- UI dipecah ke partial `MuseumUI` untuk katalog/detail, media, dan preferences; viewer video/cover memiliki ownership texture/decoder dan cleanup saat disable. Pencarian multikategori OR dipusatkan pada Domain dan diuji bersama irisan tahun/status/search.
- Scene existing tetap dipakai. Perubahan binding terminal arsip/auditorium, label, cover dan spawn lihat-di-galeri berlangsung di memory runtime. Tidak ada rewrite YAML atau regenerasi scene/prefab/material pengguna. Screen stand tetap menjadi fallback `displayType`.
- Cover stan dimuat bergiliran (maksimum satu request cover dalam galeri), document viewer hanya memuat halaman aktif. Video lokal dibatasi MP4 dan 256 MB, timeout prepare 20 detik, satu viewer aktif; stop/release saat ditutup.
- `AtlasMvpBuild.Run` menyediakan perbandingan hash asset sebelum/sesudah builder dua kali, cek preferences dan build. `MvpSmoke` opt-in menguji alur UI/media/zona dengan profil penyimpanan terpisah; profiling tambahan opt-in 120 detik. Runner ini belum dijalankan pada kode baru karena startup Unity terhalang IPC.
- Perluasan M2/M3 dilakukan mengikuti permintaan pengguna menuju MVP; tidak menyatakan gate visual/interaksi M1 ataupun DoD MVP sudah lulus. Laporan historis dan terbaru dipisahkan.

## Keputusan historis M0/M1

1. Pertahankan Unity/URP/packages pengguna; tanpa backend, framework DI, atau Addressables.
2. Domain dan Application tidak merujuk UnityEngine. Infrastructure mengurai DTO dengan JsonUtility. Presentation berkomunikasi melalui IAtlasHost; Runtime menjadi composition root. Editor assembly hanya untuk Editor.
3. Bootstrap memegang satu rig, UI, dan EventSystem. Frontend/Lobby/Gallery dimuat additive. Tujuan dan spawn divalidasi sebelum zona lama dilepas. Permintaan transisi ganda diblokir.
4. Gameplay dan Navigation adalah action map Input System runtime. Gameplay dinonaktifkan ketika modal/loading/kehilangan fokus. UI memakai InputSystemUIInputModule. Tidak menggunakan Time.timeScale sebagai pause.
5. Karakter 1,8 m, radius 0,3 m, kamera 1,65 m, FOV 65°, gerak 3,5/5,5 m/s. Ray 2,5 m menghentikan pencarian pada collider pertama, termasuk dinding. Layer Ignore Raycast digunakan untuk rig.
6. JSON offline publik ditolak jika berisi draft/private atau media tanpa offlineAllowed. Path relatif tervalidasi, URL sumber dibatasi HTTPS Drive/Docs. Ini bukan implementasi otorisasi server fase connected.
7. Media viewer memiliki satu texture aktif, request dibatalkan dan texture dihancurkan saat panel ditutup. Batas file 16 MB dan dimensi metadata 4096 px. Tidak ada cache tak terbatas. Bundle lokal harus dikurasi; decoder tetap memerlukan audit lebih lanjut sebelum menerima media dari jaringan.
8. Builder memakai Unity Editor API. Scene/material/prefab yang sudah ada tidak ditimpa. Jalankan ulang untuk mengisi yang belum ada. Untuk regenerasi visual yang sudah diedit, buat output terpisah; jangan menghapus scene pengguna.
9. Fixture PNG adalah diagram wireframe buatan kode, bukan screenshot karya nyata. Konten disimpan di JSON, bukan pada scene.
10. M1 mencakup menu, lobby, galeri 8 slot, detail, viewer gambar, katalog dasar/paging dan validasi. Filter UI lengkap, dokumen/video, arsip, auditorium, settings, bookmark dan backend tetap milestone berikutnya.
11. Read-only original-editor lock ditangani dengan salinan validasi. Build tidak memerlukan perubahan package atau menghentikan proses pengguna.

Rujukan konsep InputAction enable/disable: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/api/UnityEngine.InputSystem.InputAction.html . Implementasi disesuaikan dengan package lokal 1.20.0; versi rujukan tidak dipakai untuk mengganti dependency.
