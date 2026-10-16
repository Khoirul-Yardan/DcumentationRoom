local Players=game:GetService("Players")
local Http=game:GetService("HttpService")
local TextService=game:GetService("TextService")
local shared=game:GetService("ReplicatedStorage"):WaitForChild("Atlas")
local remote=shared:WaitForChild("Presentation")
local config=require(script.Parent.PresentationConfig)
local registeredDecks=require(script.Parent.CanvaDecks)
local Rules=require(script.Parent.PresentationRules)
local podium=Vector3.new(24,8,-119)
local current={presenterId=0,presenterName="",expiresAt=0,index=1,title="RISTEK / FORUM",slides={
    {title="Panggung untuk semua ide",body="Dekati podium, ambil giliran presenter, lalu pilih demo, slide teks, gambar Roblox, atau tautan Canva yang sudah dipetakan."},
    {title="Presentasi yang terbaca",body="Slide berikutnya dan sebelumnya sinkron untuk semua pemain. Gunakan tombol Layar penuh untuk membaca dari tempat duduk."},
    {title="Canva → layar museum",body="Input tautan tersedia di podium. Desain Canva perlu diekspor ke gambar Roblox melalui pemetaan deck atau bridge sebelum bisa ditampilkan."},
}}
local defaultSlides=table.clone(current.slides)
local rate,away={},{}
local generation=0
local surfaces={}
local function now()return workspace:GetServerTimeNow()end
local function near(player,limit)
    local c=player.Character;local root=c and c:FindFirstChild("HumanoidRootPart");local h=c and c:FindFirstChildOfClass("Humanoid")
    return root and h and h.Health>0 and (root.Position-podium).Magnitude<=limit
end
local function create(class,props,parent)
    local obj=Instance.new(class);for k,v in pairs(props)do obj[k]=v end;obj.Parent=parent;return obj
end
local auditorium=workspace:WaitForChild("AtlasMuseum"):WaitForChild("Auditorium")
for _,name in ipairs({"ForumFront","AudienceScreen"})do
    local screen=auditorium:WaitForChild(name)
    local sign=screen:FindFirstChild("Sign");if sign then sign.Enabled=false end
    local ui=create("SurfaceGui",{Name="LivePresentation",Face=Enum.NormalId.Back,CanvasSize=Vector2.new(1600,900),LightInfluence=0},screen)
    local frame=create("Frame",{Size=UDim2.fromScale(1,1),BackgroundColor3=Color3.fromRGB(22,37,51)},ui)
    local image=create("ImageLabel",{Name="SlideImage",Size=UDim2.fromScale(1,1),BackgroundTransparency=1,ScaleType=Enum.ScaleType.Fit,Visible=false},frame)
    local title=create("TextLabel",{Name="SlideTitle",Position=UDim2.fromScale(.05,.1),Size=UDim2.fromScale(.9,.2),TextColor3=Color3.fromRGB(80,220,199),BackgroundTransparency=1,Font=Enum.Font.GothamBold,TextSize=68,TextWrapped=true},frame)
    local body=create("TextLabel",{Name="SlideBody",Position=UDim2.fromScale(.08,.34),Size=UDim2.fromScale(.84,.48),TextColor3=Color3.fromRGB(241,239,225),BackgroundTransparency=1,Font=Enum.Font.Gotham,TextSize=44,TextWrapped=true},frame)
    local footer=create("TextLabel",{Position=UDim2.fromScale(.04,.91),Size=UDim2.fromScale(.92,.07),TextColor3=Color3.fromRGB(180,198,201),BackgroundColor3=Color3.fromRGB(22,37,51),Font=Enum.Font.Gotham,TextSize=25},frame)
    surfaces[#surfaces+1]={image=image,title=title,body=body,footer=footer}
end
local function render()
    local slide=current.slides[current.index]
    for _,s in ipairs(surfaces)do
        s.image.Visible=slide.image~=nil;s.image.Image=slide.image or ""
        s.title.Visible=not slide.image;s.body.Visible=not slide.image
        s.title.Text=slide.title or current.title;s.body.Text=slide.body or ""
        s.footer.Text=current.index.." / "..#current.slides.."   •   "..(current.presenterId==0 and "Podium tersedia" or "Presenter: "..current.presenterName).."   •   Gunakan Layar penuh untuk membaca"
    end
end
local function send(player,message)
    local packet={kind="state",state=current,message=message}
    if player then remote:FireClient(player,packet)else remote:FireAllClients(packet)end
end
local function release()
    generation+=1;current.presenterId=0;current.presenterName="";current.expiresAt=0
    render();send()
end
local function stillOwns(player,ticket)return current.presenterId==player.UserId and generation==ticket and now()<current.expiresAt and near(player,24)end
local function setSlides(player,slides,title,ticket)
    if not stillOwns(player,ticket)then return end
    current.slides=slides;current.title=title;current.index=1;render();send()
end
remote.OnServerEvent:Connect(function(player,action,input)
    if type(action)~="string" then return end
    local clock=now();if clock-(rate[player] or -math.huge)<.2 then return end;rate[player]=clock
    if action=="sync" then send(player);return end
    if current.presenterId~=0 and clock>current.expiresAt then release()end
    if action=="claim" then
        if not near(player,18)then send(player,"Dekati podium terlebih dahulu.");return end
        if current.presenterId~=0 and current.presenterId~=player.UserId then send(player,"Podium sedang dipakai "..current.presenterName..". Tunggu giliran berikutnya.");return end
        current.presenterId=player.UserId;current.presenterName=player.DisplayName;current.expiresAt=clock+config.LeaseSeconds;away[player]=clock
        render();send();return
    end
    if current.presenterId~=player.UserId or not near(player,24) then send(player,"Kontrol presentasi hanya tersedia untuk presenter di dekat podium.");return end
    current.expiresAt=clock+config.LeaseSeconds;away[player]=clock
    if action=="release" then release()
    elseif action=="next" or action=="prev" then current.index=math.clamp(current.index+(action=="next" and 1 or -1),1,#current.slides);render();send()
    elseif action=="demo" then generation+=1;setSlides(player,table.clone(defaultSlides),"Demo RISTEK",generation)
    elseif action=="images" then
        if type(input)~="string" or #input>1500 then send(player,"Daftar ID gambar terlalu panjang.");return end
        local ids={};for id in input:gmatch("[^,%s]+")do table.insert(ids,id)end
        local slides=Rules.images(ids,config.MaxSlides)
        if not slides then send(player,"Masukkan 1–30 ID gambar Roblox, dipisahkan koma atau baris baru.");return end
        generation+=1;setSlides(player,slides,"Slide gambar",generation)
    elseif action=="canva" then
        local designId=Rules.designId(input)
        if not designId then send(player,"Gunakan tautan https://www.canva.com/design/... yang valid.");return end
        generation+=1;local ticket=generation
        local slides=Rules.images(registeredDecks[designId] or config.Decks[designId],config.MaxSlides)
        if slides then setSlides(player,slides,"Presentasi Canva",ticket);return end
        if config.BridgeUrl=="" then send(player,"Tautan Canva valid, tetapi slide belum dipetakan. Ekspor PNG, unggah ke Roblox, lalu gunakan ID gambar atau daftarkan deck. Bridge otomatis belum dikonfigurasi.");return end
        send(player,"Memuat slide Canva...")
        local ok,response=pcall(function()
            return Http:RequestAsync({Url=config.BridgeUrl,Method="POST",Headers={['Content-Type']='application/json',['x-atlas-token']=Http:GetSecret(config.BridgeSecretName)},Body=Http:JSONEncode({designId=designId})})
        end)
        if not stillOwns(player,ticket)then return end
        if not ok or not response.Success or #response.Body>50000 then send(player,"Bridge Canva belum dapat memuat slide. Presentasi sebelumnya tetap ditampilkan.");return end
        local decoded,data=pcall(function()return Http:JSONDecode(response.Body)end)
        slides=decoded and type(data)=="table" and Rules.images(data.images,config.MaxSlides)
        if not slides then send(player,"Deck belum siap atau respons bridge tidak valid.");return end
        setSlides(player,slides,"Presentasi Canva",ticket)
    elseif action=="text" then
        if type(input)~="string" or #input<1 or #input>6000 then send(player,"Isi 1–6000 karakter. Pisahkan slide dengan baris ---.");return end
        local chunks={};local normalized=input:gsub("\r","").."\n---\n"
        for text in normalized:gmatch("(.-)\n%-%-%-\n")do if text:match("%S")then table.insert(chunks,text)end end
        if #chunks==0 or #chunks>8 then send(player,"Gunakan maksimal delapan slide teks.");return end
        for _,chunk in ipairs(chunks)do if #chunk>1000 then send(player,"Satu slide maksimal 1000 karakter agar terbaca.");return end end
        generation+=1;local ticket=generation
        local ok,slides=pcall(function()
            local result={}
            for i,text in ipairs(chunks)do
                local filtered=TextService:FilterStringAsync(text,player.UserId):GetNonChatStringForBroadcastAsync()
                result[i]={title="Presentasi / "..i,body=filtered}
            end
            return result
        end)
        if not stillOwns(player,ticket)then return end
        if not ok then send(player,"Filter teks Roblox belum tersedia. Coba lagi atau gunakan demo/gambar.");return end
        setSlides(player,slides,"Presentasi teks",ticket)
    end
end)
Players.PlayerRemoving:Connect(function(player)
    rate[player]=nil;away[player]=nil;if current.presenterId==player.UserId then release()end
end)
task.spawn(function()
    while true do
        task.wait(1)
        if current.presenterId~=0 then
            local p=Players:GetPlayerByUserId(current.presenterId)
            if p and near(p,24)then away[p]=now()end
            if not p or now()>current.expiresAt or now()-(away[p] or 0)>10 then release()end
        end
    end
end)
render()
