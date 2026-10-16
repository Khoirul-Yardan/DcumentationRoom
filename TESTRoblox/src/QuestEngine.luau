-- Pure state transitions. Authority, distance checks, and payout live on server.
local Engine={}
local cards={
    {file="Github Web HIMA.txt",category="PROJECT"},
    {file="Github Scraping web.txt",category="PROJECT"},
    {file="Knowledge Base",category="Tech Insight"},
    {file="Mini Showcase.txt",category="Mini Showcase"},
    {file="Tech Corner.txt",category="Tech Corner"},
    {file="5_Template_Tech_Insight_RISTEK.docx",category="Administrasi"},
}
Engine.categories={"PROJECT","Tech Insight","Mini Showcase","Tech Corner","Administrasi"}
function Engine.start(id,now,rng,token)
    if id~="circuit" and id~="archive" and id~="garden" then return nil end
    local q={id=id,token=token,deadline=now+(id=="garden" and 120 or 90),round=1,step=1,mistakes=0,progress=0,collected={},status="active",started=now}
    if id=="circuit" then
        q.sequence={};for i=1,3 do q.sequence[i]=rng:NextInteger(1,4) end
        q.readyAt=now+3.2
    elseif id=="archive" then
        q.cards=table.clone(cards)
        for i=#q.cards,2,-1 do local j=rng:NextInteger(1,i);q.cards[i],q.cards[j]=q.cards[j],q.cards[i] end
    end
    return q
end
function Engine.expire(q,now)
    if q.status=="active" and now>q.deadline then q.status="failed" end
    return q.status=="failed"
end
function Engine.answer(q,value,now,rng)
    if q.status~="active" or Engine.expire(q,now) then return "inactive" end
    if q.id=="circuit" then
        if now<q.readyAt then return "watch" end
        if type(value)~="number" or value%1~=0 or value<1 or value>4 then return "invalid" end
        if value~=q.sequence[q.step] then q.status="failed";return "wrong" end
        q.step+=1
        if q.step>#q.sequence then
            q.progress+=1
            if q.round==3 then q.status="ready";return "ready" end
            q.round+=1;q.step=1;q.sequence={}
            for i=1,q.round+2 do q.sequence[i]=rng:NextInteger(1,4) end
            q.readyAt=now+.8*#q.sequence+.8
            return "nextRound"
        end
        return "correct"
    elseif q.id=="archive" then
        if type(value)~="string" or not table.find(Engine.categories,value) then return "invalid" end
        if value~=q.cards[q.step].category then
            q.mistakes+=1
            if q.mistakes>=3 then q.status="failed";return "wrong" end
            return "retry"
        end
        q.progress+=1;q.step+=1
        if q.progress==5 then q.status="ready";return "ready" end
        return "correct"
    end
    return "invalid"
end
function Engine.collect(q,index,now)
    if q.id~="garden" or q.status~="active" or Engine.expire(q,now) then return false end
    if type(index)~="number" or index%1~=0 or index<1 or index>5 or q.collected[index] then return false end
    q.collected[index]=true;q.progress+=1
    if q.progress==5 then q.status="ready" end
    return true
end
function Engine.public(q)
    if not q then return nil end
    return {id=q.id,token=q.token,deadline=q.deadline,round=q.round,step=q.step,status=q.status,progress=q.progress,mistakes=q.mistakes,
        sequence=q.id=="circuit" and table.clone(q.sequence) or nil,readyAt=q.readyAt,
        card=q.id=="archive" and q.cards[math.min(q.step,#q.cards)].file or nil,collected=table.clone(q.collected)}
end
function Engine.claim(q)
    if not q or q.status~="ready" then return false end
    q.status="claimed";return true
end
return Engine
