local Feature={}
function Feature.mount(ui)
    local Players=game:GetService("Players")
    local RunService=game:GetService("RunService")
    local player=Players.LocalPlayer
    local shared=ui.shared
    local config=require(shared.GameConfig)
    local archive=require(shared.DriveArchive)
    local quest=shared.Quest
    local presentation=shared.Presentation
    local state={tokens=0,completed={},owned={}}
    local talk,missions,play,shop,archiveFolder,podium,screen
    local currentView=""
    local activeEpoch=0
    local activeRound=""
    local presentationState
    local showSequence
    local sequenceVersion=0
    local C=ui.colors
    local lookup={};for _,node in ipairs(archive.items)do lookup[node.id]=node end
    local missionsById={};for _,m in ipairs(config.missions)do missionsById[m.id]=m end
    local function open(title,back)
        sequenceVersion+=1;ui.page(title,back);activeEpoch=ui.epoch()
    end
    local function input(placeholder,multiline,height)
        ui.nextOrder()
        local field=ui.make("TextBox",{LayoutOrder=ui.order(),Size=UDim2.new(1,-4,0,height or 54),BackgroundColor3=C.panel,TextColor3=C.white,PlaceholderColor3=C.muted,Text="",PlaceholderText=placeholder,
            TextSize=17,Font=Enum.Font.Gotham,ClearTextOnFocus=false,TextXAlignment=Enum.TextXAlignment.Left,TextYAlignment=Enum.TextYAlignment.Top,MultiLine=multiline or false,TextWrapped=true,ZIndex=8},ui.body)
        ui.make("UIPadding",{PaddingLeft=UDim.new(0,12),PaddingRight=UDim.new(0,12),PaddingTop=UDim.new(0,10)},field)
        ui.make("UICorner",{CornerRadius=UDim.new(0,10)},field)
        return field
    end
    local function navigate(zone)
        shared.Travel:FireServer(zone);ui.notify("Setelah tiba, dekati NPC dan tekan E untuk mulai.")
    end
    local tracker=ui.make("TextButton",{Name="QuestTracker",AnchorPoint=Vector2.new(1,0),Position=UDim2.new(1,-16,0,12),Size=UDim2.new(.42,0,0,78),BackgroundColor3=C.ink,TextColor3=C.gold,TextSize=14,TextWrapped=true,Font=Enum.Font.GothamMedium,ZIndex=3},ui.gui)
    ui.make("UISizeConstraint",{MaxSize=Vector2.new(370,78)},tracker);ui.make("UICorner",{CornerRadius=UDim.new(0,12)},tracker)
    tracker.Activated:Connect(function()if state.active and (state.active.status=="active" or state.active.status=="ready")then play()else missions()end end)
    local function progressText()
        local q=state.active
        if not q then return state.tokens.." Token Inspirasi\nKlik untuk misi NPC" end
        local m=missionsById[q.id]
        if q.status=="ready"then return state.tokens.." Token • SELESAI\nTemui "..m.npc.." untuk reward"end
        if q.status~="active"then return state.tokens.." Token Inspirasi\nMisi berikutnya menunggumu"end
        return m.title.."  "..q.progress.."/"..(q.id=="circuit" and 3 or 5).."\n"..math.max(0,math.ceil(q.deadline-workspace:GetServerTimeNow())).." detik • "..state.tokens.." Token"
    end
    local function cancel()
        if state.active then quest:FireServer("cancel",nil,state.active.token)end
    end
    missions=function()
        currentView="missions";open("Petualangan RISTEK",ui.menu)
        ui.label("Bantu teman-teman museum",C.teal,true)
        ui.label("Misi opsional, bisa diulang. Reward pertama 30–40 token; pengulangan 5 token. Token dan kosmetik berlaku selama sesi ini, bukan Robux.",C.muted)
        ui.label("Token Inspirasi: "..state.tokens,C.gold,true)
        if state.active and (state.active.status=="active" or state.active.status=="ready")then
            ui.button("Lanjutkan: "..missionsById[state.active.id].title,play,true)
            ui.button("Batalkan misi aktif",cancel)
        end
        for _,m in ipairs(config.missions)do
            ui.label((state.completed[m.id] and "✓ " or "")..m.title.." / "..m.npc,C.white,true)
            ui.label(m.description.."\nReward pertama: "..m.reward.." Token Inspirasi.",C.muted)
            ui.button("Pergi ke "..m.npc,function()navigate(m.zone)end,true)
        end
        ui.button("Lihat toko Lumi",shop)
    end
    talk=function(name)
        local npc;for _,n in ipairs(config.npcs)do if n.name==name then npc=n;break end end
        if not npc then return end
        currentView="npc:"..name;open(name.." / "..npc.role,missions)
        ui.label(npc.dialogue,C.white,true)
        if npc.mission then
            local m=missionsById[npc.mission];ui.label(m.description,C.muted)
            local q=state.active
            if q and q.id==npc.mission and q.status=="ready" then
                ui.button("Terima reward",function()quest:FireServer("claim",nil,q.token)end,true)
            elseif q and q.id==npc.mission and q.status=="active" then ui.button("Lanjutkan misi",play,true)
            else ui.button("Mulai misi • "..(state.completed[m.id] and config.replayReward or m.reward).." token",function()quest:FireServer("start",m.id)end,true)end
        elseif name=="Lumi" then ui.button("Tukar Token Inspirasi",shop,true)
        elseif name=="Dimas" then ui.button("Buka kontrol podium",podium,true);ui.button("Lihat presentasi",screen)
        else ui.button("Pilih minigame",missions,true);ui.button("Buka arsip Drive",function()archiveFolder(archive.rootId)end)end
    end
    play=function()
        local q=state.active;if not q then missions();return end
        currentView="play";activeRound=q.token..":"..q.round
        local m=missionsById[q.id];open(m.title,function()talk(m.npc)end)
        if q.status=="ready" then
            ui.label("Misi selesai!",C.teal,true);ui.label("Kembali ke "..m.npc.." untuk menerima reward.",C.muted)
            ui.button("Ambil reward di dekat NPC",function()quest:FireServer("claim",nil,q.token)end,true)
            ui.button("Pergi ke zona NPC",function()navigate(m.zone)end)
        elseif q.status=="failed" then
            ui.label("Belum berhasil — coba lagi!",C.gold,true)
            ui.button("Ulangi di dekat NPC",function()quest:FireServer("start",q.id)end,true)
        elseif q.status=="claimed" then
            ui.label("Reward sudah diterima",C.teal,true);ui.button("Pilih misi berikutnya",missions,true);ui.button("Lihat toko Lumi",shop)
        elseif q.id=="garden" then
            ui.label("Jelajahi taman dan cari lima benda bercahaya.",C.teal,true)
            ui.label("Terkumpul: "..q.progress.." / 5. Gunakan E atau ketuk prompt saat dekat benda. Setelah lengkap, kembali kepada Sari.",C.muted)
            ui.button("Lanjut berjalan",ui.closeModal,true)
        elseif q.id=="archive" then
            ui.label("Berkas "..(q.progress+1).." / 5",C.teal,true)
            ui.label(q.card,C.white,true)
            ui.label("Pilih folder induk RISTEK yang benar. Kesalahan: "..q.mistakes.." / 2 diperbolehkan.",C.muted)
            for _,category in ipairs({"PROJECT","Tech Insight","Mini Showcase","Tech Corner","Administrasi"})do
                ui.button(category,function()quest:FireServer("answer",category,q.token)end)
            end
        else
            ui.label("Rangkaian "..q.round.." / 3",C.teal,true)
            local hint=ui.label("Perhatikan pola lampu. Setelah selesai, tekan urutan yang sama.",C.muted)
            local choices={}
            local colors={Color3.fromRGB(80,220,199),Color3.fromRGB(242,185,85),Color3.fromRGB(94,166,228),Color3.fromRGB(236,133,115)}
            local names={"1 / TEAL","2 / EMAS","3 / BIRU","4 / CORAL"}
            local showing=false
            for i=1,4 do
                local b=ui.button(names[i],function()
                    if not showing and workspace:GetServerTimeNow()>=q.readyAt then quest:FireServer("answer",i,q.token)end
                end)
                b.TextColor3=colors[i];choices[i]=b
            end
            showSequence=function()
                if showing then return end
                showing=true;local version=sequenceVersion;hint.Text="Amati urutannya..."
                task.spawn(function()
                    for _,index in ipairs(q.sequence)do
                        if version~=sequenceVersion or not choices[index].Parent then showing=false;return end
                        choices[index].BackgroundColor3=colors[index];choices[index].TextColor3=C.ink
                        task.wait(.55)
                        if version~=sequenceVersion or not choices[index].Parent then showing=false;return end
                        choices[index].BackgroundColor3=C.soft;choices[index].TextColor3=colors[index]
                        task.wait(.25)
                    end
                    local remaining=q.readyAt-workspace:GetServerTimeNow();if remaining>0 then task.wait(remaining)end
                    if version==sequenceVersion and hint.Parent then hint.Text="Giliranmu! Masukkan urutan dari awal."end
                    showing=false
                end)
            end
            if q.step==1 then showSequence()else hint.Text="Lanjutkan urutan • langkah "..q.step end
        end
        if q.status=="active" then ui.button("Batalkan misi",cancel)end
    end
    shop=function()
        currentView="shop";open("Lumi / Toko Inspirasi",missions)
        ui.label("Saldo: "..state.tokens.." Token Inspirasi",C.gold,true)
        ui.label("Kosmetik gratis dari hasil bermain. Temui Lumi untuk menukar; kepemilikan berlaku selama sesi.",C.muted)
        for _,item in ipairs(config.shop)do
            ui.label(item.title.." / "..item.cost.." token",C.teal,true);ui.label(item.description)
            if state.owned[item.id]then ui.label("✓ Sudah aktif",C.gold)
            else ui.button("Tukar "..item.title,function()quest:FireServer("buy",item.id)end,true)end
        end
        ui.button("Kembali ke atrium / temui Lumi",function()navigate("Atrium")end)
    end
    archiveFolder=function(id)
        local folder=lookup[id];if not folder then return end
        currentView="archive";open("Arsip / "..folder.name,function()if folder.parentId then archiveFolder(folder.parentId)else ui.menu()end end)
        ui.label(folder.path,C.teal)
        ui.label("Snapshot Drive "..archive.fetchedAt..". Struktur berasal dari folder RISTEK. File tanpa preview tetap tercatat.",C.muted)
        local children={};for _,node in ipairs(archive.items)do if node.parentId==id then table.insert(children,node)end end
        table.sort(children,function(a,b)local ar=a.kind=="folder" and 0 or 1;local br=b.kind=="folder" and 0 or 1;if ar~=br then return ar<br end;return a.name:lower()<b.name:lower()end)
        if #children==0 then ui.label("Folder ini belum berisi item.",C.muted)end
        for _,node in ipairs(children)do
            ui.button((node.kind=="folder" and "[Folder] " or "[Berkas] ")..node.name,function()
                if node.kind=="folder" then archiveFolder(node.id);return end
                open(node.name,function()archiveFolder(id)end)
                ui.label(node.path,C.teal)
                ui.label("Jenis: "..(node.mimeType or node.kind).."\nDiubah: "..(node.modifiedAt or "Belum diketahui").."\nUkuran: "..(node.size and node.size.." byte" or "Belum diketahui"),C.muted)
                if node.previewText then ui.label(node.previewText)
                elseif node.kind=="shortcut"then ui.label("Shortcut tercatat; target di luar folder tidak diikuti otomatis.",C.gold)
                else ui.label("Preview belum tersedia",C.gold,true);ui.label("Metadata berkas sudah diambil dari Drive. Isi PDF/Office/gambar/ZIP/database belum ditanam ke dalam game.",C.muted)end
            end)
        end
    end
    local function showSlide(parent,slide,index,total)
        ui.label("Slide "..index.." / "..total,C.teal)
        if slide.image then
            ui.nextOrder()
            local image=ui.make("ImageLabel",{LayoutOrder=ui.order(),Size=UDim2.new(1,-4,0,360),Image=slide.image,ScaleType=Enum.ScaleType.Fit,BackgroundColor3=C.panel,ZIndex=8},parent)
            local status=ui.label("Memuat gambar Roblox...",C.muted)
            task.spawn(function()
                local start=os.clock()
                while image.Parent and not image.IsLoaded and os.clock()-start<12 do task.wait(.25)end
                if status.Parent then status.Text=image.IsLoaded and "" or "Gambar belum termuat. Periksa ID aset, moderasi, dan izin experience."end
            end)
        else ui.label(slide.title or "Presentasi",C.white,true);ui.label(slide.body or "")end
    end
    screen=function()
        currentView="screen";open("Layar presentasi",ui.menu)
        if not presentationState then ui.label("Mengambil presentasi...");presentation:FireServer("sync");return end
        local s=presentationState
        ui.label(s.presenterId==0 and "Podium tersedia" or "Presenter: "..s.presenterName,C.gold)
        showSlide(ui.body,s.slides[s.index],s.index,#s.slides)
        ui.label("Tampilan ini mengikuti slide presenter secara langsung.",C.muted)
        ui.button("Kontrol podium",podium)
    end
    podium=function()
        currentView="podium";open("Podium / Presentasi bersama",screen)
        if not presentationState then ui.label("Mengambil status podium...");presentation:FireServer("sync");return end
        local s=presentationState
        ui.label(s.presenterId==0 and "Podium tersedia. Setiap pemain boleh mengambil giliran." or "Presenter: "..s.presenterName,C.teal)
        if s.presenterId~=player.UserId then
            ui.button("Ambil giliran presenter",function()presentation:FireServer("claim")end,true)
            ui.button("Lihat layar besar",screen)
            ui.label("Berdirilah di dekat podium untuk mengambil giliran. Satu presenter mengendalikan slide untuk semua penonton.",C.muted)
            return
        end
        ui.button("< Slide sebelumnya",function()presentation:FireServer("prev")end)
        ui.button("Slide berikutnya >",function()presentation:FireServer("next")end,true)
        ui.label("Slide "..s.index.." / "..#s.slides.." • "..s.title,C.gold)
        ui.button("Tampilkan demo RISTEK",function()presentation:FireServer("demo")end)
        ui.label("Tautan Canva",C.white,true)
        local url=input("https://www.canva.com/design/.../view",false,70)
        ui.button("Tampilkan Canva",function()presentation:FireServer("canva",url.Text)end,true)
        ui.label("Canva ditampilkan sebagai slide gambar, bukan website tertanam. Tautan harus sudah dipetakan ke aset Roblox atau diproses bridge yang dikonfigurasi pengelola.",C.muted)
        ui.label("Slide gambar Roblox",C.white,true)
        local images=input("ID gambar 1, ID gambar 2, ...",true,90)
        ui.button("Tampilkan gambar",function()presentation:FireServer("images",images.Text)end)
        ui.label("Presentasi teks cepat",C.white,true)
        local text=input("Isi slide pertama\n---\nIsi slide kedua",true,160)
        ui.button("Tampilkan teks",function()presentation:FireServer("text",text.Text)end)
        ui.label("Teks difilter Roblox sebelum ditampilkan. Maksimal 8 slide teks / 30 slide gambar. Giliran berakhir setelah 2 menit tanpa kontrol atau saat meninggalkan podium.",C.muted)
        ui.button("Selesai / serahkan podium",function()presentation:FireServer("release")end)
    end
    quest.OnClientEvent:Connect(function(packet)
        if packet.kind~="state"then return end
        local previous=state.active
        state=packet
        if packet.message then ui.notify(packet.message)end
        local q=state.active
        for i=1,5 do
            local pickup=workspace.AtlasMuseum.Pickups:FindFirstChild("Recycle"..i)
            if pickup then for _,object in ipairs(pickup:GetDescendants())do
                local hidden=q and q.id=="garden" and q.collected[i] or false
                if object:IsA("BasePart")then object.LocalTransparencyModifier=hidden and 1 or 0 end
                if object:IsA("ProximityPrompt")then object.Enabled=not hidden end
            end end
        end
        if not ui.isOpen() or activeEpoch~=ui.epoch()then return end
        if q and q.status=="active" and (not previous or previous.token~=q.token) then play()
        elseif currentView=="play" then
            if not q or q.id~="circuit" or q.status~="active" or activeRound~=q.token..":"..q.round then play()end
        elseif currentView=="shop"then shop()
        elseif currentView:sub(1,4)=="npc:"then talk(currentView:sub(5))end
    end)
    presentation.OnClientEvent:Connect(function(packet)
        presentationState=packet.state
        if packet.message then ui.notify(packet.message)end
        if not ui.isOpen() or activeEpoch~=ui.epoch()then return end
        if currentView=="screen"then screen()
        elseif currentView=="podium" and not packet.message then podium()end
    end)
    -- Native local ambience: friendly NPC arm motion and two strolling visitors.
    local heads={};local visitors={}
    for _,npc in ipairs(workspace.AtlasMuseum.NPCs:GetChildren())do
        for _,part in ipairs(npc:GetChildren())do if part.Name=="Arm" then heads[#heads+1]={part=part,base=part.CFrame}end end
    end
    for _,visitor in ipairs(workspace.AtlasMuseum.Visitors:GetChildren())do visitors[#visitors+1]={model=visitor,base=visitor:GetPivot()}end
    local elapsed=0
    RunService.Heartbeat:Connect(function(dt)
        elapsed+=dt;if elapsed<.1 then return end;elapsed=0
        tracker.Text=progressText();tracker.Visible=not ui.isOpen()
        if ui.reduceMotion()then return end
        local time=os.clock()
        for i,head in ipairs(heads)do head.part.CFrame=head.base*CFrame.Angles(0,0,math.sin(time*.7+i)*.09)end
        for i,v in ipairs(visitors)do v.model:PivotTo(v.base*CFrame.new(math.sin(time*.2+i)*3,math.sin(time*3)*.08,math.cos(time*.2+i)*2)*CFrame.Angles(0,time*.2+i,0))end
    end)
    quest:FireServer("sync");presentation:FireServer("sync")
    return {Missions=missions,Talk=talk,Shop=shop,Archive=function()archiveFolder(archive.rootId)end,Podium=podium,Screen=screen,
        Leave=function()currentView="";sequenceVersion+=1 end,
        HandlePrompt=function(name)
            local npc=name:match("^NPC_(.+)$")
            if npc then talk(npc);return true end
            if name=="Missions"then missions();return true end
            if name=="AudienceView"then screen();return true end
            return name:match("^Collect_")~=nil
        end}
end
return Feature
