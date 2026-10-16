# PROJECT ATLAS — Museum Kepulauan Ide

Spesifikasi Roblox Studio: produk, dunia 3D, konten, arsitektur Luau, dan tahapan implementasi.

| Metadata | Keputusan |
|---|---|
| Versi | 1.0 — adaptasi Roblox, 16 Oktober 2026 |
| Dokumen sumber | `UNITY_3D_PROJECT_MUSEUM_MASTER_SPEC.md` |
| Engine / bahasa | Roblox Studio / Luau |
| Place tujuan | `Place1.rbxl` |
| Bahasa produk | Indonesia |
| Gaya | Museum kepulauan stylized; ivory, navy, teal, emas; sore yang terang |
| Perangkat | Desktop terlebih dahulu; kontrol dan UI dasar untuk touch/gamepad |
| Anggaran aset awal | 0 Robux; aset native Roblox dan geometri yang dirakit khusus |
| Konten awal | 12 proyek sintetis `sample-*`, diberi label CONTOH |
| Status | Implementasi awal disertakan; Play, render Studio, dan publikasi belum diverifikasi |

**Instruksi engine:** gunakan Roblox Studio. Larangan mengganti Unity pada dokumen sumber telah digantikan oleh permintaan pengguna untuk Roblox. Jangan menambahkan C#, URP, scene Unity, atau paket web sebagai runtime museum.

**Integritas konten:** isi folder Drive sumber belum diinventarisasi. Semua judul, hasil, dokumen, dan milestone pada fixture adalah contoh, bukan karya asli pengguna. Jangan memasukkan file privat atau kredensial ke place publik.

## 1. Keputusan inti

Museum menggabungkan katalog proyek, proses pengembangan, arsip, dan pameran dalam satu experience Roblox. Dunia memakai lima pulau yang terhubung secara fisik. Pengunjung boleh berjalan atau membuka katalog dari HUD.

| Konsep Unity | Adaptasi Roblox |
|---|---|
| Unity scenes + additive loader | Satu Place, model per zona di Workspace |
| C# MonoBehaviour | Script, LocalScript, ModuleScript Luau |
| Prefab | Model modular / package setelah kebutuhan nyata muncul |
| CharacterController first-person | Avatar, Humanoid, kamera dan kontrol Roblox |
| uGUI / TextMeshPro | ScreenGui, SurfaceGui, TextLabel, ScrollingFrame |
| Interaksi ray custom | ProximityPrompt dengan line of sight |
| StreamingAssets / JSON lokal | ModuleScript snapshot publik dari `catalog.json` |
| PlayerPrefs | State sesi; DataStore server pada milestone persisten |
| VideoPlayer MP4 lokal | VideoFrame dengan ID aset Roblox berizin |
| PDF / browser eksternal | Preview teks/gambar yang dikurasi; bukan browser umum |
| Build Windows offline | File place dapat dibuka di Studio; experience publik membutuhkan Roblox |
| Single player | Server Roblox bersama, UI/filter/bookmark per pemain |
| Fast travel scene | Server memindahkan Character ke tujuan allowlist |

Tidak menjanjikan client Roblox berjalan sepenuhnya offline. Demo tidak membutuhkan backend Drive, HTTP, DataStore, pembelian aset, atau plugin berbayar.

## 2. Produk dan pengguna

Pengunjung pameran membutuhkan ringkasan singkat dan karya yang mudah ditemukan. Reviewer membutuhkan konteks masalah, solusi, proses, dan bukti. Kurator membutuhkan data terpisah dari geometri agar isi dapat diganti tanpa membangun ulang semua ruang.

Tujuan: menemukan proyek lewat pencarian dalam 60 detik pada uji kegunaan; seluruh contoh dapat diakses tanpa misi; perubahan metadata dapat dilakukan di katalog; status contoh dan sumber terlihat jelas. Ini target, bukan hasil pengujian pengguna.

## 3. Cakupan

| Tahap | Cakupan | Gate |
|---|---|---|
| R0 — audit dan adaptasi | Backup, audit place, spec Roblox, sumber aset | Artefak asli tersimpan |
| R1 — museum awal | Lima zona, avatar native, stan, katalog, detail, dokumen teks, peta | Play di Studio, alur end-to-end tanpa error |
| R2 — media & kenyamanan | Preview gambar asli, video berizin, bookmark/settings persisten, audit mobile/gamepad | Izin aset, DataStore, kegagalan media diuji |
| R3 — katalog nyata | Inventaris Drive, kurasi, snapshot publik, konversi dokumen | Sumber dan izin setiap proyek diketahui |
| R4 — terhubung | Backend importer dan API server-only, publish atomik | Otorisasi dan revocation diuji |
| R5 — rilis | Profiling desktop/mobile, dua client, aksesibilitas, publikasi | Semua gate rilis lulus |

File saat ini menyediakan implementasi R1 untuk diuji. Belum berarti R1 lulus gate runtime atau seluruh MVP sumber Unity sudah selesai.

## 4. Kebutuhan dan acceptance criteria

| ID | Kebutuhan | Acceptance criterion |
|---|---|---|
| RB-01 | Place editabel | Lima model zona terlihat saat membuka file, sebelum Play |
| RB-02 | Menu | Play menampilkan menu dan tombol mulai |
| RB-03 | Kendali | Kontrol avatar standar bekerja, collider menghalangi dinding |
| RB-04 | Interaksi | E/touch/X membuka proyek sesuai slot; LOS aktif; jarak 12 studs |
| RB-05 | Katalog | Seluruh proyek terpublikasi dapat diakses tanpa berjalan |
| RB-06 | Pencarian | Judul, ringkasan, kategori, tag; case-insensitive, substring literal |
| RB-07 | Filter | Kategori AND tahun AND status AND bookmark |
| RB-08 | Halaman | Katalog 6 item/halaman; galeri 8; slot kosong tersembunyi |
| RB-09 | Isolasi pemain | Perubahan halaman galeri pemain A tidak mengubah pemain B |
| RB-10 | Detail | Masalah, solusi, fitur, hasil, tim, teknologi, sumber tersedia |
| RB-11 | Dokumen | Halaman teks contoh bisa maju/mundur dan kembali ke detail |
| RB-12 | Miniatur | ViewportFrame berputar opsional, koneksi animasi dilepas saat ditutup |
| RB-13 | Timeline | Diurutkan berdasarkan tanggal ISO; data kosong jelas |
| RB-14 | Arsip | Archived tetap dapat dibaca; berbeda dari withdrawn |
| RB-15 | Peta | Tujuan server tervalidasi, tidak menerima CFrame dari client |
| RB-16 | Modal | Kontrol berjalan dinonaktifkan, kembali aktif saat ditutup |
| RB-17 | Preferensi | FOV, teks besar, efek lembut, reduce motion berlaku pada sesi |
| RB-18 | Bookmark | Simpan/batal/filter selama sesi; UI tidak mengklaim permanen |
| RB-19 | Data nyata | Hanya public/published masuk snapshot client |
| RB-20 | Pemulihan | Karakter yang jatuh di bawah Y=-5 dikembalikan ke atrium |
| RB-21 | Biaya | Tidak ada pembelian, monetisasi, paid plugin, atau upload video otomatis |
| RB-22 | Bukti | Pisahkan hasil validasi file, uji Luau, Play, visual, dan profiling |

## 5. Alur pengalaman

Menu → mulai → atrium → galeri → prompt stan → detail → dokumen/model/timeline → kembali menjelajah. Alternatif: Menu/HUD → katalog → cari/filter → detail. Katalog tidak terkunci oleh progres.

Paspor mencatat proyek yang dibuka, bukan menyatakan pemain telah membaca seluruhnya. Tidak ada combat, stamina, loot, ekonomi, atau hadiah yang menghalangi dokumen. Auditorium menyediakan koleksi cerita dan kursi; playback video belum tersedia pada fixture.

## 6. Tata ruang

Satuan adalah studs. Y ke atas; utara = -Z. Permukaan lantai utama Y=4. Karakter dipindah pada Y=8 sehingga tidak ditanam ke dalam lantai. Ukuran tidak memakai konversi meter Unity secara buta.

| Model zona | Pusat X,Z | Lantai W × D | Identitas |
|---|---|---|---|
| Atrium | 0,0 | 66 × 66 | Rotunda terbuka, patung orbital, direktori, teal |
| Gallery | -106,0 | 100 × 72 | Paviliun kaca, 8 stan, biru |
| Archive | 106,0 | 76 × 72 | Rak warna-warni, meja baca, timeline, emas |
| Auditorium | 0,-106 | 80 × 72 | Layar besar, podium, 32 kursi, lavender |
| Garden | 0,106 | 78 × 72 | Kolam teratai, pergola, taman refleksi, coral |

Jembatan selebar 16 studs terhubung rata dengan lantai. Lorong utama bebas dari dekorasi. Spawn di selatan atrium menghadap pusat. Batas pulau diberi pagar; fitur kembali ke atrium tersedia melalui peta. Laut merupakan geometri dekoratif; bukan simulasi air Terrain.

## 7. Arah visual

Patung pusat terdiri dari inti teal dan tiga orbit emas/biru. Pilar putih membingkai langit tanpa menutup atrium. Galeri menggabungkan kaca biru tipis, tiang putih, lantai ivory, dan monolit navy. Arsip memakai rak padat dan struktur kayu. Forum lebih gelap untuk mengarahkan perhatian ke layar. Taman mengembalikan suasana hangat dengan pohon bulat, kolam, dan bangku.

Material utama: SmoothPlastic, Concrete, Marble, Wood, Metal, Glass, Grass, Neon. Neon hanya untuk garis arah, lingkaran, dan lampu; bloom lembut. Semua BasePart statis di-anchor. Geometri dan furnitur dapat dipilih serta diedit di Studio.

## 8. Aset gratis dan asal-usul

Aset terpasang memakai kemampuan native Roblox: Part/Sphere/Cylinder, material, font, sky template, avatar, Humanoid, kontrol, dan prompt. Bentuk bangunan/tanaman/furnitur disusun khusus untuk museum ini. Daftar ada di `docs/ASSET_LICENSES.md`.

Creator Store dapat digunakan pada tahap polish setelah akses unduhan tersedia. Kandidat terverifikasi: **Forest Pack oleh @Roblox**, ID `6432306802`. Kandidat ini belum diunduh atau ditanam ke place. Jangan menambahkan ID model ke runtime lalu menyatakan asetnya terpasang.

Untuk model gratis yang benar-benar diimpor: catat pembuat, URL, tanggal, biaya, dan izin; periksa descendant; hapus script, package link, remote, dan aset tidak relevan sebelum memasukkan dekorasi; pertahankan mesh hanya jika hak akses experience sesuai. Buat fallback native bila mesh tidak termuat.

## 9. Kontrol dan kamera

| Input | Fungsi |
|---|---|
| WASD / joystick / stik | Berjalan dengan kontrol Roblox |
| Mouse kanan / drag touch / stik kamera | Melihat |
| Scroll | Jarak kamera, termasuk mendekati first-person |
| E / ketuk prompt / ButtonX | Interaksi |
| K | Katalog |
| M | Peta |
| H | Bantuan |
| Backspace / ButtonB | Kembali pada panel |
| Tombol Tutup | Tutup modal dan lanjut bergerak |
| Esc | Menu Roblox, tidak diambil alih |

WalkSpeed awal 18. FOV awal 70, pengaturan 60–85. Kamera default Classic; zoom .5–28. Uji semua angka pada avatar dan perangkat nyata. Jangan mengikat Tab karena berbenturan dengan UI Roblox.

## 10. UI dan aksesibilitas

ScreenGui runtime berisi HUD, navigasi bawah, menu, katalog, detail, pembaca, peta, paspor, pengaturan, bantuan, kredit, dan toast. Panel memakai ukuran relatif dengan batas maksimum; konten satu kolom dapat di-scroll. Tombol 48 px. Teks besar menambah 3 px untuk body. Tidak ada head bob atau animasi kamera wajib.

State: Exploring ↔ Modal. Halaman dokumen kembali ke tab Dokumentasi; detail kembali ke katalog dengan filter/halaman yang sama. Posisi scroll katalog belum dipertahankan. Pencarian fixture berbahasa Indonesia memakai lowercase Luau; belum menjanjikan Unicode case-folding seluruh bahasa atau full-text PDF.

Keterbatasan yang wajib diuji: phone landscape, area aman HUD, keyboard fokus, gamepad selection, input modal, respawn, dan ukuran teks. Jangan mengklaim screen-reader compliance.

## 11. Dokumentasi dan media

| Jenis | Implementasi awal | Pengembangan berikutnya |
|---|---|---|
| Teks | Dokumen contoh dua halaman dengan paging | Dokumen terkurasi sesuai sumber |
| Model konsep | ViewportFrame geometri native | Miniatur proyek berizin |
| Gambar | Belum ada gambar proyek asli | ImageLabel + asset ID; fit, zoom, reset, error state |
| PDF / Office | Tidak ada parser runtime | Konversi teks atau halaman gambar di luar Roblox |
| Video | Pesan belum tersedia | VideoFrame dengan aset disetujui dan izin experience |
| ZIP / source | Tidak dieksekusi atau diunduh | Metadata dan ringkasan terkurasi |
| Drive | Belum terhubung | Importer eksternal, review, lalu snapshot |

Jangan memakai URL halaman Drive sebagai tekstur/video. Jangan menjanjikan membuka browser bebas atau mengunduh berkas ke perangkat dari LocalScript. Tidak ada unggahan video berbayar sebagai bagian build awal ini.

## 12. Model data

Sumber yang bisa diedit: `src/catalog.json`. Modul hasil: `src/Catalog.luau`. Data terpisah dari kode kontrol dan geometri. `make_catalog.py` adalah generator fixture; menjalankannya menimpa katalog contoh. Untuk konten nyata, edit JSON lalu gunakan `tools/sync_catalog.py`, bukan generator fixture.

Field proyek: `id`, `title`, `summary`, `category`, `year`, `developmentStatus`, `publicationState`, `visibility`, `isSample`, `sortOrder`, `color`, `problem`, `solution`, `outcomes`, `features`, `team`, `techStack`, `tags`, `updatedAt`, `documents`, `milestones`, `gallery`, `videos`.

Status pengembangan: planned / in_progress / completed / archived. Status publikasi dan visibility terpisah. Hanya published/public boleh berada di ReplicatedStorage. Fase connected mengembangkan DocumentVersion, sourceRef, checksum, relasi media, dan riwayat publikasi seperti konsep sumber Unity; jangan menyebut struktur fixture sekarang sebagai sistem versi lengkap.

Validasi: versi schema didukung, ID unik, kategori valid, judul/ringkasan wajib, tahun numerik, warna valid, dokumen/pages tidak kosong, tanggal ISO, tidak ada data privat. ID contoh berawalan `sample-`. Data hilang ditampilkan sebagai belum tersedia, bukan direkayasa.

## 13. Integrasi Drive

Folder sumber dari dokumen lama tetap menjadi kandidat sumber: `https://drive.google.com/drive/folders/1K0gYFMdzSQlvm8KyYz702XvYIg6pryh_`. Belum dipindai pada implementasi ini.

Alur yang dituju: inventaris rekursif beserta pagination → mapping folder/file ke proyek → klasifikasi draft → konversi preview → review hak publikasi → upload aset yang diperlukan → snapshot publik. File tak terklasifikasi tetap dicatat. Shortcut keluar root perlu review, bukan otomatis diikuti.

OAuth/service account hanya di backend eksternal. Roblox server meminta katalog dari endpoint terkontrol melalui HttpService. Tidak ada token Drive dalam Script, LocalScript, ModuleScript, atribut, atau source repository. Semua yang berada di ReplicatedStorage/Workspace harus dianggap terlihat oleh client.

## 14. Arsitektur

Client bertanggung jawab atas UI, pencarian snapshot publik, halaman galeri pribadi, viewer, dan preferensi sesi. Server memvalidasi tujuan travel dan memulihkan pemain jatuh. Katalog publik dibaca sebagai data; tidak ada `loadstring`, dynamic require berdasarkan ID, atau HTTP runtime.

Fase terhubung: backend importer → snapshot terkurasi → Roblox server → DTO publik untuk client. Konten terbatas tidak masuk snapshot publik. UI tersembunyi bukan pengamanan. Cache dan izin media harus mengikuti revocation di server.

## 15. Struktur place dan sumber

```text
Workspace
  AtlasMuseum
    Atrium / Gallery / Archive / Auditorium / Garden
    Landscape / Bridges
  SpawnLocation
ReplicatedStorage
  Atlas
    Catalog          ModuleScript snapshot
    CatalogService   ModuleScript query/validasi
    Travel           RemoteEvent
ServerScriptService
  AtlasServer
StarterPlayer / StarterPlayerScripts
  AtlasClient
PlayerGui            saat Play
  AtlasUI
```

File source berada di `src/`; tool build dan validasi di `tools/`; backup input di `backups/`; laporan di `docs/`. Backup tidak boleh diganti dengan hasil build. Builder menghasilkan ulang bagian museum dari template asli, sehingga perubahan manual pada hasil harus dicadangkan terlebih dahulu.

## 16. Lifecycle

Geometri sudah tersimpan di file, bukan baru muncul melalui runtime builder. Saat Play, client memvalidasi katalog, membuat UI, mengikat delapan stan, menampilkan menu. PlayerModule controls dinonaktifkan selama modal. Setelah ditutup, kontrol aktif kembali. Respawn memakai spawn atrium.

Pindah zona memakai allowlist server, cooldown satu detik, pemeriksaan Humanoid dan karakter aktif. Server tidak mempercayai posisi yang dikirim client. Event balasan menutup UI setelah travel diterima. Viewport render connection dilepas ketika berpindah panel.

Streaming belum diaktifkan oleh builder. Jika kelak diaktifkan, binding stan wajib menangani model stream-in/out dan meminta area tujuan sebelum memindahkan karakter. Jangan sekadar menyalakan StreamingEnabled tanpa menyesuaikan kode.

## 17. Kontrak modul

| Kontrak | Fungsi |
|---|---|
| `CatalogService.validate(catalog)` | Tolak schema/ID/kategori/publication invalid |
| `CatalogService.search(catalog, query)` | Filter AND, pencarian substring literal, sort stabil |
| `CatalogService.page(items,index,size)` | Paging satu-based, clamp, empty state |
| `CatalogService.find(catalog,id)` | Lookup proyek |
| `Travel:FireServer(zoneId)` | Hanya string pada allowlist lima zona |
| `Travel:FireClient(player,zoneId)` | Acknowledgement tujuan yang diterima |

API masa depan tetap dapat memakai `/v1/projects`, `/documents`, `/media`, dan `/publications`. Cursor harus terikat snapshot. Error dan snapshot gagal tidak boleh menghapus state valid terakhir.

## 18. Authoring

1. Cadangkan place sebelum mengubah layout di Studio.
2. Ubah `src/catalog.json` untuk metadata publik yang sudah dikurasi.
3. Jalankan `python tools/sync_catalog.py` untuk validasi dan ModuleScript.
4. Ubah source Luau atau generator jika diperlukan.
5. Jalankan `python tools/build_museum.py` hanya setelah perubahan Studio sudah diamankan.
6. Jalankan validator, buka place, lakukan Play test.
7. Untuk edit langsung tanpa rebuild, ubah ModuleScript Catalog dan script terkait di Studio, lalu Save.

Builder mengandalkan Python 3.10+ dan library Zstd Git for Windows untuk membaca template. Path library dapat diatur dengan `ATLAS_ZSTD_DLL`. Builder tidak memasang plugin atau paket baru dan tidak menghubungi Roblox.

## 19. Performa

Budget awal: <1.500 BasePart, geometri anchored, ≤8 stan aktif per client, satu ViewportFrame aktif, lima PointLight tanpa bayangan lokal. Transparansi kaca dan banyak SurfaceGui tetap harus diprofilkan.

Target provisional: desktop 60 FPS, mobile 30 FPS, menu cepat terbuka setelah karakter siap. Catat device, GPU, resolusi, quality, player count, frame-time, dan memori. Nilai ini belum diukur. Tur dua menit melalui semua zona, buka/tutup viewer 50 kali, dan uji dua client diperlukan sebelum rilis.

## 20. Keamanan dan pemulihan

Remote hanya menerima ID zona; argumen tipe lain/unknown ditolak. Cooldown membatasi spam. Client tidak mengatur gerakan pemain lain. State kunjungan tidak bernilai ekonomi. Tidak ada pembelian atau pengiriman pesan ke pengguna lain.

Sebelum model gratis ditanam, periksa script dan dependensi. Jangan menyertakan kredensial atau inventaris privat. Untuk DataStore nanti, lakukan validasi ukuran dan ID, retry terbatas, UpdateAsync, serta indikator gagal simpan. Jangan mengaktifkan API Services pada universe produksi untuk tes.

## 21. Roadmap

Prioritas berikutnya adalah memverifikasi R1 di Roblox Studio: loader binary, Luau, navigasi, ukuran ruang, LOS, UI, paging, dan respawn. Setelah itu lakukan polish visual berbasis screenshot nyata. Konten Drive, persistensi, gambar/video, serta publikasi mengikuti gate terpisah. Tidak ada fitur masa depan yang dianggap selesai karena dokumennya telah ditulis.

## 22. Aturan kerja

Pertahankan backup dan file sumber Unity. Perubahan pengguna di Studio tidak boleh ditimpa diam-diam oleh rebuild. Catat perubahan source dan laporan uji. Jangan mengubah engine lagi. Tidak memerlukan pekerjaan multi-agent. Satu integrator memiliki file binary final.

## 23. Test matrix / Definition of Done

| Uji | Gate |
|---|---|
| Header/chunk/parent/referent binary valid | Build |
| Source Luau tertanam sama dengan source disk | Build |
| Fixture public, ID unik, relasi kategori benar | Build |
| Lima zona dan 8 prompt stan tersimpan | Build |
| Place terbuka dan bisa disimpan kembali | Studio |
| Play menu → stan → detail → halaman dokumen → kembali | Runtime |
| Search literal `%`, kombinasi filter, kosong, 1/8/9/100 item | Logic / runtime |
| Peta 5 tujuan, spam/ID invalid, seated travel, jatuh, respawn | Runtime server |
| Dua pemain memakai halaman galeri berbeda | Multiplayer |
| Teks panjang, portrait/landscape, keyboard/gamepad | UI |
| 50 siklus viewer, 20 perjalanan, tur profil | Performance |

DoD rilis memerlukan seluruh gate relevan, konten/sumber jujur, lisensi tercatat, dan panduan menjalankan. Status pemeriksaan aktual ada di `docs/TEST_REPORT.md`.

## 24. Risiko dan handover

Loader Studio belum diverifikasi dalam sesi ini. File ditulis sebagai RBXL binary, validasi struktural bukan bukti render atau Play. Editor tersedia di mesin tetapi sesi otomasi tidak berhasil mengakses desktop interaktif. Bila Studio melaporkan masalah, gunakan backup asli; source dan builder tersedia untuk koreksi reproduktif.

Aset Creator Store belum diimpor; tidak ada klaim paket eksternal terpasang. Snapshot adalah contoh. Bookmark/settings belum persisten. Tidak ada video, viewer gambar asli, importer Drive, backend, atau publikasi online. Laporan harus mempertahankan batas ini sampai ada bukti baru.

## 25. Prompt lanjutan

```text
Baca ROBLOX_PROJECT_MUSEUM_MASTER_SPEC.md dan docs/TEST_REPORT.md.
Buka Place1.rbxl di Roblox Studio. Cadangkan perubahan manual terlebih dahulu.
Verifikasi R1 end-to-end, mulai loader/Play, lalu UI, prompt, 8 slot,
halaman kedua, dokumen, travel, respawn, dan dua client. Perbaiki masalah
yang terbukti. Catat screenshot asli Studio dan Output. Jangan mengklaim
Drive, video, persistensi, atau publikasi selesai tanpa implementasi dan uji.
Semua sample-* tetap dilabeli contoh sampai kurator mengganti datanya.
```

## 26. Referensi

- [Place files](https://create.roblox.com/docs/projects/place-files)
- [Toolbox dan Creator Store](https://create.roblox.com/docs/projects/assets/toolbox)
- [Proximity prompts](https://create.roblox.com/docs/ui/proximity-prompts)
- [Data stores](https://create.roblox.com/docs/cloud-services/data-stores)
- [Video frames](https://create.roblox.com/docs/ui/video-frames)
- [Forest Pack oleh Roblox — kandidat, belum diimpor](https://create.roblox.com/store/asset/6432306802)
- [Spesifikasi binary komunitas rbx-dom](https://dom.rojo.space/binary.html)

Referensi diakses 16 Oktober 2026. Ukuran, layout, warna, budget, dan roadmap adalah keputusan desain proyek ini.
