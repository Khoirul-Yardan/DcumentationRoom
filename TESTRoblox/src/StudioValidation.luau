-- Run in Studio Command Bar (Edit mode):
-- require(game.ServerStorage.AtlasValidation).run()
-- Read-only assertions. Does not publish, travel, or write player data.
local Test = {}
function Test.run()
    local shared=game:GetService("ReplicatedStorage"):WaitForChild("Atlas")
    local service=require(shared.CatalogService)
    local actual=require(shared.Catalog)
    local passed=0
    local function check(name,condition)
        assert(condition,"[ATLAS TEST FAILED] "..name)
        passed+=1;print("[ATLAS PASS] "..name)
    end
    check("Actual catalog validates",service.validate(actual))
    local museum=workspace:FindFirstChild("AtlasMuseum")
    check("Museum root exists",museum~=nil)
    for _,name in ipairs({"Atrium","Gallery","Archive","Auditorium","Garden"})do
        check("Zone "..name,museum:FindFirstChild(name)~=nil)
    end
    check("Streaming disabled",not workspace.StreamingEnabled)
    for slot=1,8 do
        local stand=museum.Gallery:FindFirstChild(string.format("Stand%02d",slot))
        check("Exhibit slot "..slot,stand~=nil and stand.Interact:FindFirstChild("Project_"..slot)~=nil)
    end
    local fixture={schemaVersion=1,categories={"Teknologi","Lingkungan"},projects={}}
    local function project(id,title,category,year,status,sort)
        return {id=id,title=title,summary="Catatan",category=category,year=year,developmentStatus=status,
            sortOrder=sort,tags={"uji"},publicationState="published",visibility="public",isSample=true}
    end
    fixture.projects={
        project("sample-a","Alpha","Teknologi",2026,"completed",1),
        project("sample-b","Beta","Lingkungan",2026,"completed",2),
        project("sample-c","Gamma","Teknologi",2025,"archived",3),
        project("sample-d","Delta 100%","Teknologi",2026,"in_progress",4),
    }
    check("Search is case-insensitive",service.search(fixture,{text="ALPHA"})[1].id=="sample-a")
    check("Percent treated literally",#service.search(fixture,{text="%"})==1)
    local filtered=service.search(fixture,{category="Teknologi",year=2026,status="completed"})
    check("Three filters intersect",#filtered==1 and filtered[1].id=="sample-a")
    check("Archive remains searchable",service.search(fixture,{status="archived"})[1].id=="sample-c")
    check("Bookmark intersection",#service.search(fixture,{category="Lingkungan",onlyBookmarks=true,bookmarks={["sample-a"]=true}})==0)
    for _,count in ipairs({0,1,8,9,100})do
        local items={}
        for i=1,count do items[i]={id="sample-"..i} end
        local page,index,pages=service.page(items,999,8)
        local expectedPages=math.max(1,math.ceil(count/8))
        check("Page bounds "..count,index==expectedPages and pages==expectedPages and #page==(count==0 and 0 or (count-1)%8+1))
    end
    local duplicate=table.clone(fixture);duplicate.projects=table.clone(fixture.projects);table.insert(duplicate.projects,fixture.projects[1])
    check("Duplicate IDs rejected",not pcall(service.validate,duplicate))
    local draft=table.clone(fixture);draft.projects={table.clone(fixture.projects[1])};draft.projects[1].publicationState="draft"
    check("Draft snapshot rejected",not pcall(service.validate,draft))
    print("[ATLAS] "..passed.." Studio data/structure assertions passed. Play, UI and multiplayer still require testing.")
    return passed
end
return Test
