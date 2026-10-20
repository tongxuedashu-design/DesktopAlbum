-- Local adaptive Reminisce photo-card extension, 2026-10-20.
-- Uses native Rainmeter image decoding with EXIF orientation.
local paused = false
local controlsVisible = false
local controlsProgress, controlsStart, controlsTarget = 0, 0, 0
local controlsTick, controlsTicks = 0, 1
local controlsBegan, controlsDuration = 0, 0
local controlsGeneration = 0
function Update() return 0 end
local function number(name, fallback)
    return tonumber(SKIN:GetVariable(name)) or fallback
end
local function set(name, value)
    SKIN:Bang('!SetVariable', name, tostring(value))
end
local hoverAlpha, hoverStart, hoverTarget = 255, 255, 255
local hoverTick, hoverTicks = 0, 1
local hoverBegan, hoverDuration = 0, 0
local function setHoverAlpha(value)
    hoverAlpha = math.max(1, math.min(255, math.floor(value + 0.5)))
    SKIN:Bang('!SetTransparency', tostring(hoverAlpha))
end
function ApplyHoverSettings()
    SKIN:Bang('!CommandMeasure', 'HoverAnimation', 'Stop 1')
    hoverStart, hoverTarget, hoverTick = 255, 255, 0
    setHoverAlpha(255)
    SKIN:Bang('!CommandMeasure', 'ControlsAnimation', 'Stop 1')
    controlsProgress, controlsStart, controlsTarget = 0, 0, 0
    controlsVisible = false
    set('ControlProgress', 0)
    set('ControlAlpha', 0)
end
local function transitionHover(target, durationName)
    -- Reverse from the current alpha when the pointer changes direction.
    SKIN:Bang('!CommandMeasure', 'HoverAnimation', 'Stop 1')
    hoverStart, hoverTarget, hoverTick = hoverAlpha, target, 0
    local duration = number(durationName, -1)
    if duration < 0 then duration = number('HoverFadeDuration', 180) end
    duration = math.max(0, math.min(5000, duration))
    hoverDuration, hoverBegan = duration, os.clock()
    hoverTicks = math.max(1, math.ceil(duration / 16) + 2)
    if duration == 0 or hoverStart == hoverTarget then
        setHoverAlpha(hoverTarget)
        return
    end
    SKIN:Bang('!SetOption', 'HoverAnimation', 'ActionList1',
        'Wait 16 | Repeat HoverStep, 16, ' .. hoverTicks .. ' | Wait 16 | HoverFinish')
    SKIN:Bang('!UpdateMeasure', 'HoverAnimation')
    SKIN:Bang('!CommandMeasure', 'HoverAnimation', 'Execute 1')
end
function HoverStep()
    if hoverAlpha == hoverTarget then return end
    local progress = math.min(1, math.max(0, (os.clock() - hoverBegan) * 1000 / math.max(1, hoverDuration)))
    local eased = progress * progress * (3 - 2 * progress)
    setHoverAlpha(hoverStart + (hoverTarget - hoverStart) * eased)
end
function HoverFinish()
    setHoverAlpha(hoverTarget)
end
function HoverEnter()
    local opacity = number('HoverEnabled', 1) == 0 and 255 or number('HoverOpacity', 90)
    transitionHover(math.max(1, math.min(255, math.floor(opacity))), 'HoverEnterDuration')
end
function HoverLeave()
    transitionHover(255, 'HoverLeaveDuration')
    HideControls()
end
local function controlsFit()
    return number('ShowControls', 1) ~= 0 and number('DisplayW', 0) >= 156 and number('DisplayH', 0) >= 76
end
local function renderControls()
    local distance = math.max(0, math.min(200, number('ControlSlideDistance', 54)))
    set('ControlProgress', controlsProgress)
    set('ControlTarget', controlsTarget)
    set('ControlAlpha', math.floor(controlsProgress * 255 + 0.5))
    set('ControlY', number('ControlBaseY', 0) + distance * (1 - controlsProgress))
    if controlsFit() and (controlsProgress > 0 or controlsTarget > 0) then
        SKIN:Bang('!ShowMeterGroup', 'ActionButtons')
    else
        SKIN:Bang('!HideMeterGroup', 'ActionButtons')
    end
    SKIN:Bang('!UpdateMeterGroup', 'ActionButtons')
    SKIN:Bang('!Redraw')
end
local function transitionControls(target)
    if target > 0 and not controlsFit() then return end
    if controlsTarget == target then return end
    SKIN:Bang('!CommandMeasure', 'ControlsAnimation', 'Stop 1')
    controlsGeneration = controlsGeneration + 1
    controlsStart, controlsTarget, controlsTick = controlsProgress, target, 0
    controlsVisible = target > 0
    local name = target > 0 and 'ControlShowDuration' or 'ControlHideDuration'
    local duration = math.max(0, math.min(5000, number(name, target > 0 and 240 or 200)))
    -- Reverse from the current progress, keeping roughly the same speed.
    controlsDuration = duration * math.abs(target - controlsStart)
    controlsBegan = os.clock()
    controlsTicks = math.max(1, math.ceil(controlsDuration / 16) + 2)
    if duration == 0 or controlsStart == target then
        controlsProgress = target
        renderControls()
        return
    end
    renderControls()
    SKIN:Bang('!SetOption', 'ControlsAnimation', 'ActionList1',
        'Wait 16 | Repeat ControlsStep, 16, ' .. controlsTicks .. ' | Wait 16 | ControlsFinish')
    SKIN:Bang('!SetOption', 'ControlsAnimation', 'ControlsFinish',
        '[!CommandMeasure Layout "ControlsFinish(' .. controlsGeneration .. ')"]')
    SKIN:Bang('!UpdateMeasure', 'ControlsAnimation')
    SKIN:Bang('!CommandMeasure', 'ControlsAnimation', 'Execute 1')
end
function ControlsStep()
    if controlsProgress == controlsTarget then return end
    -- Windows Lua's clock is elapsed process time; avoid stretching the
    -- animation when the message queue delivers timer steps later than 16 ms.
    local progress = math.min(1, math.max(0, (os.clock() - controlsBegan) * 1000 / math.max(1, controlsDuration)))
    local eased = progress * progress * (3 - 2 * progress)
    controlsProgress = controlsStart + (controlsTarget - controlsStart) * eased
    renderControls()
end
function ControlsFinish(generation)
    if generation ~= controlsGeneration then return end
    controlsProgress = controlsTarget
    renderControls()
end
function RenderCurrent()
    if (SKIN:GetVariable('CurrentPhoto') or '') == '' then return end
    SKIN:Bang('!SetOption', 'Photo', 'W', '')
    SKIN:Bang('!SetOption', 'Photo', 'H', '')
    SKIN:Bang('!UpdateMeter', 'Photo')
    local photo = SKIN:GetMeter('Photo')
    local nativeW, nativeH = photo:GetW(), photo:GetH()
    if nativeW < 1 or nativeH < 1 then return end
    local pad = math.max(20, math.floor(number('FramePadding', 20)))
    local maxW = math.max(1, number('SizeW', 480))
    local maxH = math.max(1, number('SizeH', 480))
    maxW = math.min(maxW, math.max(1, number('WORKAREAWIDTH', maxW+2*pad+16)-2*pad-16))
    maxH = math.min(maxH, math.max(1, number('WORKAREAHEIGHT', maxH+2*pad+20)-2*pad-20))
    local scale = math.min(1, maxW/nativeW, maxH/nativeH)
    local width = math.max(1, math.floor(nativeW*scale+0.5))
    local height = math.max(1, math.floor(nativeH*scale+0.5))
    set('DisplayW', width)
    set('DisplayH', height)
    set('PhotoX', pad)
    set('PhotoY', pad)
    set('Radius', math.min(math.max(0, number('CornerRadius', 16)), width/6, height/6))
    set('FrameW', width+2*pad)
    set('FrameH', height+2*pad+4)
    local show = controlsFit()
    set('ControlStep', show and 1 or 0)
    set('ControlX', show and (pad+(width-144)/2) or 0)
    set('ControlBaseY', show and (pad+height-54) or 0)
    local triggerPercent = math.max(1, math.min(100, number('ControlTriggerPercent', 30)))
    local triggerHeight = math.min(height, math.max(64, math.floor(height * triggerPercent / 100 + 0.5)))
    set('ControlTriggerH', triggerHeight)
    set('ControlTriggerY', pad + height - triggerHeight)
    SKIN:Bang('!SetOption', 'Photo', 'W', '#DisplayW#')
    SKIN:Bang('!SetOption', 'Photo', 'H', '#DisplayH#')
    SKIN:Bang('!UpdateMeterGroup', 'Frame')
    if not show then
        SKIN:Bang('!CommandMeasure', 'ControlsAnimation', 'Stop 1')
        controlsVisible = false
        controlsProgress, controlsStart, controlsTarget = 0, 0, 0
    end
    renderControls()
end
function SelectPhoto()
    local selected = SKIN:GetMeasure('RandomPhoto'):GetStringValue()
    if not selected or selected == '' then return end
    local current = SKIN:GetVariable('CurrentPhoto') or ''
    if selected ~= current then set('PreviousPhoto', current) end
    set('CurrentPhoto', selected)
    RenderCurrent()
    SKIN:Bang('!Log', 'PhotoFrame: '..selected, 'Notice')
end
-- Manual previous/next follow the same name-sorted folder list.
-- FileView scans asynchronously; its FinishAction calls LibraryReady.
local pendingSteps = 0

local function pathKey(path)
    return string.lower((path or ''):gsub(string.char(92), '/'))
end

local function navigate(step)
    pendingSteps = pendingSteps + step
    SKIN:Bang('!CommandMeasure', 'PhotoLibrary', 'Update')
    SKIN:Bang('!UpdateMeasure', 'PhotoLibrary')
end

function LibraryReady()
    if pendingSteps == 0 then return end
    local steps = pendingSteps
    pendingSteps = 0
    SKIN:Bang('!UpdateMeasure', 'PhotoLibrary')
    local count = math.floor(SKIN:GetMeasure('PhotoLibrary'):GetValue())
    if count < 1 then return end
    local files = {}
    local current = SKIN:GetVariable('CurrentPhoto') or ''
    local currentIndex = nil
    for index = 1, count do
        SKIN:Bang('!SetOption', 'PhotoEntry', 'Index', tostring(index))
        SKIN:Bang('!UpdateMeasure', 'PhotoEntry')
        local path = SKIN:GetMeasure('PhotoEntry'):GetStringValue()
        if path and path ~= '' then
            files[#files + 1] = path
            if pathKey(path) == pathKey(current) then currentIndex = #files end
        end
    end
    if #files == 0 then return end
    local targetIndex
    if currentIndex then
        targetIndex = (currentIndex - 1 + steps) % #files + 1
    else
        targetIndex = steps > 0 and 1 or #files
    end
    local selected = files[targetIndex]
    if selected ~= current then set('PreviousPhoto', current) end
    set('CurrentPhoto', selected)
    RenderCurrent()
    SKIN:Bang('!Log', 'PhotoFrame manual: '..selected, 'Notice')
end

function ShowPrevious()
    navigate(-1)
end

function NextPhoto()
    navigate(1)
end

function TogglePause()
    paused = not paused
    SKIN:Bang(paused and '!PauseMeasure' or '!UnpauseMeasure', 'RandomPhoto')
    set('PauseAlpha', paused and 0 or 242)
    set('PlayAlpha', paused and 242 or 0)
    set('PauseTip', paused and '继续轮播' or '暂停轮播')
    SKIN:Bang('!UpdateMeter', 'PauseButton')
    SKIN:Bang('!Redraw')
end
function ShowControls()
    transitionControls(1)
end
function HideControls()
    for _,name in ipairs({'PrevHover','PauseHover','NextHover','OpenHover'}) do set(name,1) end
    transitionControls(0)
end
