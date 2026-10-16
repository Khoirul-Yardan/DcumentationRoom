local Players=game:GetService("Players")
local Http=game:GetService("HttpService")
local PPS=game:GetService("ProximityPromptService")
local shared=game:GetService("ReplicatedStorage"):WaitForChild("Atlas")
local config=require(shared.GameConfig)
local Engine=require(script.Parent.QuestEngine)
local remote=shared:WaitForChild("Quest")
local world=workspace:WaitForChild("AtlasMuseum")
local rng=Random.new()
local states={}
local function now()return workspace:GetServerTimeNow()end
local missions={};for _,m in ipairs(config.missions)do missions[m.id]=m end
local function near(player,pos,distance)
    local c=player.Character;local h=c and c:FindFirstChildOfClass("Humanoid");local root=c and c:FindFirstChild("HumanoidRootPart")
    return h and h.Health>0 and root and (root.Position-pos).Magnitude<=distance
end
local function send(player,message)
    local s=states[player];if not s then return end
    remote:FireClient(player,{kind="state",tokens=s.tokens,completed=s.completed,owned=s.owned,active=Engine.public(s.active),message=message})
end
local function cosmetics(player)
    local s=states[player];local c=player.Character;if not s or not c then return end
    local root=c:FindFirstChild("HumanoidRootPart");if not root then return end
    if s.owned.trail and not root:FindFirstChild("AtlasTrail") then
        local a=Instance.new("Attachment");a.Name="TrailTop";a.Position=Vector3.new(0,1,0);a.Parent=root
        local b=Instance.new("Attachment");b.Name="TrailBottom";b.Position=Vector3.new(0,-1,0);b.Parent=root
        local t=Instance.new("Trail");t.Name="AtlasTrail";t.Attachment0=a;t.Attachment1=b;t.Lifetime=.6;t.Color=ColorSequence.new(Color3.fromRGB(61,206,190));t.Transparency=NumberSequence.new(.2,1);t.Parent=root
    end
    if s.owned.aura and not c:FindFirstChild("AtlasAura") then
        local h=Instance.new("Highlight");h.Name="AtlasAura";h.FillTransparency=1;h.OutlineColor=Color3.fromRGB(242,185,85);h.DepthMode=Enum.HighlightDepthMode.Occluded;h.Parent=c
    end
end
local function setup(player)
    if states[player] then return end
    states[player]={tokens=0,completed={},owned={},active=nil,last=-math.huge,lastFinish=-math.huge}
    local stats=Instance.new("Folder");stats.Name="leaderstats";stats.Parent=player
    local tokens=Instance.new("IntValue");tokens.Name="Inspirasi";tokens.Parent=stats
    player.CharacterAdded:Connect(function(character)
        character:WaitForChild("HumanoidRootPart",10);cosmetics(player)
    end)
end
Players.PlayerAdded:Connect(setup);for _,p in ipairs(Players:GetPlayers())do setup(p)end
Players.PlayerRemoving:Connect(function(player)states[player]=nil end)
remote.OnServerEvent:Connect(function(player,action,value,token)
    local s=states[player];if not s or type(action)~="string" then return end
    local clock=now();if clock-s.last<.12 then return end;s.last=clock
    if action=="sync" then send(player);return end
    if action=="start" then
        local m=type(value)=="string" and missions[value]
        if not m or not near(player,m.position,18) then send(player,"Dekati NPC misi terlebih dahulu.");return end
        if s.active and (s.active.status=="active" or s.active.status=="ready") then send(player,"Selesaikan atau batalkan misi aktif terlebih dahulu.");return end
        if clock-s.lastFinish<config.replayCooldown then send(player,"Istirahat sebentar. Misi berikutnya tersedia dalam "..math.ceil(config.replayCooldown-(clock-s.lastFinish)).." detik.");return end
        s.active=Engine.start(value,clock,rng,Http:GenerateGUID(false));send(player,"Misi dimulai!")
    elseif action=="answer" then
        local q=s.active;if not q or token~=q.token then return end
        if not near(player,missions[q.id].position,24) then send(player,"Kembali ke dekat NPC untuk melanjutkan.");return end
        local result=Engine.answer(q,value,clock,rng)
        send(player,result=="retry" and "Kategori belum tepat. Coba lagi." or result=="wrong" and "Misi belum berhasil. Kamu bisa mencoba lagi." or result=="ready" and "Selesai! Ambil reward dari NPC." or nil)
    elseif action=="claim" then
        local q=s.active;if not q or token~=q.token or not near(player,missions[q.id].position,18) then return end
        if Engine.claim(q) then
            local amount=s.completed[q.id] and config.replayReward or missions[q.id].reward
            s.completed[q.id]=true;s.tokens+=amount;s.lastFinish=clock
            player.leaderstats.Inspirasi.Value=s.tokens
            send(player,"Misi selesai! +"..amount.." Token Inspirasi.")
        end
    elseif action=="cancel" then
        if s.active and token==s.active.token then s.active=nil;send(player,"Misi dibatalkan. Kamu bisa memilih lagi.")end
    elseif action=="buy" then
        if not near(player,Vector3.new(19,7,-18),18) then send(player,"Temui Lumi di atrium untuk menukar reward.");return end
        for _,item in ipairs(config.shop)do
            if item.id==value and not s.owned[item.id] then
                if s.tokens<item.cost then send(player,"Token belum cukup. Selesaikan misi NPC.");return end
                s.tokens-=item.cost;s.owned[item.id]=true;player.leaderstats.Inspirasi.Value=s.tokens;cosmetics(player);send(player,item.title.." aktif selama sesi ini.");return
            end
        end
    end
end)
PPS.PromptTriggered:Connect(function(prompt,player)
    if not prompt:IsDescendantOf(world) then return end
    local index=tonumber(prompt.Name:match("^Collect_(%d+)$"));if not index then return end
    local s=states[player]
    if not s or not s.active then send(player,"Bicara dengan Sari untuk memulai misi taman.");return end
    if not config.pickups[index] or not near(player,config.pickups[index],13) then return end
    if Engine.collect(s.active,index,now())then send(player,s.active.status=="ready" and "Kelima benda terkumpul! Kembali ke Sari." or "Benda terkumpul: "..s.active.progress.." / 5")end
end)
task.spawn(function()
    while true do
        task.wait(1)
        for player,s in pairs(states)do
            if s.active and s.active.status=="active" and Engine.expire(s.active,now()) then send(player,"Waktu misi habis. Kamu bisa mencoba lagi.")end
        end
    end
end)
