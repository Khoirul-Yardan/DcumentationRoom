-- Data operations have no dependency on world objects or UI.
local Service = {}
local function normalize(value)
    return string.lower(tostring(value or "")):gsub("%s+", " ")
end
function Service.validate(catalog)
    assert(catalog.schemaVersion == 1, "Versi katalog belum didukung")
    local seen = {}
    for _, project in ipairs(catalog.projects) do
        assert(type(project.id) == "string" and not seen[project.id], "ID proyek kosong atau duplikat")
        seen[project.id] = true
        assert(type(project.title) == "string" and #project.title > 0, "Judul wajib")
        assert(table.find(catalog.categories, project.category), "Kategori tidak dikenal")
        assert(project.publicationState == "published" and project.visibility == "public", "Snapshot client hanya boleh berisi data publik")
        assert(project.year == nil or type(project.year) == "number", "Tahun harus angka atau belum diketahui")
        if project.isSample then assert(string.sub(project.id, 1, 7) == "sample-", "ID contoh harus berawalan sample-") end
    end
    return true
end
function Service.search(catalog, query)
    query = query or {}
    local out = {}
    local q = normalize(query.text)
    for _, project in ipairs(catalog.projects) do
        local haystack = normalize(project.title .. " " .. project.summary .. " " .. project.category .. " " .. table.concat(project.tags or {}, " "))
        if project.publicationState == "published" and project.visibility == "public"
            and (q == "" or string.find(haystack, q, 1, true))
            and (not query.category or project.category == query.category)
            and (not query.year or project.year == query.year)
            and (not query.status or project.developmentStatus == query.status)
            and (not query.onlyBookmarks or query.bookmarks[project.id]) then
            table.insert(out, project)
        end
    end
    table.sort(out, function(a, b)
        if a.sortOrder ~= b.sortOrder then return a.sortOrder < b.sortOrder end
        return a.id < b.id
    end)
    return out
end
function Service.page(items, index, size)
    local pages = math.max(1, math.ceil(#items / size))
    index = math.clamp(index, 1, pages)
    local out = {}
    for i = (index - 1) * size + 1, math.min(index * size, #items) do table.insert(out, items[i]) end
    return out, index, pages
end
function Service.find(catalog, id)
    for _, project in ipairs(catalog.projects) do if project.id == id then return project end end
    return nil
end
return Service
