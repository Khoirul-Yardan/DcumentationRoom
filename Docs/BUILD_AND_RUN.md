# Menjalankan PROJECT ATLAS

Gunakan Unity **6000.3.23f1** dan proyek `D:\DcumentationRoom`.

**Status 14 Oktober:** 7 scene sudah ada. Source kandidat MVP telah ditambahkan, tetapi build terbaru belum diverifikasi karena Editor agent gagal koneksi Package Manager/Licensing. EXE existing masih build 10 Oktober. Buka proyek melalui Unity Hub berlisensi untuk melakukan import dan validasi terbaru.

## Menjalankan kandidat MVP terbaru

1. Buka `Assets/_Project/Scenes/Bootstrap.unity` setelah kompilasi selesai. Save terlebih dahulu jika ada scene pengguna yang belum disimpan.
2. Play: katalog memiliki 9 proyek sintetis, filter kategori/tahun/status, bookmark, pengaturan, dan tab detail. Contoh Atlas Catatan memiliki dokumen dua halaman, timeline dan video.
3. Gunakan **Tools > Project Atlas > Build MVP Offline Package** untuk builder idempotency, data/preferences checks, validasi scene dan build Windows. Builder menjaga scene/prefab/material existing.
4. Uji build baru secara interaktif, lalu jalankan smoke dari root proyek:

```powershell
$atlasSmoke = Start-Process -FilePath '.\Builds\Windows\ProjectAtlas.exe' -ArgumentList '-batchmode -atlasMvpSmoke -atlasProfile -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile D:/DcumentationRoom/Logs/Atlas-MvpPlayer.log' -WorkingDirectory 'D:\DcumentationRoom' -WindowStyle Hidden -PassThru -Wait
$atlasSmoke.ExitCode
```

Jangan memakai `-nographics` untuk smoke video/screenshot/profiling. Output terbaru masuk `Docs/Evidence/MVP`; gunakan `MVP_ACCEPTANCE.md` untuk checklist manual. Smoke perlu build dari kode terbaru; EXE lama tidak mengenal argumen tersebut.

Pengaturan/bookmark memakai `Application.persistentDataPath/atlas-preferences-v1.json`; smoke memakai subfolder `MvpSmoke`. Semua data disimpan lokal, tanpa kredensial atau koneksi Drive. Fitur pending dan batas bukti dijelaskan di `TEST_REPORT.md`.

`python Tools/prepare_mvp.py` menambahkan fixture sintetis secara idempotent (membutuhkan Pillow, ffmpeg dengan libx264/AAC); tidak diperlukan untuk Play karena media sudah disertakan. `python Tools/check_mvp_assets.py` memeriksa fixture dan dependency, membutuhkan Pillow/ffprobe. Jangan menjalankan generator terhadap katalog produksi; generator menolak katalog yang bukan sample.

## Alur dasar

1. Tunggu Unity mengimpor file dan menyelesaikan kompilasi.
2. Pilih **Tools > Project Atlas > Build Starter Scenes**. Jika TMP Essentials baru diimpor dan builder meminta diulang, tunggu import selesai lalu jalankan lagi.
3. Buka `Assets/_Project/Scenes/Bootstrap.unity`, lalu **Play**.
4. Pilih **Jelajahi museum**. Gunakan WASD/mouse, Shift untuk cepat, dan E pada papan di sisi kiri lobby untuk masuk galeri.
5. Dekati salah satu stan contoh, tekan E, baca detail, buka gambar. Scroll untuk zoom, tarik untuk pan, Reset gambar untuk kembali. Esc menutup panel teratas. Galeri memuat delapan stan sekaligus; halaman kedua berisi proyek kesembilan.
6. Tab membuka katalog dari mode eksplorasi. Menu utama juga menyediakan akses katalog langsung tanpa berjalan.

Scene SampleScene bawaan tidak memuat game Atlas. Selalu mulai dari Bootstrap. Save scene Anda sebelum mengganti scene secara manual.

## Build Windows

Pilih **Tools > Project Atlas > Build Windows**. Output: `Builds/Windows/ProjectAtlas.exe`. Distribusikan seluruh folder Windows beserta `_Data`, UnityPlayer.dll, dan file pendukung; jangan hanya EXE.

Build awal menggunakan Development untuk diagnosa M1. Gunakan modul Windows Build Support dari Editor yang sama. Tidak ada package tambahan yang dibutuhkan.

Builder tidak menimpa scene/prefab/material Atlas yang sudah ada, dan tidak menghapus scene bawaan. Bootstrap diletakkan pertama dalam build settings, disusul zona Atlas; entri lain dipertahankan.

## Konten dan validasi

- Edit `Assets/StreamingAssets/Catalog/catalog.json`. Semua fixture harus tetap jelas berlabel contoh.
- Media berada di `Assets/StreamingAssets/Media`. Penambahan proyek dilakukan lewat manifest, tanpa mengedit scene/script.
- Jalankan **Tools > Project Atlas > Validate Catalog**, lalu **Run Data Checks**.
- `python Tools/prepare_atlas.py` membuat fixture hanya jika katalog belum ada, menyiapkan asmdef dan .meta yang belum ada. Tidak diperlukan saat menjalankan game.
- Source URL/media asli Google Drive belum terhubung pada milestone ini.

## Validasi otomatis

Dengan proyek tertutup di Editor, command batch:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\DcumentationRoom' -executeMethod ProjectAtlas.Editor.AtlasBuilder.BatchBuild -logFile 'D:\DcumentationRoom\Logs\Atlas-Build.log'
```

`BatchBuild` menjalankan builder dua kali, data checks, dan Windows build. Smoke route standalone bersifat opt-in melalui argumen `-atlasSmoke`, menghasilkan screenshot dan `runtime-smoke.txt` di `Docs/Evidence` relatif working directory. Ini tidak menggantikan uji manual mouse/WASD, collision, readability, dan performa.
