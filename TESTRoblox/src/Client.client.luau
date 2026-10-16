if not game:IsLoaded() then game.Loaded:Wait() end
local Players = game:GetService("Players")
local UIS = game:GetService("UserInputService")
local RunService = game:GetService("RunService")
local PPS = game:GetService("ProximityPromptService")
local GuiService = game:GetService("GuiService")
local Lighting = game:GetService("Lighting")
local player = Players.LocalPlayer
local shared = game:GetService("ReplicatedStorage"):WaitForChild("Atlas")
local catalog = require(shared:WaitForChild("Catalog"))
local CatalogService = require(shared:WaitForChild("CatalogService"))
CatalogService.validate(catalog)
local travel = shared:WaitForChild("Travel")
local museum = workspace:WaitForChild("AtlasMuseum")
local C = {ink=Color3.fromRGB(22,37,51), panel=Color3.fromRGB(32,49,65), soft=Color3.fromRGB(48,66,81),
    white=Color3.fromRGB(241,239,225), muted=Color3.fromRGB(180,198,201), teal=Color3.fromRGB(80,220,199), gold=Color3.fromRGB(245,193,104)}
local statusNames = {planned="Direncanakan", in_progress="Dikembangkan", completed="Selesai", archived="Arsip",documented="Terinventarisasi"}
local bookmarks, visited = {}, {}
local query = {text="", bookmarks=bookmarks}
local catalogPage, galleryPage = 1, 1
local reducedMotion, largeText, softEffects = false, false, true
local fov = 70
local modalOpen, currentBack, cleanup = false, nil, nil
local features
local pageEpoch=0
local controls
local function make(class, props, parent)
    local object = Instance.new(class)
    for key, value in pairs(props) do object[key] = value end
    object.Parent = parent
    return object
end
local function round(parent, radius)
    make("UICorner", {CornerRadius=UDim.new(0,radius or 10)}, parent)
end
local function stroke(parent, color)
    make("UIStroke", {Color=color or C.soft, Thickness=1}, parent)
end
local gui = make("ScreenGui", {Name="AtlasUI", ResetOnSpawn=false, ZIndexBehavior=Enum.ZIndexBehavior.Sibling, DisplayOrder=20}, player:WaitForChild("PlayerGui"))
local hud = make("Frame", {Name="HUD", BackgroundTransparency=1, Size=UDim2.fromScale(1,1)}, gui)
local brand = make("TextLabel", {Text="A T L A S   /   MUSEUM IDE", Font=Enum.Font.GothamBold, TextSize=18,
    TextColor3=C.white, TextScaled=true, BackgroundColor3=C.ink, BackgroundTransparency=.08, Size=UDim2.new(.5,-16,0,44), Position=UDim2.fromOffset(16,12)}, hud)
make("UISizeConstraint",{MaxSize=Vector2.new(330,44)},brand)
make("UITextSizeConstraint",{MinTextSize=10,MaxTextSize=18},brand)
round(brand)
local zoneLabel = make("TextLabel", {Text="ATRIUM  /  RISTEK", Font=Enum.Font.Gotham, TextSize=14, TextScaled=true, TextColor3=C.muted,
    BackgroundColor3=C.ink, BackgroundTransparency=.08, Size=UDim2.new(.5,-16,0,30), Position=UDim2.fromOffset(16,60)}, hud)
make("UISizeConstraint",{MaxSize=Vector2.new(330,30)},zoneLabel)
make("UITextSizeConstraint",{MinTextSize=9,MaxTextSize=14},zoneLabel)
round(zoneLabel)
local navigation = make("Frame", {AnchorPoint=Vector2.new(.5,1), Position=UDim2.new(.5,0,1,-18), Size=UDim2.new(.94,0,0,52), BackgroundColor3=C.ink}, hud)
make("UISizeConstraint", {MaxSize=Vector2.new(650,52)}, navigation)
round(navigation,14)
make("UIListLayout", {FillDirection=Enum.FillDirection.Horizontal, HorizontalAlignment=Enum.HorizontalAlignment.Center, VerticalAlignment=Enum.VerticalAlignment.Center, Padding=UDim.new(0,4)}, navigation)
local veil = make("Frame", {Name="ModalVeil", Size=UDim2.fromScale(1,1), BackgroundColor3=Color3.new(0,0,0), BackgroundTransparency=.4, Visible=false, ZIndex=5, Active=true}, gui)
local panel = make("Frame", {AnchorPoint=Vector2.new(.5,.5), Position=UDim2.fromScale(.5,.48), Size=UDim2.fromScale(.92,.85), BackgroundColor3=C.ink, ZIndex=6}, veil)
make("UISizeConstraint", {MaxSize=Vector2.new(1020,760)}, panel)
round(panel,18);stroke(panel,C.soft)
local heading = make("TextLabel", {Size=UDim2.new(1,-130,0,62), Position=UDim2.fromOffset(22,0), BackgroundTransparency=1, Text="ATLAS", TextColor3=C.white, TextXAlignment=Enum.TextXAlignment.Left, Font=Enum.Font.GothamBold, TextSize=23, TextTruncate=Enum.TextTruncate.AtEnd, ZIndex=7},panel)
local close = make("TextButton", {Size=UDim2.fromOffset(92,40), Position=UDim2.new(1,-108,0,11), BackgroundColor3=C.soft, Text="Tutup  X", TextSize=15, Font=Enum.Font.GothamMedium, TextColor3=C.white, Modal=true, ZIndex=7},panel)
round(close)
local body = make("ScrollingFrame", {Name="Content", Position=UDim2.fromOffset(18,68), Size=UDim2.new(1,-36,1,-86), BackgroundTransparency=1, BorderSizePixel=0, ScrollBarThickness=5, ScrollBarImageColor3=C.teal, CanvasSize=UDim2.new(), AutomaticCanvasSize=Enum.AutomaticSize.Y, ScrollingDirection=Enum.ScrollingDirection.Y, ZIndex=7},panel)
make("UIPadding", {PaddingLeft=UDim.new(0,3),PaddingRight=UDim.new(0,12),PaddingBottom=UDim.new(0,12)},body)
make("UIListLayout", {Padding=UDim.new(0,12),SortOrder=Enum.SortOrder.LayoutOrder},body)
local toast = make("TextLabel", {AnchorPoint=Vector2.new(.5,1), Position=UDim2.new(.5,0,1,-83), Size=UDim2.new(.85,0,0,48), BackgroundColor3=C.teal, TextColor3=C.ink, Font=Enum.Font.GothamMedium, TextSize=15, TextWrapped=true, Visible=false, ZIndex=30},gui)
make("UISizeConstraint", {MaxSize=Vector2.new(650,60)},toast);round(toast)
local toastId=0
local function notify(text)
    toastId+=1;local id=toastId;toast.Text=text;toast.Visible=true
    task.delay(4,function() if toastId==id then toast.Visible=false end end)
end
local order=0
local function label(text, color, title, parent)
    order+=1
    return make("TextLabel", {LayoutOrder=order, Size=UDim2.new(1,-4,0,0), AutomaticSize=Enum.AutomaticSize.Y, BackgroundTransparency=1,
        Text=text, TextColor3=color or C.white, Font=title and Enum.Font.GothamBold or Enum.Font.Gotham,
        TextSize=(title and 23 or 17)+(largeText and 3 or 0), TextWrapped=true, TextXAlignment=Enum.TextXAlignment.Left, TextYAlignment=Enum.TextYAlignment.Top, ZIndex=8},parent or body)
end
local function button(text, callback, accent, parent)
    order+=1
    local b=make("TextButton", {LayoutOrder=order, Size=UDim2.new(1,-4,0,48), BackgroundColor3=accent and C.teal or C.soft, TextColor3=accent and C.ink or C.white,
        Text=text, TextSize=largeText and 19 or 16, Font=Enum.Font.GothamMedium, TextWrapped=true, AutoButtonColor=true, ZIndex=8},parent or body)
    round(b);b.Activated:Connect(callback)
    return b
end
local function clearContent()
    if cleanup then cleanup();cleanup=nil end
    for _, child in ipairs(body:GetChildren()) do
        if child:IsA("GuiObject") then child:Destroy() end
    end
    body.CanvasPosition=Vector2.zero;order=0
end
local function setModal(value)
    modalOpen=value;veil.Visible=value;navigation.Visible=not value
    if controls then if value then controls:Disable() else controls:Enable() end end
    UIS.MouseIconEnabled=true
    if not value then GuiService.SelectedObject=nil end
end
local function closeModal()
    pageEpoch+=1;clearContent();currentBack=nil;setModal(false)
end
local function page(title, back)
    pageEpoch+=1;clearContent();heading.Text=title;currentBack=back;setModal(true)
    if back then button("<  Kembali",back) end
    if UIS.GamepadEnabled then GuiService.SelectedObject=close end
end
close.Activated:Connect(closeModal)
task.spawn(function()
    local module=player:WaitForChild("PlayerScripts"):WaitForChild("PlayerModule")
    controls=require(module):GetControls()
    if modalOpen then controls:Disable() end
end)
player.CharacterAdded:Connect(function()
    task.defer(function() if controls and modalOpen then controls:Disable() end end)
end)

local showMenu,showCatalog,showDetail,showMap,showSettings,showHelp,showCredits,showPassport
local function recordVisit(project)
    if not visited[project.id] then visited[project.id]=true end
end
local function showDocument(project, document, index)
    index=math.clamp(index,1,#document.pages)
    page(document.title,function() showDetail(project,"Dokumentasi") end)
    label((document.previewKind=="metadata" and "METADATA DRIVE" or "PREVIEW TEKS DRIVE").."  /  Halaman "..index.." dari "..#document.pages,C.gold)
    label(document.pages[index],C.white)
    if index>1 then button("<  Halaman sebelumnya",function()showDocument(project,document,index-1)end) end
    if index<#document.pages then button("Halaman berikutnya  >",function()showDocument(project,document,index+1)end,true) end
    label("Sumber: "..(project.sourceLabel or "Katalog")..". Preview dibedakan dari isi file asli.",C.muted)
end
local function showMiniature(project)
    label("MODEL KONSEP 3D  /  Geometri demonstrasi",C.teal)
    order+=1
    local viewport=make("ViewportFrame", {LayoutOrder=order,Size=UDim2.new(1,-4,0,260), BackgroundColor3=C.panel, Ambient=Color3.fromRGB(190,190,190), LightColor=Color3.new(1,1,1), ZIndex=8},body)
    round(viewport)
    local wm=make("WorldModel",{},viewport)
    local camera=make("Camera",{FieldOfView=45},viewport);viewport.CurrentCamera=camera
    local color=Color3.fromRGB(table.unpack(project.color))
    for i=1,5 do
        local h=1.5+i*.7
        make("Part",{Name="Concept",Size=Vector3.new(1.3,h,1.3),CFrame=CFrame.new((i-3)*1.8,h/2,0)*CFrame.Angles(0,i*.4,0),Color=color,Material=Enum.Material.SmoothPlastic,Anchored=true},wm)
    end
    make("Part",{Size=Vector3.new(12,.4,7),Position=Vector3.new(0,-.2,0),Color=C.ink,Anchored=true},wm)
    local angle=0
    local function draw() camera.CFrame=CFrame.lookAt(Vector3.new(math.sin(angle)*17,10,math.cos(angle)*17),Vector3.new(0,2,0)) end
    draw()
    local conn=RunService.RenderStepped:Connect(function(dt) if not reducedMotion then angle+=dt*.22;draw() end end)
    cleanup=function()conn:Disconnect()end
    button("Putar model 45 derajat",function()angle+=math.pi/4;draw()end)
    label("Galeri foto asli belum tersedia. Model ini hanya ilustrasi konsep; bukan hasil proyek pengguna.",C.muted)
end
showDetail=function(project,tab)
    tab=tab or "Ringkasan";recordVisit(project)
    page(project.title,function()showCatalog()end)
    label((project.isSample and "CONTOH SINTETIS" or "KOLEKSI DRIVE").."  /  "..project.category.."  /  Tahun: "..tostring(project.year or "belum diketahui").."  /  "..statusNames[project.developmentStatus],C.gold)
    label(project.summary,C.muted)
    button((bookmarks[project.id] and "★  Tersimpan" or "☆  Simpan bookmark").." — sesi ini",function()
        bookmarks[project.id]=not bookmarks[project.id] or nil;showDetail(project,tab)
    end)
    local tabs={"Ringkasan","Dokumentasi","Model 3D","Timeline","Teknologi","Demo"}
    -- A single selector keeps all tabs reachable on a narrow mobile display.
    button("Bagian: "..tab.."   /   Ganti bagian >",function()
        local idx=table.find(tabs,tab) or 1;showDetail(project,tabs[idx%#tabs+1])
    end,true)
    if tab=="Ringkasan" then
        label("Masalah",nil,true);label(project.problem)
        label("Solusi",nil,true);label(project.solution)
        label("Fitur",nil,true);label("• "..table.concat(project.features,"\n• "))
        local team={}
        for _,member in ipairs(project.team)do table.insert(team,member.displayName.." — "..member.role)end
        label("Hasil & tim",nil,true);label(table.concat(project.outcomes,"\n").."\n"..table.concat(team,"\n"))
    elseif tab=="Dokumentasi" then
        label("Dokumentasi",nil,true)
        for _,doc in ipairs(project.documents) do button(doc.title.."  /  "..#doc.pages.." halaman",function()showDocument(project,doc,1)end) end
        if #project.documents==0 then label("Belum tersedia.",C.muted) end
        label("PDF, Office, dan Drive memerlukan preview yang disiapkan serta ditinjau terlebih dahulu.",C.muted)
    elseif tab=="Model 3D" then showMiniature(project)
    elseif tab=="Timeline" then
        local milestones=table.clone(project.milestones);table.sort(milestones,function(a,b)return a.date<b.date end)
        for _,m in ipairs(milestones) do label(m.date.."  /  "..m.title,C.teal,true);label(m.description) end
        if #milestones==0 then label("Belum ada milestone yang terdokumentasi.",C.muted) end
    elseif tab=="Teknologi" then label(#project.techStack>0 and table.concat(project.techStack,"\n") or "Belum didokumentasikan pada metadata yang dibaca.",C.teal,true)
    elseif tab=="Demo" then label("Video belum tersedia",nil,true);label("VideoFrame Roblox memerlukan aset video yang dapat diakses experience ini.",C.muted) end
    label("Diperbarui: "..project.updatedAt.."  /  Sumber: "..(project.isSample and "fixture museum" or (project.sourceLabel or "katalog terkurasi")),C.muted)
end
showCatalog=function(preset)
    if preset=="archive" and features then features.Archive();return end
    page("Katalog proyek",showMenu)
    label("Koleksi dan metadata berasal dari folder Drive RISTEK. Preview yang belum dikonversi ditandai secara jelas.",C.muted)
    order+=1
    local search=make("TextBox",{LayoutOrder=order,Size=UDim2.new(1,-4,0,50),BackgroundColor3=C.panel,TextColor3=C.white,PlaceholderColor3=C.muted,PlaceholderText="Cari judul, ringkasan, kategori, atau tag...",Text=query.text,TextSize=17,Font=Enum.Font.Gotham,ClearTextOnFocus=false,TextXAlignment=Enum.TextXAlignment.Left,ZIndex=8},body)
    make("UIPadding",{PaddingLeft=UDim.new(0,14),PaddingRight=UDim.new(0,14)},search);round(search);stroke(search)
    local refresh
    local categories={"Semua kategori"};for _,c in ipairs(catalog.categories)do table.insert(categories,c)end
    local cb;cb=button("Kategori: "..(query.category or "Semua"),function()
        local index=query.category and table.find(categories,query.category) or 1
        local nextValue=categories[index%#categories+1];query.category=nextValue~="Semua kategori" and nextValue or nil
        cb.Text="Kategori: "..(query.category or "Semua");catalogPage=1;refresh()
    end)
    local statuses={"all"};local years={};local seenStatuses,seenYears={},{}
    for _,project in ipairs(catalog.projects)do
        if not seenStatuses[project.developmentStatus]then seenStatuses[project.developmentStatus]=true;table.insert(statuses,project.developmentStatus)end
        if project.year and not seenYears[project.year]then seenYears[project.year]=true;table.insert(years,project.year)end
    end
    table.sort(years,function(a,b)return a>b end)
    local sb;sb=button("Status: "..(statusNames[query.status] or "Semua"),function()
        local index=table.find(statuses,query.status or "all") or 1;local nextValue=statuses[index%#statuses+1]
        query.status=nextValue~="all" and nextValue or nil;sb.Text="Status: "..(statusNames[query.status] or "Semua");catalogPage=1;refresh()
    end)
    local yb;yb=button("Tahun: "..(query.year or "Semua"),function()
        local index=query.year and table.find(years,query.year) or 0
        query.year=years[(index or 0)+1]
        yb.Text="Tahun: "..(query.year or "Semua");catalogPage=1;refresh()
    end)
    if #years==0 then yb.Text="Tahun belum tercantum pada metadata Drive";yb.Active=false;yb.AutoButtonColor=false end
    local bb;bb=button(query.onlyBookmarks and "★ Hanya bookmark sesi" or "☆ Semua proyek",function()
        query.onlyBookmarks=not query.onlyBookmarks;bb.Text=query.onlyBookmarks and "★ Hanya bookmark sesi" or "☆ Semua proyek";catalogPage=1;refresh()
    end)
    button("Reset pencarian & filter",function()query={text="",bookmarks=bookmarks};catalogPage=1;showCatalog()end)
    local count=label("",C.teal)
    order+=1
    local list=make("Frame",{LayoutOrder=order,Size=UDim2.new(1,0,0,0),AutomaticSize=Enum.AutomaticSize.Y,BackgroundTransparency=1,ZIndex=8},body)
    make("UIListLayout",{Padding=UDim.new(0,10),SortOrder=Enum.SortOrder.LayoutOrder},list)
    refresh=function()
        for _,child in ipairs(list:GetChildren())do if child:IsA("GuiObject")then child:Destroy()end end
        local results=CatalogService.search(catalog,query)
        local items,index,pages=CatalogService.page(results,catalogPage,6);catalogPage=index
        count.Text=#results.." proyek  /  Halaman "..index.." dari "..pages
        if #items==0 then label("Tidak ada proyek yang cocok. Coba reset filter.",C.muted,false,list) end
        for _,project in ipairs(items)do
            local b=button(project.title.."   →\n"..project.category.." / DRIVE RISTEK",function()showDetail(project)end,false,list)
            b.Size=UDim2.new(1,-4,0,74);stroke(b,Color3.fromRGB(table.unpack(project.color)))
        end
        if index>1 then button("< Sebelumnya",function()catalogPage-=1;refresh()end,false,list)end
        if index<pages then button("Berikutnya >",function()catalogPage+=1;refresh()end,false,list)end
    end
    search:GetPropertyChangedSignal("Text"):Connect(function()query.text=search.Text;catalogPage=1;refresh()end)
    refresh()
end
local zoneInfo={
    {id="Atrium",title="00  /  ATRIUM",description="Patung orbital, direktori & pusat museum.",pos=Vector3.new(0,4,0)},
    {id="Gallery",title="01  /  GALERI INOVASI",description="Koleksi Drive dan minigame pola listrik bersama Bima.",pos=Vector3.new(-106,4,0)},
    {id="Archive",title="02  /  PERPUSTAKAAN PROSES",description="Struktur Drive RISTEK dan misi Kurator Kilat bersama Tika.",pos=Vector3.new(106,4,0)},
    {id="Auditorium",title="03  /  FORUM",description="Ruang presentasi dengan 32 kursi.",pos=Vector3.new(0,4,-106)},
    {id="Garden",title="04  /  TAMAN IDE",description="Kolam teratai, pergola & tempat beristirahat.",pos=Vector3.new(0,4,106)},
}
showMap=function()
    page("Peta kepulauan",showMenu)
    label("                    FORUM\n                       |\nGALERI — ATRIUM — ARSIP\n                       |\n                 TAMAN IDE",C.teal,true)
    label("Pilih zona untuk perjalanan cepat. Seluruh jembatan juga dapat dilalui dengan berjalan.",C.muted)
    for _,zone in ipairs(zoneInfo)do
        button(zone.title,function()travel:FireServer(zone.id)end,true)
        label(zone.description,C.muted)
    end
end
travel.OnClientEvent:Connect(function(zone)
    closeModal();notify("Tiba di "..zone..". Selamat menjelajah!")
end)
showSettings=function()
    page("Pengaturan",showMenu)
    label("Pengaturan dan bookmark berlaku selama sesi ini.",C.gold)
    button("Sudut pandang: "..fov.."°  /  Ganti",function()
        fov=fov>=85 and 60 or fov+5;workspace.CurrentCamera.FieldOfView=fov;showSettings()
    end)
    button("Gerakan model: "..(reducedMotion and "Dikurangi" or "Aktif"),function()reducedMotion=not reducedMotion;showSettings()end)
    button("Ukuran teks: "..(largeText and "Besar" or "Standar"),function()largeText=not largeText;showSettings()end)
    button("Efek lembut: "..(softEffects and "Aktif" or "Mati"),function()
        softEffects=not softEffects
        for _,effect in ipairs(Lighting:GetChildren())do if effect:IsA("BloomEffect") or effect:IsA("SunRaysEffect")then effect.Enabled=softEffects end end
        showSettings()
    end)
    button("Reset pengaturan",function()
        reducedMotion=false;largeText=false;softEffects=true;fov=70;workspace.CurrentCamera.FieldOfView=fov
        for _,effect in ipairs(Lighting:GetChildren())do if effect:IsA("BloomEffect") or effect:IsA("SunRaysEffect")then effect.Enabled=true end end
        showSettings()
    end)
    label("Volume, sensitivitas kamera, dan kualitas grafis umum tersedia di menu pengaturan Roblox. Museum ini belum menyertakan audio atau video eksternal.",C.muted)
end
showHelp=function()
    page("Cara menjelajah",showMenu)
    label("Bergerak dengan kontrol Roblox",C.teal,true)
    label("Desktop: WASD untuk berjalan, mouse kanan untuk melihat, scroll untuk jarak kamera. E berinteraksi dengan stan.\n\nLayar sentuh: joystick dan geser kamera, lalu ketuk prompt.\n\nGamepad: stik untuk berjalan/melihat, tombol X pada prompt, navigasi UI dengan fokus tombol.")
    label("Pintasan",C.teal,true)
    label("K = katalog  /  M = peta  /  H = bantuan\nBackspace atau tombol B gamepad = kembali satu tingkat.\nTombol Tutup mengembalikan kendali karakter. Esc tetap membuka menu Roblox.")
    label("Tidak ada misi wajib. Semua proyek tersedia lewat katalog. Gunakan Peta > Atrium jika ingin kembali ke pusat museum.",C.muted)
end
showCredits=function()
    page("Tentang ATLAS",showMenu)
    label("MUSEUM KEPULAUAN IDE",C.teal,true)
    label("Sebuah ruang untuk karya, proses, dan refleksi. Lima pulau menyatukan eksplorasi 3D dengan akses langsung ke dokumentasi.")
    label("Aset gratis",C.gold,true)
    label("Part, material, sky, font Gotham, avatar, kontrol, dan ProximityPrompt bawaan Roblox. Arsitektur, furnitur, tanaman, patung, dan miniatur dirakit khusus dari geometri native. Tidak ada model Creator Store pihak ketiga yang diimpor.")
    label("Status konten",C.gold,true)
    label("Arsip mengikuti snapshot folder Drive RISTEK: 16 folder dan 28 berkas/shortcut. File tanpa preview hanya menampilkan metadata. NPC dan cerita minigame adalah karakter fiktif. Presentasi auditorium dikendalikan bersama melalui podium.")
end
showPassport=function()
    page("Paspor penjelajah",showMenu)
    local n=0;for _ in pairs(visited)do n+=1 end
    label(n.." / "..#catalog.projects.." cerita telah dibuka",C.teal,true)
    label("Koleksi kunjungan opsional selama sesi ini. Semua informasi tetap dapat dibuka kapan saja.",C.muted)
    for _,project in ipairs(catalog.projects)do
        button((visited[project.id] and "✓  " or "○  ")..project.title,function()showDetail(project)end)
    end
end
showMenu=function()
    page("ATLAS / Museum Kepulauan Ide")
    label("Setiap karya punya cerita.",C.white,true)
    label("Jelajahi lima pulau pengetahuan. Baca proses di balik karya, temukan hubungan antaride, dan simpan inspirasi.",C.muted)
    label("RISTEK / 3 MINIGAME / 6 NPC / ARSIP DRIVE",C.gold)
    button("Main minigame dulu  →",function()features.Missions()end,true)
    button("Mulai menjelajah  →",closeModal,true)
    button("Buka katalog proyek",function()showCatalog()end)
    button("Arsip folder Drive",function()features.Archive()end)
    button("Lihat presentasi auditorium",function()features.Screen()end)
    button("Peta & perjalanan cepat",showMap)
    button("Paspor penjelajah",showPassport)
    button("Pengaturan",showSettings)
    button("Cara bermain",showHelp)
    button("Tentang & kredit aset",showCredits)
end
for _,item in ipairs({{"Misi",function()features.Missions()end},{"Katalog",function()showCatalog()end},{"Peta",showMap},{"Arsip",function()features.Archive()end},{"Menu",showMenu}})do
    local b=make("TextButton",{Size=UDim2.new(.185,0,0,42),BackgroundColor3=C.panel,Text=item[1],TextColor3=C.white,TextSize=15,Font=Enum.Font.GothamMedium},navigation)
    round(b);b.Activated:Connect(item[2])
end
-- Gallery binding is local: changing pages never changes another player's exhibit.
local bindings={}
local originalCollisions={}
local function bindGallery(index)
    local items,actual,pages=CatalogService.page(CatalogService.search(catalog,{}),index,8)
    galleryPage=actual
    for slot=1,8 do
        local stand=museum.Gallery:FindFirstChild(string.format("Stand%02d",slot))
        local project=items[slot];bindings[slot]=project
        if stand then
            for _,object in ipairs(stand:GetDescendants())do
                if object:IsA("BasePart")then
                    if originalCollisions[object]==nil then originalCollisions[object]=object.CanCollide end
                    object.LocalTransparencyModifier=project and 0 or 1
                    object.CanCollide=project~=nil and originalCollisions[object]
                end
                if object:IsA("SurfaceGui")then object.Enabled=project~=nil end
                if object:IsA("ProximityPrompt")then object.Enabled=project~=nil;if project then object.ObjectText=project.title end end
            end
            if project then
                local color=Color3.fromRGB(table.unpack(project.color))
                stand.Title.Sign.Text.Text=string.format("%02d / %s\nDRIVE RISTEK",(actual-1)*8+slot,project.title)
                stand.Title.Sign.Text.TextColor3=color;stand.Category.Sign.Text.Text=string.upper(project.category)
                stand.Halo.Color=color
            end
        end
    end
    return pages
end
bindGallery(1)
PPS.PromptTriggered:Connect(function(prompt,who)
    if who and who~=player then return end
    if modalOpen or not prompt:IsDescendantOf(museum) then return end
    if features and features.HandlePrompt(prompt.Name)then return end
    local slot=tonumber(prompt.Name:match("^Project_(%d+)$"))
    if slot and bindings[slot] then showDetail(bindings[slot]);return end
    if prompt.Name=="Catalog" then showCatalog()
    elseif prompt.Name=="Archive" then features.Archive()
    elseif prompt.Name=="Showcase" then
        features.Podium()
    elseif prompt.Name=="Credits" then showCredits()
    elseif prompt.Name=="PageNext" or prompt.Name=="PagePrev" then
        local pages=math.max(1,math.ceil(#catalog.projects/8))
        local direction=prompt.Name=="PageNext" and 1 or -1
        bindGallery((galleryPage-1+direction)%pages+1);notify("Galeri — halaman "..galleryPage.." / "..pages)
    end
end)
UIS.InputBegan:Connect(function(input,processed)
    if processed or UIS:GetFocusedTextBox() then return end
    if input.KeyCode==Enum.KeyCode.K then showCatalog()
    elseif input.KeyCode==Enum.KeyCode.M then showMap()
    elseif input.KeyCode==Enum.KeyCode.H then showHelp()
    elseif modalOpen and (input.KeyCode==Enum.KeyCode.Backspace or input.KeyCode==Enum.KeyCode.ButtonB) then
        if currentBack then currentBack() else closeModal() end
    end
end)
local elapsed=0
RunService.Heartbeat:Connect(function(dt)
    elapsed+=dt;if elapsed<.5 then return end;elapsed=0
    local root=player.Character and player.Character:FindFirstChild("HumanoidRootPart")
    if not root then return end
    local nearest,dist=zoneInfo[1],math.huge
    for _,zone in ipairs(zoneInfo)do local d=(root.Position-zone.pos).Magnitude;if d<dist then dist=d;nearest=zone end end
    zoneLabel.Text=nearest.title.."  /  RISTEK"
end)
workspace.CurrentCamera.FieldOfView=fov
features=require(shared:WaitForChild("FeatureUI")).mount({shared=shared,gui=gui,body=body,colors=C,make=make,label=label,button=button,page=page,closeModal=closeModal,notify=notify,menu=showMenu,
    nextOrder=function()order+=1 end,order=function()return order end,isOpen=function()return modalOpen end,epoch=function()return pageEpoch end,reduceMotion=function()return reducedMotion end})
showMenu()
print("[ATLAS] Client ready; catalog, detail, reader, map and session bookmarks initialized.")
