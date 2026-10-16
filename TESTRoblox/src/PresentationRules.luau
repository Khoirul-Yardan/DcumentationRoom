local Rules={}
function Rules.designId(url)
    if type(url)~="string" or #url>2048 then return nil end
    url=url:match("^%s*(.-)%s*$")
    local host,path=url:match("^https://([^/]+)(/.*)$")
    if host~="www.canva.com" and host~="canva.com" then return nil end
    local id=path:match("^/design/([%w_-]+)/") or path:match("^/design/([%w_-]+)$")
    if not id or #id<5 or #id>100 then return nil end
    return id
end
function Rules.imageId(value)
    if type(value)~="string" or #value>40 then return nil end
    local id=value:match("^rbxassetid://(%d+)$") or value:match("^(%d+)$")
    if not id or #id<1 or #id>20 or tonumber(id)==0 then return nil end
    return "rbxassetid://"..id
end
function Rules.images(input,limit)
    if type(input)~="table" or #input==0 or #input>limit then return nil end
    local out={}
    for _,value in ipairs(input)do local id=Rules.imageId(value);if not id then return nil end;table.insert(out,{image=id,title="Presentasi",body=""})end
    return out
end
return Rules
