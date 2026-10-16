import json
from pathlib import Path
root=Path(__file__).resolve().parent.parent
entries=[
('Kota Cerdas','Teknologi','completed',[94,166,228],'Informasi kota tersebar di banyak tempat.','Dashboard contoh yang menggabungkan informasi ruang publik.',['Peta layanan','Panel informasi'],'Luau / Data visualization'),
('Kebun Masa Depan','Lingkungan','in_progress',[61,206,190],'Perawatan tanaman memerlukan pencatatan yang konsisten.','Simulasi pemantauan kebun dengan indikator visual.',['Catatan tanaman','Simulasi kelembapan'],'IoT / Prototyping'),
('Orbit Pengetahuan','Pendidikan','completed',[166,145,224],'Konsep ruang sulit dijelaskan melalui teks saja.','Model orbital interaktif untuk menjelaskan hubungan ide.',['Eksplorasi model','Panduan belajar'],'Roblox Studio / Luau'),
('Jaringan Kreatif','Desain','in_progress',[236,133,115],'Hubungan antaride sering hilang dalam catatan terpisah.','Visualisasi simpul dan koneksi untuk diskusi tim.',['Papan ide','Kelompok tema'],'Design thinking / UI'),
('Jejak Energi','Lingkungan','completed',[242,185,85],'Penggunaan energi sulit dipahami tanpa konteks visual.','Contoh panel perbandingan konsumsi energi.',['Grafik perbandingan','Catatan pengamatan'],'Data / Sustainability'),
('Ruang Inklusif','Desain','planned',[94,166,228],'Ruang presentasi perlu menyediakan beberapa cara mengakses informasi.','Eksperimen navigasi langsung dan katalog yang terbaca.',['Pencarian langsung','Teks besar'],'UX / Accessibility'),
('Laboratorium Cahaya','Teknologi','in_progress',[61,206,190],'Eksperimen pencahayaan sulit dibandingkan secara konsisten.','Ruang contoh untuk eksplorasi material dan suasana.',['Material native','Pengaturan visual'],'Lighting / Material'),
('Peta Cerita','Pendidikan','completed',[166,145,224],'Cerita proyek perlu urutan yang dapat ditelusuri.','Timeline naratif dari ide sampai refleksi.',['Milestone','Catatan proses'],'Storytelling / Education'),
('Arsip Gelombang','Teknologi','archived',[94,166,228],'Eksperimen lama mudah terlupakan.','Contoh arsip yang tetap dapat dicari setelah selesai.',['Metadata arsip','Riwayat proses'],'Archiving / Data'),
('Benih Komunitas','Lingkungan','planned',[61,206,190],'Ide komunitas membutuhkan ruang dokumentasi bersama.','Konsep ruang berbagi untuk ide lingkungan.',['Papan ide','Dokumentasi konsep'],'Community / Design'),
('Bentuk & Makna','Desain','archived',[236,133,115],'Keputusan bentuk perlu dijelaskan bersama prosesnya.','Eksplorasi komposisi geometris dan alasan desain.',['Studi bentuk','Refleksi desain'],'3D / Visual design'),
('Kelas Terbuka','Pendidikan','completed',[242,185,85],'Materi belajar perlu dapat ditemukan tanpa urutan wajib.','Katalog pembelajaran berbasis eksplorasi.',['Katalog topik','Eksplorasi mandiri'],'Education / Luau'),
]
projects=[]
for i,(title,category,status,color,problem,solution,features,stack) in enumerate(entries,1):
 projects.append({'id':f'sample-project-{i:03}','title':title,'category':category,'year':2026 if i<9 else 2025,'developmentStatus':status,'publicationState':'published','visibility':'public','isSample':True,'sortOrder':i,'color':color,'summary':f'Contoh sintetis — {solution}','problem':problem,'solution':solution,'outcomes':['Demonstrasi museum; bukan hasil atau capaian proyek asli pengguna.'],'features':features,'team':[{'displayName':'Tim Contoh','role':'Kurator demo'}],'techStack':stack.split(' / '),'tags':[category.lower(),'contoh','prototipe'],'updatedAt':'2026-10-16','documents':[{'id':f'sample-doc-{i:03}','title':'Catatan konsep — contoh','kind':'text','pages':[f'{title}\nCATATAN KONSEP SINTETIS\n\nMasalah\n{problem}\n\nUsulan pendekatan\n{solution}', 'REFLEKSI CONTOH\n\nDokumen ini dibuat untuk mendemonstrasikan pembaca halaman dalam museum. Tidak berasal dari Google Drive pengguna.\n\nLangkah berikutnya: ganti contoh ini dengan konten yang sudah ditinjau dan diizinkan untuk publikasi.']}],'milestones':[{'date':'2026-10-01','title':'Ide contoh','description':'Perumusan kebutuhan sintetis.'},{'date':'2026-10-10','title':'Eksperimen contoh','description':'Penyusunan prototipe demonstrasi.'},{'date':'2026-10-16','title':'Pameran contoh','description':'Penempatan fixture dalam museum.'}],'gallery':[],'videos':[]})
catalog={'schemaVersion':1,'isSample':True,'catalogVersion':'sample-2026-10-16-001','categories':['Teknologi','Lingkungan','Pendidikan','Desain'],'projects':projects}
raw=json.dumps(catalog,ensure_ascii=False,indent=2)
(root/'src/catalog.json').write_text(raw,encoding='utf-8')
(root/'src/Catalog.luau').write_text('-- Generated from catalog.json by tools/make_catalog.py. Public sample data only.\nreturn game:GetService("HttpService"):JSONDecode([====[\n'+raw+'\n]====])\n',encoding='utf-8')
print('Created 12 clearly labeled sample projects')
