# Task board — 14 Oktober 2026

Milestone aktif: **M2/M3 — kandidat MVP lokal, menunggu validasi Unity**. Permintaan pengguna melanjutkan sampai MVP menjadi cakupan sesi ini. Gate interaksi/visual M1 tetap terbuka; implementasi lanjutan tidak dianggap menutup gate tersebut.

| Task | Status | Implementasi dan gate |
|---|---|---|
| T-001 Audit | done | Unity 6000.3.23f1, URP/package existing dan SampleScene dipertahankan |
| T-002 DTO/validator/fixture | review | 9 proyek sintetis, 3 kategori/tahun, 2 dokumen, 6 media; 45 domain checks dan 34 asset/dependency checks PASS; import terbaru belum diuji |
| T-003 Bootstrap/UI/input | review | Composition root, modal stack, input M, preferences; compile API PASS; runtime terbaru belum diuji |
| T-004 Player/interaksi | review | WASD/mouse/collision/ray 2,5 m existing; sensitivitas/FOV ditambahkan; uji interaktif masih diperlukan |
| T-005 Scene/prefab builder | review | 7 scene existing, 24 model kit existing; runner MVP menjaga hash scene/prefab/material; runner belum bisa dijalankan dalam Editor sesi ini |
| T-006 Lobby/galeri | review | 8 slot, paging, cover gambar world-space dengan decode bergiliran, label status; butuh screenshot baru |
| T-007 Detail/viewer gambar | review | Tab ringkasan/dokumen/galeri/demo/timeline; context stack dan cleanup; compile API PASS |
| T-008 Katalog lanjut | review | Search, kategori OR, tahun/status AND, reset, paging, lihat-di-galeri; domain AND/OR/paging PASS; klik UI belum diuji |
| T-009 Dokumen/video | review | Preview per halaman, sumber allowlist/fallback, MP4 play/pause/seek/volume/cleanup; fixture codec diperiksa; playback Unity belum diuji |
| T-010 Timeline/bookmark/settings | review | Urutan tanggal dan bukti, bookmark persisten/ID hilang, audio/sensitivitas/FOV/kualitas/teks/reset; uji persistence Editor belum dijalankan |
| T-011 Arsip/auditorium/map | review | Preset arsip pada terminal, koleksi pameran/video, peta lima zona dan kembali Lobby; runtime terbaru belum diuji |
| T-012 Visual kit/lighting/UI | review | Kit dan ruang lama dipertahankan; cover/title stan dan UI disesuaikan; kualitas visual, varian displayType pedestal/document, serta profiling masih terbuka |
| T-013/T-014 Drive/backend | deferred | Fase connected; isi Drive tidak direkayasa, tidak diperlukan untuk kandidat MVP lokal |
| T-015 Remote/offline export | partial | Bundle lokal dan builder Windows tersedia; remote snapshot menunggu connected |
| T-016 QA/release | blocked environment | Editor startup gagal Package Manager IPC; jalur cached gagal Licensing IPC. Build terbaru, smoke, profiling, uji manual belum ada |

Owner sesi: Codex, tanpa agent paralel. Workspace bukan Git repository.

Langkah berikutnya: buka proyek dengan Unity Hub/Editor berlisensi pada mesin pengguna, tunggu import, jalankan **Tools > Project Atlas > Build MVP Offline Package**, lalu smoke dan checklist `MVP_ACCEPTANCE.md`. Mulai Play dari `Assets/_Project/Scenes/Bootstrap.unity`.

Riwayat: batch build M1 dan smoke dasar berhasil 10 Oktober menurut bukti sebelumnya. EXE itu belum berisi perubahan 14 Oktober. `TEST_REPORT.md` memisahkan hasil historis dan terbaru.
