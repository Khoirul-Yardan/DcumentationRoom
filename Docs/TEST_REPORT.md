# Laporan pengujian — status terbaru 14 Oktober 2026

## Kandidat MVP lokal: implementasi tersedia, gate Unity belum lulus

Perubahan 14 Oktober belum mempunyai build/Play tervalidasi. EXE dalam `Builds/Windows` masih artifact **10 Oktober**, sehingga tidak memuat fitur MVP terbaru. Bukti historis di bawah tidak berlaku sebagai validasi perubahan baru.

| Pemeriksaan sesi 14 Oktober | Hasil / bukti |
|---|---|
| Kompilasi API enam assembly (Roslyn terhadap DLL Unity/packages lokal) | PASS; `Evidence/compiler-check.txt`. Bukan import/compile Editor. |
| Domain, paging, search AND/OR, validator, beban 100 proyek/1.000 dokumen | 45 PASS; `Evidence/domain-checks.txt`. Rata-rata query metadata dicatat; bukan FPS. |
| Fixture, checksum, decode PNG, codec MP4, metadata, dependency assembly | 34 PASS; `Evidence/MVP/asset-checks.txt`. MP4 teridentifikasi H.264/AAC, belum diputar di Unity terbaru. |
| Generator fixture dijalankan ulang | PASS, manifest byte-identical dan tidak menggandakan ID; `Evidence/MVP/fixture-idempotency.txt`. |
| Batch Unity normal | Gagal sebelum menjalankan builder: Package Manager IPC tidak tersambung; `Logs/Atlas-MvpBuild.log`. |
| Batch Unity memakai cache `-noUpm` | Terhenti pada Licensing Client: connection refused / initialization timeout; `Logs/Atlas-MvpOffline.log`. Proses Editor percobaan dihentikan. |
| Perlindungan asset lama | Tidak ada penulisan scene/prefab/material oleh implementasi. Timestamp scene/prefab/material masih 10 Oktober atau sebelumnya; SampleScene dan manifest/lock tetap. Baseline hash awal berada di Temp dan terhapus saat startup Unity: perbandingan byte awal tidak dapat dibuktikan. |
| Editor idempotency dua kali, preferences JsonUtility, checksums dalam Editor | Runner `AtlasMvpBuild.Run` tersedia; **belum dieksekusi** karena startup gagal. |
| Smoke MVP, 50 siklus gambar, 20 putaran lima zona, video pause/seek/volume/cleanup | Runner `-atlasMvpSmoke` tersedia; **belum dieksekusi** pada build baru. |
| Profiling 120 detik, GPU/CPU, frame p95, peak working set | Runner tambahan `-atlasProfile` tersedia; **belum diukur**. |
| Visual 3D/UI terbaru, keterbacaan, WASD/mouse/collision/occlusion dan audio | **Belum diverifikasi**. Tidak ada screenshot baru yang diklaim sebagai hasil runtime MVP. |

Kesimpulan: source dan data telah diperluas untuk kandidat MVP lokal M2/M3. Jangan menandai MVP selesai sebelum build baru, smoke, interaksi manual, visual, dan profiling lolos. Akses Drive/backend tetap di luar rilis lokal ini. Langkah validasi ada di `BUILD_AND_RUN.md` dan `MVP_ACCEPTANCE.md`.

---

# Riwayat M0/M1 — 10 Oktober 2026

## Pemeriksaan yang benar-benar dijalankan

| Pemeriksaan | Hasil | Bukti |
|---|---|---|
| Audit Editor, package, URP, input, scene awal | Selesai | PROJECT_AUDIT.md |
| Kompilasi enam assembly dengan Roslyn dan DLL Unity/package terpasang | **PASS**, tanpa diagnostic compiler | Evidence/compiler-check.txt |
| Domain tests, dieksekusi lewat Mono bawaan Unity | **39 PASS** | Evidence/domain-checks.txt |
| Struktur fixture dan metadata | **PASS**: 3 signature/dimensi PNG, 32 GUID unik, .meta lengkap, asmdef JSON valid | Pemeriksaan Python standard library |
| Batch Editor workspace utama | Ditolak karena proyek sedang terbuka | Logs/Atlas-BatchProbe.log |
| Batch Editor salinan validasi | Gagal koneksi IPC Unity Package Manager | Logs/Atlas-Validation.log |
| Percobaan offline `-noUpm` dengan cache lokal | Terhambat timeout inisialisasi lisensi | Logs/Atlas-OfflineValidation.log |

Compiler check memakai Roslyn dari Unity 6000.3.23f1, netstandard 2.1, UnityEngine/UnityEditor DLL, serta Unity.InputSystem, Unity.TextMeshPro dan UnityEngine.UI yang sudah ada. Ini membuktikan source C# cocok dengan API assembly tersebut. **Ini bukan pengganti kompilasi/import di Editor, IL2CPP, PlayMode, atau build player.**

Domain tests menguji fixture valid/null, duplicate ID, FK rusak, schema unsupported, null array/record, draft/private, batas judul, sample label, path traversal, URL allowlist, normalisasi Unicode, filter AND, archived, paging 0/1/8/9/100, sort stabil, dan versi dokumen milik dokumen lain.

## Belum terverifikasi — gate M1 masih terbuka

- Import Unity penuh dan pembuatan scene/prefab melalui builder. Scene/prefab hasil builder belum tersedia sebagai artifact tervalidasi.
- Idempotensi builder dua kali dalam Editor, GUID/referensi asset hasilnya.
- Main Menu → Lobby → Gallery → Detail → Image → Back dalam Play/build.
- Mouse/WASD, collision, interaksi terhalang dinding, radius 2,5 m, cursor/focus.
- Tampilan TMP, layout panjang/Unicode, keterbacaan, material dan pencahayaan.
- Image decode/fallback dan cleanup saat dibuka/ditutup berulang.
- Singleton rig/EventSystem dan perpindahan zona berulang.
- Windows EXE, screenshot runtime, profiling FPS/memori.

Tidak ada klaim M1 selesai atau aplikasi sudah dapat dimainkan berdasarkan compiler check saja. Implementasi dan builder telah disiapkan; jalankan langkah BUILD_AND_RUN.md di Editor pengguna untuk menuntaskan gate.

Pembersihan salinan `.atlas-validation` dan penghentian proses uji ditolak oleh pemeriksaan izin otomatis (`sandbox_approval` tidak diizinkan). Folder sementara dibiarkan dan sudah dikecualikan dalam .gitignore. Tidak ada permintaan persetujuan yang dapat diajukan untuk kategori tersebut pada sesi ini.

`AtlasChecks.Run` menyediakan pemeriksaan tambahan JsonUtility/catalog aktual dalam Editor. `RuntimeSmoke` menyediakan rute otomatis opt-in untuk build (`-atlasSmoke`), screenshot, pemeriksaan texture, modal, singleton, dan kembali lobby. Keduanya belum dijalankan di Unity pada sesi ini.

## Lanjutan 10 Oktober 2026 (sesi Hermes agent)

### Bug ditemukan dan diperbaiki
1. `AtlasWorldBuilder.cs:155` memakai `LocalCatalogRepository` tanpa `using ProjectAtlas.Infrastructure;` → compiler error CS0246 saat Unity benar-benar compile di Editor (bukan hanya Roslyn standalone check). Ditambahkan using directive.
2. `AtlasBuilder.PrepareMuseumKit()` memanggil `EditorSceneManager.NewScene(additive)` langsung di atas scene untitled bawaan batchmode yang belum tersimpan → `InvalidOperationException: Cannot create a new scene additively with an untitled scene unsaved`. Diperbaiki: jika scene aktif masih untitled dan bersih, buat base scene Single dulu sebelum additive.

### Pemeriksaan yang benar-benar dijalankan dan PASS
| Pemeriksaan | Hasil | Bukti |
|---|---|---|
| `BatchBuild` penuh (compile → BuildStarterScenes → ValidateCatalog → data checks → BuildWindows) via `Unity.exe -batchmode -nographics -quit -executeMethod ProjectAtlas.Editor.AtlasBuilder.BatchBuild` | **SUKSES**, exit code 0, "Exiting batchmode successfully now!" | Logs/Atlas-Build4.log |
| Compile 6 assembly di Editor nyata (bukan Roslyn standalone) | **PASS** tanpa error | Logs/Atlas-Build4.log baris 286-298 |
| Catalog load + validate (`AtlasBuilder.ValidateCatalog`) | **PASS**: "ATLAS: catalog valid / 3 projects." | Logs/Atlas-Build4.log |
| Generate 7 scene (`BuildStarterScenes`) | **PASS**: Bootstrap 5 obj, Frontend 12 obj/6 renderer, Lobby 93 obj/61 renderer, Gallery 111 obj/69 renderer, Archive 103 obj/65 renderer, Showcase 135 obj/80 renderer, Courtyard 61 obj/37 renderer — 0 missing script di semua scene | Logs/Atlas-Build4.log; Assets/_Project/Scenes/*.unity |
| `AtlasChecks.Run` data checks | **20 PASS** | Logs/Atlas-Build4.log baris 740; Docs/Evidence/data-checks.txt |
| Build Windows x64 (`BuildWindows`) | **SUKSES**: `Builds/Windows/ProjectAtlas.exe` + `_Data` dihasilkan, exit code 0 | Logs/Atlas-Build4.log; Builds/Windows/ |
| Runtime smoke (`ProjectAtlas.exe -batchmode -atlasSmoke`, workdir project root) | **PASS** semua 4 assertion | Docs/Evidence/runtime-smoke.txt: "Image loaded: True / Modal restores detail and blocks movement: True / Single player/EventSystem: True / Return zone: Lobby" |

### Belum terverifikasi — masih terbuka
- **Visual nyata tidak terlihat.** Sesi agent ini headless/RDP tanpa GPU display aktif; `ScreenCapture.CaptureScreenshot` di `-nographics` gagal total ("Failed to capture screen shot."), dan pada run tanpa `-nographics` menghasilkan file PNG tapi isinya hitam polos (hanya sedikit teks TMP putih/cyan/magenta terbaca samar — UI label, bukan render 3D). Warna, material, pencahayaan, proporsi ruang, dan keterbacaan panel **belum terbukti sama sekali** dari sesi ini. Lihat `Docs/Evidence/0*.png`.
- Mouse/WASD interaktif, collision dinding, ray occlusion jarak 2,5 m — RuntimeSmoke memanggil `app.Navigate()` dan `app.OpenProject()` langsung lewat API, bukan lewat gerakan pemain sungguhan; gate FR-02/FR-03 belum diuji manual.
- Idempotensi builder dijalankan dua kali berturut — belum diuji eksplisit.
- Performa/profiling FPS dan memori — tidak dapat diukur tanpa GPU render aktif di sesi ini.

Tidak ada klaim M1 "selesai dan terlihat bagus". Pipeline build-and-data penuh terbukti berfungsi end-to-end dengan bukti log nyata. Kualitas visual dan interaksi manual wajib diverifikasi pengguna di mesin dengan GPU/display aktif sebelum menganggap M1 tuntas.
