# PROJECT ATLAS

- Baca `Assets/UNITY_3D_PROJECT_MUSEUM_MASTER_SPEC.md` serta Docs sebelum melanjutkan.
- Milestone aktif dan gate tercatat dalam `Docs/TASK_BOARD.md` dan `Docs/TEST_REPORT.md`.
- Pertahankan Unity 6000.3.23f1, URP, package existing dan SampleScene. Jangan meng-upgrade otomatis.
- Semua isi sample harus berlabel sintetis. Jangan mengarang isi Drive atau menyimpan kredensial dalam Assets.
- Gunakan Editor builder, bukan scene/prefab YAML buatan tangan. Jangan menimpa scene/prefab/material yang sudah diedit pengguna.
- Editor/package/import/Play/build belum diverifikasi hanya dengan Roslyn compiler check. Nyatakan bukti secara terpisah.
- Assembly mengarah Domain → Application → Infrastructure/Presentation → Runtime → Editor; jangan membuat dependency sirkular.
- Jangan commit Library, Temp, Logs, Builds, atau `.atlas-validation`.
