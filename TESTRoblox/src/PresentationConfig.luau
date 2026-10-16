-- Server-only configuration. Never put OAuth tokens/API keys in a place file.
return {
    BridgeUrl = "", -- Optional HTTPS endpoint, e.g. https://your-host/v1/canva/resolve
    BridgeSecretName = "AtlasBridgeToken", -- Roblox Creator Hub secret, restricted to your host.
    LeaseSeconds = 120,
    MaxSlides = 30,
    Decks = {}, -- ["CANVA_DESIGN_ID"] = {"rbxassetid://IMAGE_ID", ...}
}
