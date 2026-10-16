local Players = game:GetService("Players")
local RunService = game:GetService("RunService")
local shared = game:GetService("ReplicatedStorage"):WaitForChild("Atlas")
local travel = shared:WaitForChild("Travel")
local destinations = {
    Atrium = Vector3.new(0, 8, 22), Gallery = Vector3.new(-64, 8, 0),
    Archive = Vector3.new(74, 8, 0), Auditorium = Vector3.new(0, 8, -76),
    Garden = Vector3.new(0, 8, 79),
}
local lastTravel = {}
travel.OnServerEvent:Connect(function(player, zone)
    if typeof(zone) ~= "string" or not destinations[zone] then return end
    local now = os.clock()
    if now - (lastTravel[player] or -math.huge) < 1 then return end
    lastTravel[player] = now
    local character = player.Character
    local humanoid = character and character:FindFirstChildOfClass("Humanoid")
    local root = character and character:FindFirstChild("HumanoidRootPart")
    if not root or not humanoid or humanoid.Health <= 0 then return end
    humanoid.Sit = false
    -- Let the seat weld release before moving the entire character.
    task.defer(function()
        if player.Character ~= character or not character.Parent then return end
        root.AssemblyLinearVelocity = Vector3.zero
        root.AssemblyAngularVelocity = Vector3.zero
        character:PivotTo(CFrame.new(destinations[zone]))
        travel:FireClient(player, zone)
    end)
end)
Players.PlayerRemoving:Connect(function(player) lastTravel[player] = nil end)
local elapsed = 0
RunService.Heartbeat:Connect(function(dt)
    elapsed += dt
    if elapsed < 1 then return end
    elapsed = 0
    for _, player in ipairs(Players:GetPlayers()) do
        local root = player.Character and player.Character:FindFirstChild("HumanoidRootPart")
        if root and root.Position.Y < -5 then
            root.AssemblyLinearVelocity = Vector3.zero
            player.Character:PivotTo(CFrame.new(destinations.Atrium))
        end
    end
end)
print("[ATLAS] Museum ready; server-validated travel and curated RISTEK Drive snapshot loaded.")
