# Keputusan arsitektur

1. **Roblox menggantikan Unity** atas instruksi pengguna. Dokumen Unity dipertahankan sebagai sumber; spec baru ditulis terpisah.
2. **Satu place, lima model zona.** Museum cukup kecil untuk disimpan penuh. Streaming dimatikan pada versi ini karena client mengikat stan tetap; mengaktifkannya memerlukan penanganan stream-in/out.
3. **Dunia tersimpan saat edit.** Geometri, prompt, signage, dan script benar-benar ditanam di binary; bukan sekadar paket script builder yang harus ditempel pengguna untuk melihat museum.
4. **Aset native gratis.** Kebutuhan awal dipenuhi tanpa unduhan eksternal, paid plugins, atau upload video. Forest Pack Roblox hanya kandidat.
5. **Client membaca snapshot public/published.** Data privat tidak boleh berada di ReplicatedStorage. Drive belum terhubung.
6. **UI dan galeri per pemain.** Paging dan visibility lokal; server hanya mengotorisasi travel. Pergantian galeri tidak mengubah isi pemain lain.
7. **Kontrol native.** Tidak mengganti PlayerModule atau membuat sistem gerak baru. Modal memakai Controls:Disable/Enable.
8. **Persistensi ditunda secara eksplisit.** Bookmark dan pengaturan sesi tidak disalahlabeli permanen. DataStore memerlukan milestone server dan pengujian tersendiri.
9. **Dokumen fixture berupa teks.** Tidak berpura-pura membaca PDF/Office/Drive langsung di Roblox. Viewport 3D menggantikan kebutuhan gambar contoh eksternal.
10. **Build loss-preserving dari backup tetap.** Opaque chunks lama dipertahankan. Builder tidak menggabungkan edit manual Studio pada hasil; panduan mewajibkan backup sebelum regenerate.
11. **Bukti terbatas.** Validasi binary dan JSON terpisah dari kompilasi Luau/Play. Preview teknis tidak dilabeli screenshot Studio.
